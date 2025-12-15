# Machine-Assertable E2E Tests - Design Document

**Date:** 2025-12-15  
**Status:** Approved  
**Author:** Design brainstorming session

## Overview

Replace the SSE-based E2E test suite with a machine-assertable testing approach that enables automated regression detection and development workflow feedback without requiring human or LLM supervision.

## Goals

- **Machine-assertable tests**: Tests can pass/fail automatically based on behavioral outcomes
- **Regression detection**: Catch when changes break repost detection functionality
- **Development workflow**: Quick feedback loop when working on repost detection code
- **Not CI/CD**: Tests remain local-only due to heavy ML service requirements
- **Future-proof**: Support message response assertions (not just reactions)
- **Simplicity**: Remove SSE complexity, use straightforward query endpoint

## Problem with Current SSE Approach

The current SSE implementation streams events in real-time, but:
- ❌ Cannot be asserted on programmatically (requires human to watch stream)
- ❌ No way to wait for async processing to complete
- ❌ Cannot determine test pass/fail automatically
- ❌ Adds unnecessary complexity (Channel-based pub/sub, SSE protocol)

**Result**: Tests require human supervision and cannot detect regressions automatically.

## Proposed Solution: Action Logging with Query Endpoint

Replace SSE with an in-memory action logger that:
- ✅ Captures all NullChatClient actions (reactions, messages, embeds)
- ✅ Provides query endpoint that waits for expected action count
- ✅ Returns actions in machine-readable format for assertions
- ✅ Simpler implementation (no SSE/Channel complexity)

## Architecture

### New Components

#### 1. ITestActionLogger Interface

**Location:** `src/ShitpostBot/src/ShitpostBot.WebApi/Services/ITestActionLogger.cs`

```csharp
namespace ShitpostBot.WebApi.Services;

public interface ITestActionLogger
{
    Task LogActionAsync(ulong messageId, TestAction action);
    Task<List<TestAction>> WaitForActionsAsync(ulong messageId, int expectedCount, TimeSpan timeout);
}

public record TestAction(string Type, string Data, DateTimeOffset Timestamp);
```

**Purpose:**
- `LogActionAsync`: Log an action for a specific messageId
- `WaitForActionsAsync`: Wait until expected number of actions logged or timeout
- `TestAction`: Represents a single chat action with type, data, and timestamp

#### 2. TestActionLogger Implementation

**Location:** `src/ShitpostBot/src/ShitpostBot.WebApi/Services/TestActionLogger.cs`

```csharp
using System.Collections.Concurrent;
using System.Diagnostics;

namespace ShitpostBot.WebApi.Services;

public class TestActionLogger : ITestActionLogger
{
    private readonly ConcurrentDictionary<ulong, List<TestAction>> _actions = new();
    private readonly ILogger<TestActionLogger> _logger;

    public TestActionLogger(ILogger<TestActionLogger> logger)
    {
        _logger = logger;
    }

    public Task LogActionAsync(ulong messageId, TestAction action)
    {
        _actions.AddOrUpdate(
            messageId,
            _ => new List<TestAction> { action },
            (_, existing) => 
            {
                lock (existing)
                {
                    existing.Add(action);
                }
                return existing;
            }
        );
        
        _logger.LogDebug("Logged action for message {MessageId}: {Type}", messageId, action.Type);
        return Task.CompletedTask;
    }

    public async Task<List<TestAction>> WaitForActionsAsync(
        ulong messageId, 
        int expectedCount, 
        TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();
        
        _logger.LogInformation("Waiting for {ExpectedCount} actions on message {MessageId} (timeout: {Timeout}ms)", 
            expectedCount, messageId, timeout.TotalMilliseconds);
        
        while (stopwatch.Elapsed < timeout)
        {
            if (_actions.TryGetValue(messageId, out var actions))
            {
                lock (actions)
                {
                    if (actions.Count >= expectedCount)
                    {
                        _logger.LogInformation("Found {Count} actions for message {MessageId} after {Elapsed}ms", 
                            actions.Count, messageId, stopwatch.ElapsedMilliseconds);
                        return actions.ToList();
                    }
                }
            }
            
            await Task.Delay(100); // Poll every 100ms
        }
        
        // Timeout - return whatever we have
        var final = _actions.TryGetValue(messageId, out var finalActions) 
            ? finalActions.ToList() 
            : new List<TestAction>();
        
        _logger.LogWarning("Timeout waiting for actions on message {MessageId}. Expected {Expected}, got {Actual}", 
            messageId, expectedCount, final.Count);
        
        return final;
    }
}
```

