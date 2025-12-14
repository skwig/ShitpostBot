# Test API for ShitpostBot - Design Document

**Date:** 2024-12-14  
**Status:** Approved  
**Author:** Design brainstorming session

## Overview

Enable testing of ShitpostBot Worker without Discord connectivity while still using real ML service for image feature extraction and real images (local fixtures or remote URLs).

## Goals

- **End-to-end feature testing**: Test complete workflows like "image posted → repost detected → reactions added" with real ML vectors but simulated Discord messages
- **Local development workflow**: Run the Worker locally and manually trigger scenarios with test images without needing a live Discord server
- Support both local test image fixtures and remote image URLs
- Use real ML service for vector extraction
- Enable both automated E2E tests and manual testing via HTTP API

## Architecture

### High-Level Structure

Create a **3-layer architecture** to enable testing without Discord:

**1. ShitpostBot.Application (NEW)**
- Contains MediatR handlers and application use cases
- Progressively extract repost-related handlers from Worker:
  - `TrackImageMessageHandler` / `TrackLinkMessageHandler`
  - `EvaluateRepost_ImagePostTrackedHandler` / `EvaluateRepost_LinkPostTrackedHandler`
- Becomes the shared business logic layer
- No Discord dependencies, no HTTP dependencies

**2. ShitpostBot.WebApi (NEW)**
- Minimal API endpoints for triggering test scenarios
- Test-only HTTP interface (Development environment)
- Publishes MediatR notifications to trigger Application handlers
- Runs as separate docker-compose service

**3. ShitpostBot.Worker (EXISTING - REFACTORED)**
- Becomes a thin Discord adapter
- References Application project
- Discord events → MediatR notifications → Application handlers
- Keeps Discord-specific handlers (Help, Config, Wumpus, etc.) for now

### Message Flow

**Production (via Worker):**
```
Discord Message → ChatMessageCreatedListener 
  → ImageMessageCreated (MediatR) 
  → TrackImageMessageHandler (Application)
  → ImagePostTracked (MassTransit)
  → EvaluateRepost_ImagePostTrackedHandler (Application)
  → ML Service call + DB query + Discord reactions
```

**Testing (via WebApi):**
```
HTTP POST /test/image-message
  → ImageMessageCreated (MediatR)
  → TrackImageMessageHandler (Application)
  → ImagePostTracked (MassTransit)
  → EvaluateRepost_ImagePostTrackedHandler (Application)
  → ML Service call + DB query + (no Discord reactions - NullChatClient)
```

## API Design

### Minimal API Endpoints

**ShitpostBot.WebApi** exposes these endpoints (Development environment only):

#### `POST /test/image-message`
Simulate an image message being posted to Discord.

**Simple mode (auto-generated IDs):**
```json
{
  "imageUrl": "https://example.com/image.jpg"
}
```
or
```json
{
  "imageUrl": "file://fixtures/reposts/cat1.jpg"
}
```

**Advanced mode (explicit Discord context):**
```json
{
  "imageUrl": "https://example.com/image.jpg",
  "guildId": 123456789,
  "channelId": 987654321,
  "userId": 111222333,
  "messageId": 444555666,
  "timestamp": "2024-01-15T10:30:00Z"
}
```

**Response:**
```json
{
  "messageId": 444555666,
  "tracked": true,
  "imagePostId": "guid-here"
}
```

#### `POST /test/link-message`
Same structure as image-message but for link posts.

#### `GET /test/fixtures`
List available test fixtures.

**Response:**
```json
{
  "reposts": ["cat1.jpg", "cat1-repost.jpg", "meme-original.png", "meme-repost.png"],
  "non-reposts": ["unique1.jpg", "unique2.jpg"],
  "edge-cases": ["too-small.jpg", "corrupted.jpg"]
}
```

### Hybrid Image Support

The WebApi supports both:
- **Remote URLs**: `https://...` or `http://...` - downloads and processes
- **Local fixtures**: `file://fixtures/reposts/cat1.jpg` - resolves from `test/fixtures/images/`

## Project Structure

### ShitpostBot.Application (NEW)

**Location:** `src/ShitpostBot/src/ShitpostBot.Application/`

**Structure:**
```
ShitpostBot.Application/
├── Features/
│   ├── PostTracking/
│   │   ├── ImageMessageCreated.cs (MediatR notification)
│   │   ├── TrackImageMessageHandler.cs
│   │   ├── LinkMessageCreated.cs
│   │   └── TrackLinkMessageHandler.cs
│   └── Repost/
│       ├── EvaluateRepost_ImagePostTrackedHandler.cs
│       └── EvaluateRepost_LinkPostTrackedHandler.cs
├── DependencyInjection.cs
└── ShitpostBot.Application.csproj
```

