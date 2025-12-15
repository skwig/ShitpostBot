# E2E Test Suite for Repost Detection - Design Document

**Date:** 2025-12-14  
**Status:** Approved  
**Author:** Design brainstorming session

## Overview

Create a simple end-to-end test suite using the WebApi that uploads sequences of images and verifies repost detection through Server-Sent Events (SSE). This test suite runs locally only (not in CI pipeline) and is used by developers and agents after making substantial changes to repost detection logic.

## Goals

- **Async-aware testing**: Handle asynchronous repost processing via SSE event stream
- **Similarity verification**: Check for approximate similarity ranges (>= 0.95, 0.80-0.95, < 0.80)
- **Local execution only**: Manual testing tool, not automated CI tests
- **Agent-friendly**: Clear documentation for agents to run after substantial changes
- **Reuse existing logic**: Leverage proven code from `RepostMatchAllBotCommandHandler`

## Architecture

### SSE Event Stream

**Endpoint:** `GET /test/events`
- Streams `text/event-stream` content
- Real-time events during test execution
- Keep-alive to maintain connection
- Multiple clients can connect simultaneously

**Event Types:**

1. **`chat-message`** - NullChatClient.SendMessage() called
   ```json
   {
     "type": "chat-message",
     "destination": { "guildId": 123, "channelId": 456, "messageId": 789 },
     "content": "Higher is a closer match (cosine distance):\n1. Match of `0.9987` with ..."
   }
   ```

2. **`chat-reaction`** - NullChatClient.React() called (repost detected)
   ```json
   {
     "type": "chat-reaction",
     "messageId": 444555666,
     "emoji": ":police_car:"
   }
   ```

3. **`chat-embed`** - NullChatClient.SendEmbeddedMessage() called
   ```json
   {
     "type": "chat-embed",
     "destination": { "guildId": 123, "channelId": 456, "messageId": 789 },
     "embedTitle": "...",
     "embedDescription": "..."
   }
   ```

**Key Design Decision:**
No separate query endpoint. All verification happens through SSE stream. When repost detection completes, the existing bot logic (like `RepostMatchAllBotCommandHandler`) already formats similarity results into messages. In test mode, `NullChatClient` streams these messages to SSE instead of sending to Discord.

## Implementation Components

### 1. ITestEventPublisher Service

**Location:** `src/ShitpostBot/src/ShitpostBot.WebApi/Services/ITestEventPublisher.cs`

```csharp
public interface ITestEventPublisher
{
    Task PublishAsync<T>(string eventType, T data);
    IAsyncEnumerable<TestEvent> SubscribeAsync(CancellationToken cancellationToken);
}

public record TestEvent(string Type, string DataJson);
```

**Implementation:** `TestEventPublisher.cs`
- Uses `System.Threading.Channels.Channel<TestEvent>`
- Thread-safe pub/sub pattern
- Supports multiple SSE clients
- Auto-cleanup of disconnected subscribers

### 2. Enhanced NullChatClient

**Location:** `src/ShitpostBot/src/ShitpostBot.WebApi/Services/NullChatClient.cs`

**Changes:**
- Inject `ITestEventPublisher` in constructor
- Override `SendMessage()` → publish `chat-message` event
- Override `React()` → publish `chat-reaction` event
- Override `SendEmbeddedMessage()` → publish `chat-embed` event
- Keep existing logging behavior

**Why this works:**
- `EvaluateRepost_ImagePostTrackedHandler` calls `chatClient.React()` when repost detected (line 70)
- Bot command handlers call `chatClient.SendMessage()` with formatted similarity results
- No changes needed to Application layer - stays Discord-agnostic
- WebApi-specific behavior contained in WebApi project

### 3. SSE Endpoint

**Location:** `src/ShitpostBot/src/ShitpostBot.WebApi/Endpoints/TestEndpoints.cs`

```csharp
group.MapGet("/events", StreamEvents);

private static async Task StreamEvents(
    HttpContext context,
    [FromServices] ITestEventPublisher publisher)
{
    context.Response.Headers.Append("Content-Type", "text/event-stream");
    context.Response.Headers.Append("Cache-Control", "no-cache");
    context.Response.Headers.Append("Connection", "keep-alive");

    await foreach (var testEvent in publisher.SubscribeAsync(context.RequestAborted))
    {
        await context.Response.WriteAsync($"event: {testEvent.Type}\n");
        await context.Response.WriteAsync($"data: {testEvent.DataJson}\n\n");
        await context.Response.Body.FlushAsync();
    }
}
```

