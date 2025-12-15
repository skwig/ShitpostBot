# Machine-Assertable E2E Tests Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace SSE-based E2E testing with machine-assertable action logging that enables automated regression detection without human supervision.

**Architecture:** Remove SSE infrastructure (ITestEventPublisher, TestEventPublisher, SSE endpoint), replace with ITestActionLogger that logs actions to in-memory buffer, add query endpoint that waits for expected action count.

**Tech Stack:** ASP.NET Core Minimal APIs, ConcurrentDictionary, in-memory action logging

---

## Task 1: Create ITestActionLogger Interface

**Files:**
- Create: `src/ShitpostBot/src/ShitpostBot.WebApi/Services/ITestActionLogger.cs`

**Step 1: Write the interface and record type**

Create the file with:

```csharp
namespace ShitpostBot.WebApi.Services;

public interface ITestActionLogger
{
    Task LogActionAsync(ulong messageId, TestAction action);
    Task<List<TestAction>> WaitForActionsAsync(ulong messageId, int expectedCount, TimeSpan timeout);
}

public record TestAction(string Type, string Data, DateTimeOffset Timestamp);
```

**Step 2: Verify file compiles**

Run: `dotnet build src/ShitpostBot/src/ShitpostBot.WebApi/ShitpostBot.WebApi.csproj`
Expected: Build succeeds

**Step 3: Commit**

```bash
git add src/ShitpostBot/src/ShitpostBot.WebApi/Services/ITestActionLogger.cs
git commit -m "feat: add ITestActionLogger interface for action logging"
```

---

## Task 2: Implement TestActionLogger

**Files:**
- Create: `src/ShitpostBot/src/ShitpostBot.WebApi/Services/TestActionLogger.cs`

**Step 1: Write the implementation**

Create the file with:

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

**Step 2: Verify file compiles**

Run: `dotnet build src/ShitpostBot/src/ShitpostBot.WebApi/ShitpostBot.WebApi.csproj`
Expected: Build succeeds

**Step 3: Commit**

```bash
git add src/ShitpostBot/src/ShitpostBot.WebApi/Services/TestActionLogger.cs
git commit -m "feat: implement TestActionLogger with in-memory action logging"
```

---

## Task 3: Replace ITestEventPublisher with ITestActionLogger in DI

**Files:**
- Modify: `src/ShitpostBot/src/ShitpostBot.WebApi/Program.cs`

**Step 1: Replace DI registration**

Find this line (currently line 18):
```csharp
builder.Services.AddSingleton<ITestEventPublisher, TestEventPublisher>();
```

Replace with:
```csharp
builder.Services.AddSingleton<ITestActionLogger, TestActionLogger>();
```

**Step 2: Verify file compiles**

Run: `dotnet build src/ShitpostBot/src/ShitpostBot.WebApi/ShitpostBot.WebApi.csproj`
Expected: Build succeeds

**Step 3: Commit**

```bash
git add src/ShitpostBot/src/ShitpostBot.WebApi/Program.cs
git commit -m "refactor: replace ITestEventPublisher with ITestActionLogger in DI"
```

---

## Task 4: Update NullChatClient to Use ITestActionLogger

**Files:**
- Modify: `src/ShitpostBot/src/ShitpostBot.WebApi/Services/NullChatClient.cs`

**Step 1: Replace dependency injection**

Change constructor from:
```csharp
private readonly ITestEventPublisher _eventPublisher;

public NullChatClient(ILogger<NullChatClient> logger, ITestEventPublisher eventPublisher)
{
    _logger = logger;
    _eventPublisher = eventPublisher;
}
```

To:
```csharp
private readonly ITestActionLogger _actionLogger;

public NullChatClient(ILogger<NullChatClient> logger, ITestActionLogger actionLogger)
{
    _logger = logger;
    _actionLogger = actionLogger;
}
```

**Step 2: Update SendMessage(string) method**

Replace the method body:
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

**Step 3: Update SendMessage(DiscordMessageBuilder) method**

Replace the method body:
```csharp
public async Task SendMessage(MessageDestination destination, DiscordMessageBuilder messageBuilder)
{
    _logger.LogInformation("Would send message builder to {Destination}", destination);
    
    await _actionLogger.LogActionAsync(
        destination.ReplyToMessageId ?? 0,
        new TestAction(
            "message",
            JsonSerializer.Serialize(new { content = "[DiscordMessageBuilder content]" }),
            DateTimeOffset.UtcNow
        )
    );
}
```

**Step 4: Update SendEmbeddedMessage method**

