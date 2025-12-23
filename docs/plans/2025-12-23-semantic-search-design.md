# Semantic Image Search Using Text Embeddings

**Date**: 2025-12-23  
**Status**: Design Complete

## Problem

Users want to search for images in the Discord channel using natural language queries (e.g., "cats on couches", "memes about programming"). Currently, the bot only supports image-to-image similarity search via `repost match all`, which requires replying to an existing image.

## Solution Overview

Add a new bot command `search <query>` that:
1. Converts text query to CLIP embedding using the ML service
2. Searches all tracked images using pgvector cosine similarity
3. Returns top 5 results formatted like `repost match all`
4. Indicates low confidence when best match < 0.8

This leverages existing infrastructure:
- ML service already has `/embed/text` endpoint with CLIP text embeddings
- Database already uses pgvector for image feature vectors
- Bot command system auto-discovers new handlers via DI

## Design Details

### 1. ML Service Integration

#### Extend IImageFeatureExtractorApi

Location: `ShitpostBot.Infrastructure/Public/Services/IImageFeatureExtractorApi.cs`

Add method:
```csharp
[Post("/embed/text")]
Task<IApiResponse<TextEmbedResponse>> EmbedTextAsync([Body] TextEmbedRequest request);
```

Add models:
```csharp
public record TextEmbedRequest
{
    [JsonPropertyName("text")] 
    public required string Text { get; init; }
}

public record TextEmbedResponse
{
    [JsonPropertyName("embedding")] 
    public required float[] Embedding { get; init; }
}
```

### 2. Database Query Extension

#### New Query Method

Location: `ShitpostBot.Infrastructure/Public/Extensions/ImagePostQueryExtensions.cs`

Add method:
```csharp
public IQueryable<ClosestToImagePost> ClosestToTextEmbedding(Vector textEmbedding)
{
    return query
        .Where(x => x.Image.ImageFeatures != null)
        .OrderBy(x => x.Image.ImageFeatures!.FeatureVector.CosineDistance(textEmbedding))
        .ThenBy(x => x.PostedOn)
        .Select(x => new ClosestToImagePost(
            x.Id,
            x.PostedOn,
            new ChatMessageIdentifier(x.ChatGuildId, x.ChatChannelId, x.ChatMessageId),
            new PosterIdentifier(x.PosterId),
            x.Image.ImageFeatures!.FeatureVector.L2Distance(textEmbedding),
            x.Image.ImageFeatures!.FeatureVector.CosineDistance(textEmbedding)));
}
```

**Key differences from image-to-image search:**
- No `postedOnBefore` filter (search all images, not just earlier ones)
- Filter `ImageFeatures != null` (skip images without embeddings)
- Use cosine distance ordering only

### 3. Bot Command Handler

#### New Handler Implementation

