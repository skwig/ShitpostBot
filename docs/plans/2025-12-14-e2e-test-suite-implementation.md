# E2E Test Suite Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Create an SSE-based end-to-end test suite for repost detection that streams real-time events during async processing.

**Architecture:** Enhance NullChatClient to publish events to an ITestEventPublisher service, add SSE endpoint to stream events, create HTTP test file with image sequences organized by similarity ranges.

**Tech Stack:** ASP.NET Core Minimal APIs, System.Threading.Channels, Server-Sent Events, HTTP test files

---

## Task 1: Create ITestEventPublisher Service Interface

**Files:**
- Create: `src/ShitpostBot/src/ShitpostBot.WebApi/Services/ITestEventPublisher.cs`

**Step 1: Write the interface and record types**

Create the file with:

```csharp
namespace ShitpostBot.WebApi.Services;

public interface ITestEventPublisher
{
    Task PublishAsync<T>(string eventType, T data);
    IAsyncEnumerable<TestEvent> SubscribeAsync(CancellationToken cancellationToken);
}

public record TestEvent(string Type, string DataJson);
```

**Step 2: Verify file compiles**

Run: `dotnet build src/ShitpostBot/src/ShitpostBot.WebApi/ShitpostBot.WebApi.csproj`
Expected: Build succeeds

**Step 3: Commit**

```bash
git add src/ShitpostBot/src/ShitpostBot.WebApi/Services/ITestEventPublisher.cs
git commit -m "feat: add ITestEventPublisher interface for SSE events"
```

---

## Task 2: Implement TestEventPublisher with Channel-based Pub/Sub

**Files:**
- Create: `src/ShitpostBot/src/ShitpostBot.WebApi/Services/TestEventPublisher.cs`

**Step 1: Write the implementation**

Create the file with:

```csharp
using System.Text.Json;
using System.Threading.Channels;

namespace ShitpostBot.WebApi.Services;

public class TestEventPublisher : ITestEventPublisher
{
    private readonly Channel<TestEvent> _channel;
    private readonly ILogger<TestEventPublisher> _logger;

    public TestEventPublisher(ILogger<TestEventPublisher> logger)
    {
        _logger = logger;
        _channel = Channel.CreateUnbounded<TestEvent>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });
    }

    public async Task PublishAsync<T>(string eventType, T data)
    {
        var dataJson = JsonSerializer.Serialize(data);
        var testEvent = new TestEvent(eventType, dataJson);
        
        _logger.LogDebug("Publishing event: {EventType}", eventType);
        
        await _channel.Writer.WriteAsync(testEvent);
    }

    public async IAsyncEnumerable<TestEvent> SubscribeAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("New SSE subscriber connected");
        
        try
        {
            await foreach (var testEvent in _channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return testEvent;
            }
        }
        finally
        {
            _logger.LogInformation("SSE subscriber disconnected");
        }
    }
}
```

**Step 2: Verify file compiles**

Run: `dotnet build src/ShitpostBot/src/ShitpostBot.WebApi/ShitpostBot.WebApi.csproj`
Expected: Build succeeds

**Step 3: Commit**

```bash
git add src/ShitpostBot/src/ShitpostBot.WebApi/Services/TestEventPublisher.cs
git commit -m "feat: implement TestEventPublisher with Channel-based pub/sub"
```

---

## Task 3: Register ITestEventPublisher in DI Container

**Files:**
- Modify: `src/ShitpostBot/src/ShitpostBot.WebApi/Program.cs`

**Step 1: Add ITestEventPublisher registration**

After line 16 (before `var app = builder.Build();`), add:

```csharp
builder.Services.AddSingleton<ITestEventPublisher, TestEventPublisher>();
```

The section should look like:

```csharp
builder.Services.AddShitpostBotInfrastructure(builder.Configuration);
builder.Services.AddShitpostBotApplication(builder.Configuration);
builder.Services.AddShitpostBotMassTransit(builder.Configuration, x =>
{
    x.AddConsumer<EvaluateRepost_ImagePostTrackedHandler>();
    x.AddConsumer<EvaluateRepost_LinkPostTrackedHandler>();
});
builder.Services.AddSingleton<IChatClient, NullChatClient>();
builder.Services.AddSingleton<TestMessageFactory>();
builder.Services.AddSingleton<ITestEventPublisher, TestEventPublisher>();

var app = builder.Build();
```