**Features:**
- Standard SSE format (`event:` and `data:` fields)
- Graceful handling of client disconnection via `RequestAborted` token
- Auto-flush to ensure real-time delivery

### 4. Dependency Injection Registration

**Location:** `src/ShitpostBot/src/ShitpostBot.WebApi/Program.cs`

```csharp
builder.Services.AddSingleton<ITestEventPublisher, TestEventPublisher>();
builder.Services.AddSingleton<IChatClient, NullChatClient>(); // already exists
```

## Test Suite Structure

### HTTP Test File

**Location:** `/test/e2e-repost-detection.http`

**Structure:**
```http
@host = http://localhost:8080

### SETUP: Open SSE stream in separate terminal/tab
# Terminal 1: curl -N http://localhost:8080/test/events
# Terminal 2: Execute scenarios from this file

### AGENT USAGE INSTRUCTIONS
# After making substantial changes to repost detection:
# 1. Start services: docker compose -f docker-compose.yml -f docker-compose.Development.Linux.yml up --build
# 2. Terminal 1: curl -N http://localhost:8080/test/events
# 3. Terminal 2: Execute scenarios from this file
# 4. Verify SSE events show expected similarity ranges
# 5. Check for chat-reaction events when similarity >= 0.99

### ============================================
### HIGH SIMILARITY - Should Detect as Reposts (>= 0.95)
### ============================================

### Scenario 1: French cat - same image, different formats
POST {{host}}/test/image-message
Content-Type: application/json

{
  "imageUrl": "https://media.discordapp.net/.../frenchcat.png"
}

###
POST {{host}}/test/image-message
Content-Type: application/json

{
  "imageUrl": "https://media.discordapp.net/.../frenchcat.jpg"
}

# Expected SSE events (wait 2-3 seconds):
# - chat-reaction with :police_car:
# - chat-reaction with :rotating_light:
# - (if using bot command) chat-message with similarity >= 0.95

### ============================================
### MEDIUM SIMILARITY - Borderline (0.80 - 0.95)
### ============================================

### Scenario 2: Similar but different images
POST {{host}}/test/image-message
Content-Type: application/json

{
  "imageUrl": "https://example.com/meme-variant-1.jpg"
}

###
POST {{host}}/test/image-message
Content-Type: application/json

{
  "imageUrl": "https://example.com/meme-variant-2.jpg"
}

# Expected SSE events:
# - NO chat-reaction (similarity < 0.99 threshold)
# - (if using bot command) chat-message with similarity between 0.80 and 0.95

### ============================================
### LOW SIMILARITY - Clearly Different (< 0.80)
### ============================================

### Scenario 3: Unrelated images
POST {{host}}/test/image-message
Content-Type: application/json

{
  "imageUrl": "https://example.com/landscape.jpg"
}

###
POST {{host}}/test/image-message
Content-Type: application/json

{
  "imageUrl": "https://example.com/portrait.jpg"
}

# Expected SSE events:
# - NO chat-reaction
# - (if using bot command) chat-message with similarity < 0.80
```

### Test Image Catalog

**Location:** `/test/e2e-test-images.md`

```markdown
# E2E Test Image Catalog

This document catalogs test images for repost detection testing.

## High Similarity Pairs (>= 0.95)

### French Cat (different formats)
- Original PNG: https://media.discordapp.net/...frenchcat.png
- JPEG version: https://media.discordapp.net/...frenchcat.jpg
- WebP version: https://media.discordapp.net/...frenchcat.webp
- WebP 50% quality: https://media.discordapp.net/...frenchcat_50.webp
- Expected: All should match with similarity >= 0.98

## Medium Similarity Pairs (0.80 - 0.95)

### Similar Memes (same template, different text)
- LLMs meme: https://media.discordapp.net/.../RDT_20251204_1703516204720909397219942.jpg
- Obsidian dog: https://media.discordapp.net/.../RDT_20251204_0806282633527631759635038.jpg
- Expected: Similarity between 0.80 and 0.95 (requires validation)

## Low Similarity (< 0.80)

### Unrelated Images
- French snails: https://media.discordapp.net/.../RDT_20251203_1901397855856549389737722.jpg
- Family Guy Bill Gates: https://media.discordapp.net/.../RDT_20250125_1000203858268434480552263.jpg
- Expected: Similarity < 0.80 (requires validation)

## Notes

- URLs are from existing `test/upload-image-message.http`
- Similarity ranges need empirical validation with actual ML service
- Add more pairs as needed based on real-world test results
```