Location: `ShitpostBot.Application/Features/BotCommands/Search/SearchBotCommandHandler.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Pgvector;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.BotCommands.Search;

public class SearchBotCommandHandler(
    IDbContext dbContext,
    IChatClient chatClient,
    IImageFeatureExtractorApi mlService)
    : IBotCommandHandler
{
    private const decimal LowConfidenceThreshold = 0.8m;
    private const int ResultLimit = 5;

    public string? GetHelpMessage() => 
        "`search <query>` - search for images using natural language";

    public async Task<bool> TryHandle(
        MessageIdentification commandMessageIdentification,
        MessageIdentification? referencedMessageIdentification,
        BotCommand command)
    {
        // Only handle "search <query>" commands
        if (!command.Command.StartsWith("search ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var messageDestination = new MessageDestination(
            commandMessageIdentification.GuildId,
            commandMessageIdentification.ChannelId,
            commandMessageIdentification.MessageId
        );

        // Extract query text after "search "
        var query = command.Command.Substring(7).Trim();
        
        if (string.IsNullOrWhiteSpace(query))
        {
            await chatClient.SendMessage(
                messageDestination,
                "Invalid usage: search query cannot be empty"
            );
            return true;
        }

        await chatClient.SendMessage(
            messageDestination, 
            $"Searching for: \"{query}\" {chatClient.Utils.Emoji(":PauseChamp:")} ..."
        );

        // Get text embedding from ML service
        var embedResponse = await mlService.EmbedTextAsync(new TextEmbedRequest { Text = query });
        
        if (!embedResponse.IsSuccessful)
        {
            throw embedResponse.Error ?? new Exception("Failed to generate text embedding");
        }

        var textEmbedding = new Vector(embedResponse.Content.Embedding);

        // Query database for similar images
        var similarPosts = await dbContext.ImagePost
            .AsNoTracking()
            .ClosestToTextEmbedding(textEmbedding)
            .Take(ResultLimit)
            .ToListAsync();

        if (similarPosts.Count == 0)
        {
            await chatClient.SendMessage(
                messageDestination,
                "No images available to search"
            );
            return true;
        }

        // Determine if results are low confidence
        var bestMatch = similarPosts.First();
        var isLowConfidence = bestMatch.CosineSimilarity < (double)LowConfidenceThreshold;
        
        var headerMessage = isLowConfidence
            ? "Best matches found (low confidence):\nHigher is a closer match:\n"
            : "Higher is a closer match:\n";

        var resultsMessage = string.Join("\n",
            similarPosts.Select((p, i) =>
                $"{i + 1}. Match of `{p.CosineSimilarity:0.00000000}` with {p.ChatMessageIdentifier.GetUri()} posted {chatClient.Utils.RelativeTimestamp(p.PostedOn)}"
            )
        );

        await chatClient.SendMessage(
            messageDestination,
            headerMessage + resultsMessage
        );

        return true;
    }
}
```

### 4. Command Behavior

#### Command Format
- **Syntax**: `@ShitpostBot search <query text>`
- **Example**: `@ShitpostBot search cats on couches`
- **Query extraction**: Everything after "search " (case-insensitive)

#### Output Format

**Standard results** (best match >= 0.8):
```
Higher is a closer match:
1. Match of `0.87654321` with https://discord.com/... posted 2 hours ago
2. Match of `0.82345678` with https://discord.com/... posted 1 day ago
...
```

**Low confidence results** (best match < 0.8):
```
Best matches found (low confidence):
Higher is a closer match:
1. Match of `0.65432100` with https://discord.com/... posted 3 days ago
2. Match of `0.54321098` with https://discord.com/... posted 1 week ago
...
```

#### Error Messages
- **Empty query**: "Invalid usage: search query cannot be empty"
- **No images**: "No images available to search"
- **ML service error**: Re-thrown (caught by ExecuteBotCommand error handler)

## Edge Cases Handled

1. **Empty/whitespace query** → Error message
2. **No images with embeddings** → "No images available to search"
3. **ML service unavailable** → Existing ExecuteBotCommand error handling
4. **Case sensitivity** → Command uses case-insensitive matching
5. **Leading/trailing whitespace** → Trimmed from query

## Testing Strategy

### Integration Tests

Location: `ShitpostBot.Tests.Integration/WebApiIntegrationTests.cs`

Add test:
```csharp
[Test]
public async Task SemanticSearch_ReturnsRelevantImages()
{
    // Post test images with different content
    var catImageResponse = await PostImageMessage("https://example.com/cat.jpg");
    var dogImageResponse = await PostImageMessage("https://example.com/dog.jpg");
    
    // Wait for ML processing
    await WaitForImageProcessing(catImageResponse.MessageId);
    await WaitForImageProcessing(dogImageResponse.MessageId);
    
    // Execute search command
    var searchResponse = await PostBotCommand("search cat");
    
    // Verify results
    var actions = await GetActions(searchResponse.MessageId, expectedCount: 2);
    var resultMessage = actions.Last().Content;
    
    resultMessage.Should().Contain("Higher is a closer match");
    resultMessage.Should().Contain("Match of");
    resultMessage.Should().Contain(catImageResponse.MessageId.ToString());
}

[Test]
public async Task SemanticSearch_EmptyQuery_ReturnsError()
{
    var searchResponse = await PostBotCommand("search   ");
    
    var actions = await GetActions(searchResponse.MessageId, expectedCount: 1);
    actions.Last().Content.Should().Contain("search query cannot be empty");
}
```