**Step 2: Verify file compiles**

Run: `dotnet build src/ShitpostBot/src/ShitpostBot.WebApi/ShitpostBot.WebApi.csproj`
Expected: Build succeeds

**Step 3: Commit**

```bash
git add src/ShitpostBot/src/ShitpostBot.WebApi/Program.cs
git commit -m "feat: register ITestEventPublisher in DI container"
```

---

## Task 4: Enhance NullChatClient to Publish Events

**Files:**
- Modify: `src/ShitpostBot/src/ShitpostBot.WebApi/Services/NullChatClient.cs`

**Step 1: Add ITestEventPublisher dependency**

Replace the constructor and add field:

```csharp
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.WebApi.Services;

public class NullChatClient : IChatClient
{
    private readonly ILogger<NullChatClient> _logger;
    private readonly ITestEventPublisher _eventPublisher;

    public NullChatClient(ILogger<NullChatClient> logger, ITestEventPublisher eventPublisher)
    {
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    public IChatClientUtils Utils { get; } = new NullChatClientUtils();

    public event AsyncEventHandler<MessageCreateEventArgs>? MessageCreated;
    public event AsyncEventHandler<MessageDeleteEventArgs>? MessageDeleted;

    public Task ConnectAsync()
    {
        _logger.LogInformation("NullChatClient.ConnectAsync - no-op");
        return Task.CompletedTask;
    }

    public async Task SendMessage(MessageDestination destination, string? messageContent)
    {
        _logger.LogInformation("Would send message to {Destination}: {Content}", destination, messageContent);
        
        await _eventPublisher.PublishAsync("chat-message", new
        {
            destination = new
            {
                guildId = destination.GuildId,
                channelId = destination.ChannelId,
                messageId = destination.MessageId
            },
            content = messageContent
        });
    }

    public async Task SendMessage(MessageDestination destination, DiscordMessageBuilder messageBuilder)
    {
        _logger.LogInformation("Would send message builder to {Destination}", destination);
        
        await _eventPublisher.PublishAsync("chat-message", new
        {
            destination = new
            {
                guildId = destination.GuildId,
                channelId = destination.ChannelId,
                messageId = destination.MessageId
            },
            content = "[DiscordMessageBuilder content]"
        });
    }

    public async Task SendEmbeddedMessage(MessageDestination destination, DiscordEmbed embed)
    {
        _logger.LogInformation("Would send embedded message to {Destination}", destination);
        
        await _eventPublisher.PublishAsync("chat-embed", new
        {
            destination = new
            {
                guildId = destination.GuildId,
                channelId = destination.ChannelId,
                messageId = destination.MessageId
            },
            embedTitle = embed.Title,
            embedDescription = embed.Description
        });
    }

    public async Task React(MessageIdentification messageIdentification, string emoji)
    {
        _logger.LogInformation("Would react to message {MessageId} with {Emoji}", 
            messageIdentification.MessageId, emoji);
        
        await _eventPublisher.PublishAsync("chat-reaction", new
        {
            messageId = messageIdentification.MessageId,
            emoji = emoji
        });
    }
}

public class NullChatClientUtils : IChatClientUtils
{
    public string Emoji(string name) => $":{name}:";
    public ulong ShitpostBotId() => 0;
    public string Mention(ulong posterId, bool useDesktop = false) => $"<@{posterId}>";
    public string RelativeTimestamp(DateTimeOffset timestamp) => timestamp.ToString("R");
}
```

**Step 2: Verify file compiles**

Run: `dotnet build src/ShitpostBot/src/ShitpostBot.WebApi/ShitpostBot.WebApi.csproj`
Expected: Build succeeds

**Step 3: Commit**

```bash
git add src/ShitpostBot/src/ShitpostBot.WebApi/Services/NullChatClient.cs
git commit -m "feat: enhance NullChatClient to publish SSE events"
```

---

## Task 5: Add SSE Endpoint to TestEndpoints

**Files:**
- Modify: `src/ShitpostBot/src/ShitpostBot.WebApi/Endpoints/TestEndpoints.cs`

**Step 1: Add SSE endpoint mapping**

After line 15 (after `group.MapPost("/link-message", PostLinkMessage);`), add:

```csharp
        group.MapGet("/events", StreamEvents);
```

**Step 2: Add StreamEvents method**

At the end of the class (before the closing brace of `TestEndpoints`), add:

```csharp
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

**Step 3: Add using statement**

At the top of the file, add:

```csharp
using ShitpostBot.WebApi.Services;
```

The full using block should be:

```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;
using ShitpostBot.Application.Features.PostTracking;
using ShitpostBot.WebApi.Services;
```

**Step 4: Verify file compiles**

Run: `dotnet build src/ShitpostBot/src/ShitpostBot.WebApi/ShitpostBot.WebApi.csproj`
Expected: Build succeeds

**Step 5: Commit**

```bash
git add src/ShitpostBot/src/ShitpostBot.WebApi/Endpoints/TestEndpoints.cs
git commit -m "feat: add SSE endpoint for streaming test events"
```

---

## Task 6: Create Test Image Catalog Documentation

**Files:**
- Create: `test/e2e-test-images.md`

**Step 1: Create the catalog file**

```markdown
# E2E Test Image Catalog

This document catalogs test images for repost detection testing.

## High Similarity Pairs (>= 0.95)

### French Cat (different formats)
- Original PNG: https://media.discordapp.net/attachments/1319991503891861534/1449832200702001183/frenchcat.png?ex=694054f5&is=693f0375&hm=987e474fcc62613908de83ee2db12365472ec3bde4a27525185bbb80184e44e5&=&format=webp&quality=lossless&width=918&height=1155
- JPEG version: https://media.discordapp.net/attachments/1319991503891861534/1449832201049870376/frenchcat.jpg?ex=694054f5&is=693f0375&hm=b8b5a37d8b72b4261dd61781d1ba828285b3ed9e5e00f1e044c69a274e3b05af&=&format=webp&width=918&height=1155
- WebP version: https://media.discordapp.net/attachments/1319991503891861534/1449832201821753567/frenchcat.webp?ex=694054f5&is=693f0375&hm=2d3c0b2ee955a4f22f4ac1f5733e295cd1284f7fd0ab96a3458c1ed601f59e44&=&format=webp&width=918&height=1155
- WebP 50% quality: https://media.discordapp.net/attachments/1319991503891861534/1449832201385676992/frenchcat_50.webp?ex=694054f5&is=693f0375&hm=da1d9257e122b9b62b8990629a3ba1c5d02e8833443e090026365841f008021a&=&format=webp&width=900&height=1133
- Expected: All should match with similarity >= 0.98

## Medium Similarity Pairs (0.80 - 0.95)

### Similar Memes (same template, different text)
- LLMs meme: https://media.discordapp.net/attachments/1319991503891861534/1449824811017572372/RDT_20251204_1703516204720909397219942.jpg?ex=69404e13&is=693efc93&hm=304eabce7576e515e2bcd7d1cc77ea6744febbce860773ee624615ffb2f9ed5d&=&format=webp&width=1210&height=1155
- Obsidian dog: https://media.discordapp.net/attachments/1319991503891861534/1449824812309414089/RDT_20251204_0806282633527631759635038.jpg?ex=69404e13&is=693efc93&hm=aec59f299be7616c28d3f3f146cc4121ecb44473e8b9bc769077b217b19abf79&=&format=webp&width=800&height=1054
- Expected: Similarity between 0.80 and 0.95 (requires validation)

## Low Similarity (< 0.80)

### Unrelated Images
- French snails: https://media.discordapp.net/attachments/1319991503891861534/1449824813462851675/RDT_20251203_1901397855856549389737722.jpg?ex=69404e13&is=693efc93&hm=9295add85964d52e714ff95792e0b6123bdbaa6149982afa93e3ec0d287c69aa&=&format=webp&width=854&height=1155
- Family Guy Bill Gates: https://media.discordapp.net/attachments/1319991503891861534/1449825108611825915/RDT_20250125_1000203858268434480552263.jpg?ex=69404e5a&is=693efcda&hm=01d74376049da7f5b1bb16484c9b7dfa7990f7ef28b2155670f1941e69e827e3&=&format=webp&width=1329&height=1155
- Expected: Similarity < 0.80 (requires validation)

## Notes

- URLs are from existing `test/upload-image-message.http`
- Similarity ranges need empirical validation with actual ML service
- Add more pairs as needed based on real-world test results
- Discord CDN URLs may expire - update as needed
```

**Step 2: Commit**

```bash
git add test/e2e-test-images.md
git commit -m "docs: add E2E test image catalog"
```

---

## Task 7: Create E2E HTTP Test File

**Files:**
- Create: `test/e2e-repost-detection.http`

**Step 1: Create the HTTP test file**

```http
@host = http://localhost:8080