**Dependencies:**
- ShitpostBot.Domain
- ShitpostBot.Infrastructure
- MediatR
- MassTransit

**Responsibilities:**
- MediatR notification handlers
- MassTransit consumers
- Application use case orchestration
- No Discord dependencies, no HTTP dependencies

### ShitpostBot.WebApi (NEW)

**Location:** `src/ShitpostBot/src/ShitpostBot.WebApi/`

**Structure:**
```
ShitpostBot.WebApi/
├── Endpoints/
│   ├── TestEndpoints.cs (minimal API route definitions)
│   └── FixtureEndpoints.cs
├── Services/
│   ├── TestMessageFactory.cs (generates fake Discord IDs)
│   └── NullChatClient.cs (logs instead of Discord reactions)
├── appsettings.json
├── appsettings.Development.json
├── Dockerfile
├── Program.cs
└── ShitpostBot.WebApi.csproj
```

**Dependencies:**
- ShitpostBot.Application
- ShitpostBot.Infrastructure
- ASP.NET Core Minimal APIs
- MediatR
- MassTransit

**Responsibilities:**
- HTTP endpoint definitions
- Request validation
- ID generation for test scenarios
- File fixture resolution

### Test Fixtures (NEW)

**Location:** `src/ShitpostBot/test/fixtures/images/`

**Structure:**
```
test/fixtures/images/
├── reposts/
│   ├── README.md (explains the repost pairs)
│   ├── cat1.jpg
│   ├── cat1-repost.jpg
│   ├── meme-original.png
│   └── meme-repost.png
├── non-reposts/
│   ├── README.md
│   ├── unique1.jpg
│   └── unique2.jpg
└── edge-cases/
    ├── README.md
    ├── too-small.jpg (< 299x299)
    └── corrupted.jpg
```

### ShitpostBot.Worker (REFACTORED)

**Changes:**
- Remove handlers that moved to Application
- Add reference to ShitpostBot.Application
- Keep Discord-specific features (Help, Config, Wumpus, etc.)
- `ChatMessageCreatedListener` publishes MediatR notifications

### ShitpostBot.Infrastructure (UPDATED)

**New shared MassTransit configuration:**

**File:** `ShitpostBot.Infrastructure/Public/MassTransitConfiguration.cs`

Provides `AddShitpostBotMassTransit()` extension method that:
- Configures PostgreSQL transport (not RabbitMQ or in-memory)
- Sets up CloudEvents serialization
- Allows host to register consumers via callback
- Reused by both Worker and WebApi

## Docker Compose Integration

**Add to `docker-compose.yml`:**
```yaml
webapi:
  build:
    context: ./src/ShitpostBot
    dockerfile: src/ShitpostBot.WebApi/Dockerfile
  ports:
    - "5001:8080"
  environment:
    - ASPNETCORE_ENVIRONMENT=Development
    - ConnectionStrings__DefaultConnection=Host=database;Database=shitpostbot;Username=postgres;Password=postgres
    - ConnectionStrings__ShitpostBotMessaging=Host=database;Database=shitpostbot;Username=postgres;Password=postgres
    - ImageFeatureExtractorApi__Uri=http://ml-service:5000
  depends_on:
    - database
    - ml-service
  networks:
    - shitpostbot
```

**Runtime Behavior:**

Development workflow:
```bash
# Start all services including WebApi
docker compose -f docker-compose.yml -f docker-compose.Development.Linux.yml up --build

# WebApi available at http://localhost:5001
# Test repost detection:
curl -X POST http://localhost:5001/test/image-message \
  -H "Content-Type: application/json" \
  -d '{"imageUrl": "file://fixtures/reposts/cat1.jpg"}'

curl -X POST http://localhost:5001/test/image-message \
  -H "Content-Type: application/json" \
  -d '{"imageUrl": "file://fixtures/reposts/cat1-repost.jpg"}'

# Check logs to see repost detection triggered
docker logs shitpostbot-webapi-1 | grep "Similarity"
```

Production deployment:
- WebApi container is not deployed (omit from production docker-compose or k8s manifests)
- Only Worker runs in production

## Handling Discord Dependencies

### NullChatClient for Testing

When `EvaluateRepost_ImagePostTrackedHandler` detects a repost, it calls `chatClient.React()`. In WebApi testing, there's no Discord connection.

**Solution:** Create `NullChatClient` implementation of `IChatClient` that logs instead of making Discord API calls.

```csharp
public class NullChatClient : IChatClient
{
    public Task React(MessageIdentification id, string emoji)
    {
        _logger.LogInformation("Would react to {MessageId} with {Emoji}", id.MessageId, emoji);
        return Task.CompletedTask;
    }
    // ... other methods similarly no-op or throw NotSupported
}
```