**Design Decisions:**
- **ConcurrentDictionary** for thread-safe storage per messageId
- **Lock on List<T>** for thread-safe additions to action list
- **Polling approach** with 100ms intervals (simple, no complex signaling)
- **No cleanup** - unlimited buffer until service restart (acceptable for local testing)
- **Timeout returns partial results** - tests can assert on what was captured

#### 3. Updated NullChatClient

**Location:** `src/ShitpostBot/src/ShitpostBot.WebApi/Services/NullChatClient.cs`

**Changes:**
- Replace `ITestEventPublisher` dependency with `ITestActionLogger`
- Remove all `PublishAsync` calls
- Add `LogActionAsync` calls in each method

**Example (React method):**
```csharp
public async Task React(MessageIdentification messageIdentification, string emoji)
{
    _logger.LogInformation("Would react to message {MessageId} with {Emoji}", 
        messageIdentification.MessageId, emoji);
    
    await _actionLogger.LogActionAsync(
        messageIdentification.MessageId,
        new TestAction(
            "reaction",
            JsonSerializer.Serialize(new { emoji }),
            DateTimeOffset.UtcNow
        )
    );
}
```

**Example (SendMessage method):**
```csharp
public async Task SendMessage(MessageDestination destination, string? messageContent)
{
    _logger.LogInformation("Would send message to {Destination}: {Content}", destination, messageContent);
    
    await _actionLogger.LogActionAsync(
        destination.ReplyToMessageId ?? 0,
        new TestAction(
            "message",
            JsonSerializer.Serialize(new { content = messageContent }),
            DateTimeOffset.UtcNow
        )
    );
}
```

#### 4. Query Endpoint

**Location:** `src/ShitpostBot/src/ShitpostBot.WebApi/Endpoints/TestEndpoints.cs`

**Add new endpoint:**
```csharp
group.MapGet("/actions/{messageId}", GetActions);

private static async Task<IResult> GetActions(
    ulong messageId,
    [FromQuery] int expectedCount = 0,
    [FromQuery] int timeout = 10000,
    [FromServices] ITestActionLogger logger)
{
    var stopwatch = Stopwatch.StartNew();
    
    var actions = await logger.WaitForActionsAsync(
        messageId, 
        expectedCount, 
        TimeSpan.FromMilliseconds(timeout)
    );
    
    return Results.Ok(new
    {
        messageId,
        actions,
        waitedMs = stopwatch.ElapsedMilliseconds
    });
}
```

**Endpoint Behavior:**
- `GET /test/actions/{messageId}?expectedCount=2&timeout=10000`
- Waits up to `timeout` milliseconds for `expectedCount` actions
- Returns immediately if expected count reached before timeout
- Returns partial results if timeout occurs
- Default: `expectedCount=0` (returns immediately), `timeout=10000` (10 seconds)

**Response Format:**
```json
{
  "messageId": 1000002,
  "actions": [
    {
      "type": "reaction",
      "data": "{\"emoji\":\":police_car:\"}",
      "timestamp": "2025-12-15T10:30:45.123Z"
    },
    {
      "type": "reaction",
      "data": "{\"emoji\":\":rotating_light:\"}",
      "timestamp": "2025-12-15T10:30:45.456Z"
    }
  ],
  "waitedMs": 2341
}
```

## Components to Remove

### Files to Delete
1. `src/ShitpostBot/src/ShitpostBot.WebApi/Services/ITestEventPublisher.cs`
2. `src/ShitpostBot/src/ShitpostBot.WebApi/Services/TestEventPublisher.cs`