### SETUP: Open SSE stream in separate terminal/tab
# Terminal 1: curl -N http://localhost:8080/test/events
# Terminal 2: Execute scenarios from this file

### AGENT USAGE INSTRUCTIONS
# After making substantial changes to repost detection:
# 1. Start services: docker compose -f docker-compose.yml -f docker-compose.Development.Linux.yml up --build
# 2. Terminal 1: curl -N http://localhost:8080/test/events
# 3. Terminal 2: Execute scenarios from this file (via VS Code REST Client or similar)
# 4. Verify SSE events show expected similarity ranges
# 5. Check for chat-reaction events when similarity >= 0.99
# 6. Wait 2-3 seconds between requests for async processing to complete

### ============================================
### HIGH SIMILARITY - Should Detect as Reposts (>= 0.95)
### ============================================

### Scenario 1a: French cat - PNG original
POST {{host}}/test/image-message
Content-Type: application/json

{
  "imageUrl": "https://media.discordapp.net/attachments/1319991503891861534/1449832200702001183/frenchcat.png?ex=694054f5&is=693f0375&hm=987e474fcc62613908de83ee2db12365472ec3bde4a27525185bbb80184e44e5&=&format=webp&quality=lossless&width=918&height=1155"
}

### Scenario 1b: French cat - JPG version (WAIT 3 seconds after 1a)
POST {{host}}/test/image-message
Content-Type: application/json

{
  "imageUrl": "https://media.discordapp.net/attachments/1319991503891861534/1449832201049870376/frenchcat.jpg?ex=694054f5&is=693f0375&hm=b8b5a37d8b72b4261dd61781d1ba828285b3ed9e5e00f1e044c69a274e3b05af&=&format=webp&width=918&height=1155"
}

# Expected SSE events (watch Terminal 1):
# - chat-reaction with :police_car:
# - chat-reaction with :rotating_light:
# Expected: Similarity >= 0.95 (likely ~0.99)

### Scenario 2a: French cat - WebP original
POST {{host}}/test/image-message
Content-Type: application/json

{
  "imageUrl": "https://media.discordapp.net/attachments/1319991503891861534/1449832201821753567/frenchcat.webp?ex=694054f5&is=693f0375&hm=2d3c0b2ee955a4f22f4ac1f5733e295cd1284f7fd0ab96a3458c1ed601f59e44&=&format=webp&width=918&height=1155"
}

### Scenario 2b: French cat - WebP 50% quality (WAIT 3 seconds after 2a)
POST {{host}}/test/image-message
Content-Type: application/json

{
  "imageUrl": "https://media.discordapp.net/attachments/1319991503891861534/1449832201385676992/frenchcat_50.webp?ex=694054f5&is=693f0375&hm=da1d9257e122b9b62b8990629a3ba1c5d02e8833443e090026365841f008021a&=&format=webp&width=900&height=1133"
}

# Expected SSE events:
# - chat-reaction with :police_car:
# - chat-reaction with :rotating_light:
# Expected: Similarity >= 0.95

### ============================================
### MEDIUM SIMILARITY - Borderline (0.80 - 0.95)
### ============================================

### Scenario 3a: LLMs meme
POST {{host}}/test/image-message
Content-Type: application/json

{
  "imageUrl": "https://media.discordapp.net/attachments/1319991503891861534/1449824811017572372/RDT_20251204_1703516204720909397219942.jpg?ex=69404e13&is=693efc93&hm=304eabce7576e515e2bcd7d1cc77ea6744febbce860773ee624615ffb2f9ed5d&=&format=webp&width=1210&height=1155"
}

### Scenario 3b: Obsidian dog (WAIT 3 seconds after 3a)
POST {{host}}/test/image-message
Content-Type: application/json

{
  "imageUrl": "https://media.discordapp.net/attachments/1319991503891861534/1449824812309414089/RDT_20251204_0806282633527631759635038.jpg?ex=69404e13&is=693efc93&hm=aec59f299be7616c28d3f3f146cc4121ecb44473e8b9bc769077b217b19abf79&=&format=webp&width=800&height=1054"
}

# Expected SSE events:
# - NO chat-reaction (similarity < 0.99 threshold)
# Expected: Similarity between 0.80 and 0.95 (requires empirical validation)