## Test Execution Workflow

### For Developers/Agents

1. **Start all services:**
   ```bash
   docker compose -f docker-compose.yml -f docker-compose.Development.Linux.yml up --build
   ```

2. **Open SSE stream (Terminal 1):**
   ```bash
   curl -N http://localhost:8080/test/events
   ```

3. **Execute HTTP tests (Terminal 2):**
   - Use VS Code REST Client extension with `e2e-repost-detection.http`
   - Or use curl to POST image messages

4. **Observe SSE stream:**
   - Watch for `chat-reaction` events when similarity >= 0.99
   - Watch for `chat-message` events with formatted similarity results
   - Verify similarity scores match expected ranges

5. **Validate results:**
   - High similarity pairs should trigger reactions
   - Medium/low similarity should not trigger reactions
   - Similarity values should be in expected ranges

### Expected Timing

- Image POST returns immediately (200 OK)
- ML feature extraction: ~1-2 seconds
- Repost evaluation: ~0.5 seconds
- Total async processing: ~2-3 seconds
- SSE events appear after processing completes

## Integration with Existing Code

### No Changes Required

- **Application layer**: No modifications needed
- **Domain layer**: No modifications needed
- **Infrastructure layer**: No modifications needed
- **Worker**: No modifications needed

### New Code Only in WebApi

- `ITestEventPublisher` and implementation
- Enhanced `NullChatClient` to publish events
- SSE endpoint in `TestEndpoints.cs`
- DI registration in `Program.cs`

### Reuses Existing Logic

- `EvaluateRepost_ImagePostTrackedHandler` already calls `chatClient.React()`
- Bot command handlers like `RepostMatchAllBotCommandHandler` already format similarity results
- In test mode, these actions flow through `NullChatClient` → `ITestEventPublisher` → SSE stream

## Key Design Decisions

1. **SSE-only verification**: No separate query endpoint, all results stream via SSE
2. **NullChatClient as event source**: Minimal invasive point, WebApi-specific
3. **Reuse bot formatting logic**: Similarity results already formatted by bot commands
4. **Approximate ranges**: Test for >= 0.95, 0.80-0.95, < 0.80 (not exact values)
5. **Remote URLs only**: No local fixtures, use Discord CDN URLs from existing tests
6. **Manual execution only**: Not integrated into CI pipeline
7. **Channel-based pub/sub**: Built-in .NET primitives, no external dependencies

## Non-Goals

- Automated CI integration (local testing only)
- Exact similarity value assertions (approximate ranges)
- Local image fixtures (use remote URLs)
- Query endpoints for polling (SSE streaming only)
- Complex test orchestration (simple HTTP file execution)
- Performance benchmarking (functional testing only)

## Success Criteria

- ✅ SSE endpoint streams events in real-time
- ✅ `NullChatClient` publishes events when methods called
- ✅ Repost detection triggers `chat-reaction` events
- ✅ Similarity results appear in SSE stream
- ✅ HTTP test file documents all scenarios
- ✅ Agents can run test suite after substantial changes
- ✅ No modifications to Application/Domain/Infrastructure layers

## Implementation Plan

### Phase 1: Event Publishing Infrastructure
- Create `ITestEventPublisher` interface
- Implement `TestEventPublisher` with Channel-based pub/sub
- Add DI registration

### Phase 2: NullChatClient Enhancement
- Inject `ITestEventPublisher`
- Publish events from `SendMessage()`, `React()`, `SendEmbeddedMessage()`
- Maintain existing logging

### Phase 3: SSE Endpoint
- Add `GET /test/events` endpoint
- Subscribe to event publisher
- Format as SSE stream
- Handle disconnections

### Phase 4: Test Suite
- Create `/test/e2e-repost-detection.http`
- Create `/test/e2e-test-images.md`
- Document execution workflow
- Add agent usage instructions

### Phase 5: Validation
- Run test scenarios locally
- Verify SSE events appear
- Validate similarity ranges
- Update documentation with empirical results