### Code to Remove
1. **TestEndpoints.cs**: Remove `GET /events` endpoint and `StreamEvents` method
2. **Program.cs**: Remove `ITestEventPublisher` DI registration

## Updated Test File

**Location:** `test/e2e-repost-detection.http`

**New Format:**
```http
@host = http://localhost:8080

### INSTRUCTIONS
# This test suite validates repost detection behavior through machine-assertable actions.
# Each scenario posts images, then queries the actions endpoint to verify expected behavior.
# 
# Expected behaviors:
# - High similarity (same image, different format): 2 reactions (repost detected)
# - Low similarity (unrelated images): 0 reactions (not a repost)

### ============================================
### SCENARIO 1: High Similarity - Repost Detection
### ============================================

### 1a. Post original image
POST {{host}}/test/image-message
Content-Type: application/json

{
  "imageUrl": "https://media.discordapp.net/attachments/1319991503891861534/1449832200702001183/frenchcat.png?ex=694054f5&is=693f0375&hm=987e474fcc62613908de83ee2db12365472ec3bde4a27525185bbb80184e44e5&=&format=webp&quality=lossless&width=918&height=1155"
}

### Expected: {"messageId": 1000001, "tracked": true}

### 1b. Post repost (same image, JPG format)
POST {{host}}/test/image-message
Content-Type: application/json

{
  "imageUrl": "https://media.discordapp.net/attachments/1319991503891861534/1449832201049870376/frenchcat.jpg?ex=694054f5&is=693f0375&hm=b8b5a37d8b72b4261dd61781d1ba828285b3ed9e5e00f1e044c69a274e3b05af&=&format=webp&width=918&height=1155"
}

### Expected: {"messageId": 1000002, "tracked": true}

### 1c. Query actions - expect 2 reactions (repost detected)
GET {{host}}/test/actions/1000002?expectedCount=2&timeout=10000

### Expected response:
# {
#   "messageId": 1000002,
#   "actions": [
#     {"type": "reaction", "data": "{\"emoji\":\":police_car:\"}", "timestamp": "..."},
#     {"type": "reaction", "data": "{\"emoji\":\":rotating_light:\"}", "timestamp": "..."}
#   ],
#   "waitedMs": 2341
# }
# PASS if: actions.length == 2 && both are type "reaction"

### ============================================
### SCENARIO 2: Low Similarity - No Repost
### ============================================

### 2a. Post first image
POST {{host}}/test/image-message
Content-Type: application/json

{
  "imageUrl": "https://media.discordapp.net/attachments/1319991503891861534/1449824813462851675/RDT_20251203_1901397855856549389737722.jpg?ex=69404e13&is=693efc93&hm=9295add85964d52e714ff95792e0b6123bdbaa6149982afa93e3ec0d287c69aa&=&format=webp&width=854&height=1155"
}

### Expected: {"messageId": 1000003, "tracked": true}

### 2b. Post unrelated image
POST {{host}}/test/image-message
Content-Type: application/json

{
  "imageUrl": "https://media.discordapp.net/attachments/1319991503891861534/1449825108611825915/RDT_20250125_1000203858268434480552263.jpg?ex=69404e5a&is=693efcda&hm=01d74376049da7f5b1bb16484c9b7dfa7990f7ef28b2155670f1941e69e827e3&=&format=webp&width=1329&height=1155"
}

### Expected: {"messageId": 1000004, "tracked": true}

### 2c. Query actions - expect 0 reactions (not a repost)
GET {{host}}/test/actions/1000004?expectedCount=0&timeout=10000

### Expected response:
# {
#   "messageId": 1000004,
#   "actions": [],
#   "waitedMs": 10003
# }
# PASS if: actions.length == 0

### ============================================
### NOTES
### ============================================
# - Each GET /test/actions waits for expectedCount or timeout
# - Timeout with partial results allows debugging (see what actions did occur)
# - Tests are machine-assertable: check actions.length and action types
# - Future: Can assert on message content for bot responses
```

## Automated Testing (Optional Future Enhancement)