### ============================================
### LOW SIMILARITY - Clearly Different (< 0.80)
### ============================================

### Scenario 4a: French snails
POST {{host}}/test/image-message
Content-Type: application/json

{
  "imageUrl": "https://media.discordapp.net/attachments/1319991503891861534/1449824813462851675/RDT_20251203_1901397855856549389737722.jpg?ex=69404e13&is=693efc93&hm=9295add85964d52e714ff95792e0b6123bdbaa6149982afa93e3ec0d287c69aa&=&format=webp&width=854&height=1155"
}

### Scenario 4b: Family Guy Bill Gates (WAIT 3 seconds after 4a)
POST {{host}}/test/image-message
Content-Type: application/json

{
  "imageUrl": "https://media.discordapp.net/attachments/1319991503891861534/1449825108611825915/RDT_20250125_1000203858268434480552263.jpg?ex=69404e5a&is=693efcda&hm=01d74376049da7f5b1bb16484c9b7dfa7990f7ef28b2155670f1941e69e827e3&=&format=webp&width=1329&height=1155"
}

# Expected SSE events:
# - NO chat-reaction
# Expected: Similarity < 0.80 (requires empirical validation)

### ============================================
### NOTES
### ============================================
# - Run SSE stream first: curl -N http://localhost:8080/test/events
# - Execute scenarios one pair at a time
# - Wait 2-3 seconds between pairs for async processing
# - Watch SSE stream for chat-reaction events
# - Similarity ranges are estimates and need validation with actual ML service
# - URLs may expire - see test/e2e-test-images.md for updates
```

**Step 2: Commit**

```bash
git add test/e2e-repost-detection.http
git commit -m "feat: add E2E HTTP test file for repost detection"
```

---

## Task 8: Manual Verification

**Files:**
- N/A (manual testing)

**Step 1: Start all services**

Run: `docker compose -f docker-compose.yml -f docker-compose.Development.Linux.yml up --build`
Expected: All services start (database, ml-service, webapi, worker)

**Step 2: Open SSE stream in Terminal 1**

Run: `curl -N http://localhost:8080/test/events`
Expected: Connection established, waiting for events

**Step 3: Execute Scenario 1a in Terminal 2**

Run:
```bash
curl -X POST http://localhost:8080/test/image-message \
  -H "Content-Type: application/json" \
  -d '{
    "imageUrl": "https://media.discordapp.net/attachments/1319991503891861534/1449832200702001183/frenchcat.png?ex=694054f5&is=693f0375&hm=987e474fcc62613908de83ee2db12365472ec3bde4a27525185bbb80184e44e5&=&format=webp&quality=lossless&width=918&height=1155"
  }'
```
Expected: HTTP 200 response

**Step 4: Wait 3 seconds, then execute Scenario 1b**

Run:
```bash
curl -X POST http://localhost:8080/test/image-message \
  -H "Content-Type: application/json" \
  -d '{
    "imageUrl": "https://media.discordapp.net/attachments/1319991503891861534/1449832201049870376/frenchcat.jpg?ex=694054f5&is=693f0375&hm=b8b5a37d8b72b4261dd61781d1ba828285b3ed9e5e00f1e044c69a274e3b05af&=&format=webp&width=918&height=1155"
  }'
```
Expected: HTTP 200 response

**Step 5: Check SSE stream in Terminal 1**

Expected events:
- `event: chat-reaction` with `emoji: ":police_car:"`
- `event: chat-reaction` with `emoji: ":rotating_light:"`

**Step 6: Stop services**

Run: `docker compose down`

**Step 7: Document results**

If SSE events appeared correctly, the implementation is complete. If not, review logs for errors.

---

## Final Checklist

- ✅ ITestEventPublisher interface created
- ✅ TestEventPublisher implementation with Channel-based pub/sub
- ✅ ITestEventPublisher registered in DI
- ✅ NullChatClient enhanced to publish events
- ✅ SSE endpoint added to TestEndpoints
- ✅ Test image catalog documented
- ✅ E2E HTTP test file created
- ✅ Manual verification performed

---

## Next Steps

After implementation, agents should:
1. Run the E2E test suite after making substantial changes to repost detection
2. Update `test/e2e-test-images.md` with empirical similarity ranges
3. Add new test scenarios as edge cases are discovered
4. Keep Discord CDN URLs updated if they expire