### E2E Tests

Location: `test/e2e/scenarios/scenario-5-semantic-search.http`

```http
### Scenario 5: Semantic Search

### Post sample images
POST {{baseUrl}}/test/image-message
Content-Type: application/json

{
  "imageUrl": "http://sample-data/frenchcat.jpg"
}

### Post another image
POST {{baseUrl}}/test/image-message
Content-Type: application/json

{
  "imageUrl": "http://sample-data/obsidianslop.webp"
}

### Wait for processing (manual delay)

### Search for "cat"
POST {{baseUrl}}/test/bot-command
Content-Type: application/json

{
  "command": "search cat"
}

### Verify results contain expected format
GET {{baseUrl}}/test/actions/{{messageId}}?expectedCount=2

### Search with low relevance query
POST {{baseUrl}}/test/bot-command
Content-Type: application/json

{
  "command": "search completely unrelated query xyz123"
}

### Test empty query
POST {{baseUrl}}/test/bot-command
Content-Type: application/json

{
  "command": "search   "
}
```

### Manual Testing

1. Deploy to development environment
2. Post variety of images (cats, memes, screenshots, etc.)
3. Test queries:
   - "cat" → Should find cat images
   - "meme" → Should find memes
   - "text" → Should find images with text
   - "random gibberish xyz" → Should show low confidence
4. Verify output format matches `repost match all`

## Performance Characteristics

- **ML Service**: Text embedding is fast (~50-100ms for CLIP text encoder)
- **Database**: pgvector cosine distance with LIMIT 5 is O(n) but fast with indexes
- **Expected latency**: 100-500ms total for typical query
- **No pagination needed**: Always returns fixed 5 results

## Files to Create

1. `ShitpostBot.Application/Features/BotCommands/Search/SearchBotCommandHandler.cs`
2. E2E test: `test/e2e/scenarios/scenario-5-semantic-search.http`

## Files to Modify

1. `ShitpostBot.Infrastructure/Public/Services/IImageFeatureExtractorApi.cs` - Add `EmbedTextAsync` + models
2. `ShitpostBot.Infrastructure/Public/Extensions/ImagePostQueryExtensions.cs` - Add `ClosestToTextEmbedding`
3. `ShitpostBot.Tests.Integration/WebApiIntegrationTests.cs` - Add integration tests

## Implementation Checklist

**Phase 1: Infrastructure**
- [ ] Add `TextEmbedRequest` and `TextEmbedResponse` models to `IImageFeatureExtractorApi.cs`
- [ ] Add `EmbedTextAsync` method to `IImageFeatureExtractorApi`
- [ ] Add `ClosestToTextEmbedding` extension method to `ImagePostQueryExtensions.cs`

**Phase 2: Application**
- [ ] Create `SearchBotCommandHandler.cs`
- [ ] Verify auto-registration via DI

**Phase 3: Testing**
- [ ] Add integration tests to `WebApiIntegrationTests.cs`
- [ ] Create E2E scenario file `scenario-5-semantic-search.http`
- [ ] Run tests and verify output format

**Phase 4: Validation**
- [ ] Build solution
- [ ] Run unit tests
- [ ] Run integration tests
- [ ] Test via WebAPI `/test/bot-command` endpoint
- [ ] Deploy to development and test with real Discord bot

## Future Enhancements

- Add configurable result limit (e.g., `search <query> --limit 10`)
- Support L2 distance metric option (like `repost match all l2`)
- Add query history/analytics
- Cache common queries
- Add relevance feedback ("was this result helpful?")
- Support multi-modal queries (text + image)

---

**Ready for implementation!** This design provides semantic search across all tracked images using natural language queries, with output format matching existing `repost match all` command and automatic integration into test infrastructure.