Replace the method body:
```csharp
public async Task SendEmbeddedMessage(MessageDestination destination, DiscordEmbed embed)
{
    _logger.LogInformation("Would send embedded message to {Destination}", destination);
    
    await _actionLogger.LogActionAsync(
        destination.ReplyToMessageId ?? 0,
        new TestAction(
            "embed",
            JsonSerializer.Serialize(new { 
                title = embed.Title, 
                description = embed.Description 
            }),
            DateTimeOffset.UtcNow
        )
    );
}
```

**Step 5: Update React method**

Replace the method body:
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

**Step 6: Verify file compiles**

Run: `dotnet build src/ShitpostBot/src/ShitpostBot.WebApi/ShitpostBot.WebApi.csproj`
Expected: Build succeeds

**Step 7: Commit**

```bash
git add src/ShitpostBot/src/ShitpostBot.WebApi/Services/NullChatClient.cs
git commit -m "refactor: update NullChatClient to use ITestActionLogger"
```

---

## Task 5: Replace SSE Endpoint with Actions Query Endpoint

**Files:**
- Modify: `src/ShitpostBot/src/ShitpostBot.WebApi/Endpoints/TestEndpoints.cs`

**Step 1: Remove SSE endpoint mapping**

Remove this line (currently line 17):
```csharp
group.MapGet("/events", StreamEvents);
```

**Step 2: Add actions query endpoint mapping**

Add this line in the same location:
```csharp
group.MapGet("/actions/{messageId}", GetActions);
```

**Step 3: Remove StreamEvents method**

Delete the entire `StreamEvents` method (lines 66-80).

**Step 4: Add GetActions method**

Add this method where `StreamEvents` was:

```csharp
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

**Step 5: Update using statements**

Remove (if present):
```csharp
using ShitpostBot.WebApi.Services;
```

Add (at the top with other using statements):
```csharp
using System.Diagnostics;
using ShitpostBot.WebApi.Services;
```

**Step 6: Verify file compiles**

Run: `dotnet build src/ShitpostBot/src/ShitpostBot.WebApi/ShitpostBot.WebApi.csproj`
Expected: Build succeeds

**Step 7: Commit**

```bash
git add src/ShitpostBot/src/ShitpostBot.WebApi/Endpoints/TestEndpoints.cs
git commit -m "refactor: replace SSE endpoint with actions query endpoint"
```

---

## Task 6: Delete SSE Infrastructure Files

**Files:**
- Delete: `src/ShitpostBot/src/ShitpostBot.WebApi/Services/ITestEventPublisher.cs`
- Delete: `src/ShitpostBot/src/ShitpostBot.WebApi/Services/TestEventPublisher.cs`

**Step 1: Delete ITestEventPublisher.cs**

Run: `rm src/ShitpostBot/src/ShitpostBot.WebApi/Services/ITestEventPublisher.cs`

**Step 2: Delete TestEventPublisher.cs**

Run: `rm src/ShitpostBot/src/ShitpostBot.WebApi/Services/TestEventPublisher.cs`

**Step 3: Verify build still succeeds**

Run: `dotnet build src/ShitpostBot/src/ShitpostBot.WebApi/ShitpostBot.WebApi.csproj`
Expected: Build succeeds (no references to deleted files)

**Step 4: Commit**

```bash
git add -A
git commit -m "refactor: remove SSE infrastructure (ITestEventPublisher, TestEventPublisher)"
```

---

## Task 7: Update E2E Test File with Machine-Assertable Format

**Files:**
- Modify: `test/e2e-repost-detection.http`

**Step 1: Replace file contents**

Replace the entire file with:

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
### SCENARIO 3: High Similarity - WebP Formats
### ============================================

### 3a. Post WebP original
POST {{host}}/test/image-message
Content-Type: application/json

{
  "imageUrl": "https://media.discordapp.net/attachments/1319991503891861534/1449832201821753567/frenchcat.webp?ex=694054f5&is=693f0375&hm=2d3c0b2ee955a4f22f4ac1f5733e295cd1284f7fd0ab96a3458c1ed601f59e44&=&format=webp&width=918&height=1155"
}

### Expected: {"messageId": 1000005, "tracked": true}

### 3b. Post WebP 50% quality
POST {{host}}/test/image-message
Content-Type: application/json

{
  "imageUrl": "https://media.discordapp.net/attachments/1319991503891861534/1449832201385676992/frenchcat_50.webp?ex=694054f5&is=693f0375&hm=da1d9257e122b9b62b8990629a3ba1c5d02e8833443e090026365841f008021a&=&format=webp&width=900&height=1133"
}

### Expected: {"messageId": 1000006, "tracked": true}

### 3c. Query actions - expect 2 reactions (repost detected)
GET {{host}}/test/actions/1000006?expectedCount=2&timeout=10000

### Expected response:
# {
#   "messageId": 1000006,
#   "actions": [
#     {"type": "reaction", "data": "{\"emoji\":\":police_car:\"}", "timestamp": "..."},
#     {"type": "reaction", "data": "{\"emoji\":\":rotating_light:\"}", "timestamp": "..."}
#   ],
#   "waitedMs": 2500
# }
# PASS if: actions.length == 2 && both are type "reaction"

### ============================================
### NOTES
### ============================================
# - Each GET /test/actions waits for expectedCount or timeout
# - Timeout with partial results allows debugging (see what actions did occur)
# - Tests are machine-assertable: check actions.length and action types
# - Future: Can assert on message content for bot responses
```