**DI Registration:**
- Worker: Register `DiscordChatClient` (real Discord implementation)
- WebApi: Register `NullChatClient` (logging implementation)

### Future Enhancement: SSE Endpoint

**Planned for future implementation:**
```
GET /test/events (Server-Sent Events endpoint)

Streams real-time events:
- Repost detections
- Reaction attempts (from NullChatClient)
- ML service calls
- Any other bot actions

Enables:
- Real-time feedback during manual testing
- Programmatic verification in E2E tests
- Better debugging experience
```

## Testing Strategy

### Integration Tests

**ShitpostBot.Tests.Integration** will use the WebApi for E2E tests:

**Example test scenario:**
```csharp
[Fact]
public async Task DetectsRepost_WhenSimilarImagePosted()
{
    // Arrange - start WebApi + DB + MlService via Testcontainers
    await _webApiContainer.StartAsync();
    var client = new HttpClient { BaseAddress = _webApiContainer.BaseUri };
    
    // Act - post original image
    var original = await client.PostAsJsonAsync("/test/image-message", new {
        imageUrl = "file://fixtures/reposts/cat1.jpg",
        userId = 12345
    });
    var originalResponse = await original.Content.ReadFromJsonAsync<TestMessageResponse>();
    
    // Act - post repost
    var repost = await client.PostAsJsonAsync("/test/image-message", new {
        imageUrl = "file://fixtures/reposts/cat1-repost.jpg",
        userId = 67890
    });
    var repostResponse = await repost.Content.ReadFromJsonAsync<TestMessageResponse>();
    
    // Assert - check that repost was detected
    var similarity = await CalculateSimilarity(
        originalResponse.ImagePostId, 
        repostResponse.ImagePostId
    );
    similarity.Should().BeGreaterThan(0.95);
}
```

### Local Development Workflow

**Typical development session:**
```bash
# 1. Start services
docker compose -f docker-compose.yml -f docker-compose.Development.Linux.yml up --build

# 2. Browse fixtures
curl http://localhost:5001/test/fixtures

# 3. Test repost detection manually
curl -X POST http://localhost:5001/test/image-message \
  -H "Content-Type: application/json" \
  -d '{"imageUrl": "file://fixtures/reposts/cat1.jpg"}'

curl -X POST http://localhost:5001/test/image-message \
  -H "Content-Type: application/json" \
  -d '{"imageUrl": "file://fixtures/reposts/cat1-repost.jpg"}'

# 4. Check logs for repost detection
docker logs shitpostbot-webapi-1 | grep "Similarity"

# 5. Test with remote image
curl -X POST http://localhost:5001/test/image-message \
  -H "Content-Type: application/json" \
  -d '{"imageUrl": "https://media.discordapp.net/attachments/.../image.jpg"}'
```

## Implementation Phases

### Phase 1: Application Layer
- Create ShitpostBot.Application project
- Extract repost-related handlers from Worker
- Extract shared MassTransit configuration to Infrastructure
- Update Worker to reference Application

### Phase 2: WebApi Project
- Create ShitpostBot.WebApi project
- Implement minimal API endpoints
- Create NullChatClient
- Add docker-compose service

### Phase 3: Test Fixtures
- Create test/fixtures/images directory structure
- Add initial test image pairs (reposts/non-reposts)
- Add README files explaining fixtures

### Phase 4: Integration Tests
- Update ShitpostBot.Tests.Integration
- Add E2E tests using WebApi
- Verify repost detection workflow

## Key Design Decisions

1. **Progressive extraction**: Start with repost handlers, migrate other features later
2. **Minimal APIs**: Modern, lightweight approach (not controllers)
3. **PostgreSQL for MassTransit**: Keep existing transport, shared configuration in Infrastructure
4. **Hybrid image support**: Both local fixtures and remote URLs
5. **Categorized test fixtures**: Self-documenting organization by scenario
6. **Separate docker-compose service**: Clean separation, easy to exclude from production
7. **NullChatClient pattern**: Simple mock for test environment, extensible to SSE in future
8. **Development-only scope**: Test endpoints only, not production API features

## Non-Goals

- Full-featured read/query API (search posts, view reposts, etc.)
- Production HTTP interface to the bot
- Refactoring all Worker features upfront (only repost detection initially)
- Webhook receivers or external integrations
- Authentication/authorization (Development environment only)

## Success Criteria

- ✅ Can trigger image repost detection via HTTP POST without Discord
- ✅ Real ML service extracts feature vectors
- ✅ Works with both local test fixtures and remote URLs
- ✅ E2E tests verify complete repost workflow
- ✅ Local development workflow via curl/Postman
- ✅ Worker and WebApi share Application layer code
- ✅ No Discord connection required for testing