While the HTTP file enables manual testing with clear pass/fail criteria, users can optionally write xUnit integration tests:

```csharp
[Fact]
public async Task RepostDetection_HighSimilarity_TriggersReactions()
{
    // Arrange
    var client = new HttpClient { BaseAddress = new Uri("http://localhost:8080") };
    
    // Act - Post original
    var original = await client.PostAsJsonAsync("/test/image-message", new {
        imageUrl = "https://...frenchcat.png"
    });
    var originalResponse = await original.Content.ReadFromJsonAsync<PostMessageResponse>();
    
    // Act - Post repost
    var repost = await client.PostAsJsonAsync("/test/image-message", new {
        imageUrl = "https://...frenchcat.jpg"
    });
    var repostResponse = await repost.Content.ReadFromJsonAsync<PostMessageResponse>();
    
    // Assert - Query actions
    var actions = await client.GetFromJsonAsync<ActionsResponse>(
        $"/test/actions/{repostResponse.MessageId}?expectedCount=2&timeout=10000"
    );
    
    Assert.Equal(2, actions.Actions.Count);
    Assert.All(actions.Actions, a => Assert.Equal("reaction", a.Type));
    Assert.Contains(actions.Actions, a => a.Data.Contains(":police_car:"));
    Assert.Contains(actions.Actions, a => a.Data.Contains(":rotating_light:"));
}
```

## Migration Plan

### Phase 1: Remove SSE Infrastructure
- Delete `ITestEventPublisher.cs` and `TestEventPublisher.cs`
- Remove SSE endpoint from `TestEndpoints.cs`
- Remove `ITestEventPublisher` registration from `Program.cs`

### Phase 2: Add Action Logging
- Create `ITestActionLogger.cs` interface
- Create `TestActionLogger.cs` implementation
- Register `ITestActionLogger` in `Program.cs`

### Phase 3: Update NullChatClient
- Replace `ITestEventPublisher` dependency with `ITestActionLogger`
- Update all methods to call `LogActionAsync` instead of `PublishAsync`
- Update constructor and fields

### Phase 4: Add Query Endpoint
- Add `GET /test/actions/{messageId}` endpoint to `TestEndpoints.cs`
- Implement waiting logic with expectedCount and timeout

### Phase 5: Update Test Documentation
- Rewrite `test/e2e-repost-detection.http` with query endpoint approach
- Update comments to explain machine-assertable format
- Add pass/fail criteria for each scenario

## Key Design Decisions

1. **Remove SSE entirely**: If tests can't assert on events, SSE adds complexity without value
2. **In-memory storage**: Simple unlimited buffer suitable for local testing, no persistence needed
3. **Polling approach**: 100ms polling is simple and sufficient for async processing (typically 2-3 seconds)
4. **Expected count waiting**: Tests specify expectations explicitly, endpoint waits for them
5. **Timeout returns partial results**: Enables debugging when tests fail (see what actions did occur)
6. **Flat action list**: Chronological order supports future message assertions
7. **JSON data field**: Flexible structure for different action types

## Benefits

- ✅ **Machine-assertable**: Tests can pass/fail automatically based on action count and types
- ✅ **Regression detection**: Catches when repost detection breaks
- ✅ **Development workflow**: Quick feedback during development
- ✅ **Simpler implementation**: No Channel/SSE complexity
- ✅ **Future-proof**: Supports message response assertions
- ✅ **Clear pass/fail**: Explicit expectations in test file
- ✅ **Local-only**: No CI/CD concerns with heavy ML service

## Non-Goals

- Automated CI/CD integration (ML service too heavy)
- Persistent action storage (in-memory sufficient for local testing)
- Real-time streaming (not needed for assertions)
- Exact similarity score assertions (behavioral outcomes only)
- Performance testing (functional testing only)

## Success Criteria

- ✅ Query endpoint waits for expected action count or timeout
- ✅ HTTP test file has clear pass/fail criteria for each scenario
- ✅ Tests can detect repost detection regressions automatically
- ✅ No human supervision required to determine test results
- ✅ SSE infrastructure fully removed
- ✅ Simpler codebase with fewer components