**Step 2: Commit**

```bash
git add test/e2e-repost-detection.http
git commit -m "refactor: update E2E test file with machine-assertable format"
```

---

## Task 8: Manual Verification

**Files:**
- N/A (manual testing)

**Step 1: Start all services**

Run: `docker compose -f docker-compose.yml -f docker-compose.Development.Linux.yml up --build`
Expected: All services start (database, ml-service, webapi, worker)

**Step 2: Execute Scenario 1a in Terminal**

Run:
```bash
curl -X POST http://localhost:8080/test/image-message \
  -H "Content-Type: application/json" \
  -d '{
    "imageUrl": "https://media.discordapp.net/attachments/1319991503891861534/1449832200702001183/frenchcat.png?ex=694054f5&is=693f0375&hm=987e474fcc62613908de83ee2db12365472ec3bde4a27525185bbb80184e44e5&=&format=webp&quality=lossless&width=918&height=1155"
  }'
```
Expected: HTTP 200 response with messageId

**Step 3: Execute Scenario 1b**

Run:
```bash
curl -X POST http://localhost:8080/test/image-message \
  -H "Content-Type: application/json" \
  -d '{
    "imageUrl": "https://media.discordapp.net/attachments/1319991503891861534/1449832201049870376/frenchcat.jpg?ex=694054f5&is=693f0375&hm=b8b5a37d8b72b4261dd61781d1ba828285b3ed9e5e00f1e044c69a274e3b05af&=&format=webp&width=918&height=1155"
  }'
```
Expected: HTTP 200 response with messageId (note the messageId for next step)

**Step 4: Query actions for repost**

Run (replace {messageId} with value from previous response):
```bash
curl "http://localhost:8080/test/actions/{messageId}?expectedCount=2&timeout=10000"
```
Expected: JSON response with 2 actions of type "reaction"

**Step 5: Verify response structure**

Expected response format:
```json
{
  "messageId": 1000002,
  "actions": [
    {
      "type": "reaction",
      "data": "{\"emoji\":\":police_car:\"}",
      "timestamp": "2025-12-15T..."
    },
    {
      "type": "reaction",
      "data": "{\"emoji\":\":rotating_light:\"}",
      "timestamp": "2025-12-15T..."
    }
  ],
  "waitedMs": 2341
}
```

**Step 6: Test no-repost scenario**

Run:
```bash
curl -X POST http://localhost:8080/test/image-message \
  -H "Content-Type: application/json" \
  -d '{
    "imageUrl": "https://media.discordapp.net/attachments/1319991503891861534/1449824813462851675/RDT_20251203_1901397855856549389737722.jpg?ex=69404e13&is=693efc93&hm=9295add85964d52e714ff95792e0b6123bdbaa6149982afa93e3ec0d287c69aa&=&format=webp&width=854&height=1155"
  }'
```

Then query actions (should return empty array after timeout since different image won't trigger repost):
```bash
curl "http://localhost:8080/test/actions/{messageId}?expectedCount=0&timeout=5000"
```

Expected: `{"messageId": ..., "actions": [], "waitedMs": 5000}`

**Step 7: Stop services**

Run: `docker compose down`

**Step 8: Document results**

If actions query endpoint returns expected results (2 reactions for reposts, 0 for non-reposts), implementation is complete.

---

## Final Checklist

- ✅ ITestActionLogger interface created
- ✅ TestActionLogger implementation with in-memory logging
- ✅ ITestActionLogger registered in DI (replaced ITestEventPublisher)
- ✅ NullChatClient updated to use ITestActionLogger
- ✅ Actions query endpoint added (replaced SSE endpoint)
- ✅ SSE infrastructure files deleted
- ✅ E2E test file updated with machine-assertable format
- ✅ Manual verification performed

---

## Next Steps

After implementation, the E2E test suite is machine-assertable:
1. Run tests locally after making changes to repost detection
2. Query `/test/actions/{messageId}` to verify expected behavior
3. Assert on action count and types programmatically
4. Future: Write xUnit tests that call these endpoints for automated regression testing
