# Rich Search Results with Discord Embeds

**Date**: 2025-12-24  
**Status**: Design Complete

## Problem

The semantic search command (`search <query>`) currently returns plain text results:

```
Higher is a closer match:
1. Match of `0.87654321` with https://discord.com/... posted 2 hours ago
2. Match of `0.82345678` with https://discord.com/... posted 1 day ago
...
```

This format is functional but lacks visual appeal. Users cannot preview the actual images in search results, making it harder to quickly identify relevant matches. Other Discord bots (like NotSoBot) use rich embeds with image thumbnails for a better user experience.

## Solution Overview

Transform search results from plain text to rich Discord embeds with visual previews:

**Current**: Single plain text message listing 5 results  
**New**: Single Discord message containing up to 5 embeds, each showing one result with thumbnail

### Key Design Decisions

1. **Multiple embeds in one message**: Send 5 separate embeds in a single Discord message (one per result)
2. **Consistent formatting**: All result embeds use identical structure (no special treatment for best match)
3. **Scores speak for themselves**: Remove low confidence warnings - users can interpret similarity scores
4. **Minimal design**: No footer, no header embed - just clean result embeds
5. **Preserve current info**: Each embed shows same data as current format (score, link, timestamp)

### Visual Structure

Each result embed contains:
- **Title**: `Result #N - Match: 0.87654321`
- **Description**: Discord message link + relative timestamp
- **Thumbnail**: The actual image from `ImagePost.Image.ImageUri`

Example message with 3 results:

```
┌─────────────────────────────────────────────────┐
│ Result #1 - Match: 0.87654321      [thumbnail]  │
│ https://discord.com/channels/.../...            │
│ Posted 2 hours ago                              │
└─────────────────────────────────────────────────┘
┌─────────────────────────────────────────────────┐
│ Result #2 - Match: 0.82345678      [thumbnail]  │
│ https://discord.com/channels/.../...            │
│ Posted 5 hours ago                              │
└─────────────────────────────────────────────────┘
┌─────────────────────────────────────────────────┐
│ Result #3 - Match: 0.75123456      [thumbnail]  │
│ https://discord.com/channels/.../...            │
│ Posted 1 day ago                                │
└─────────────────────────────────────────────────┘
```

## Design Details

### 1. Discord Embed Constraints

Discord message limitations:
- Maximum 10 embeds per message
- Each embed can have one thumbnail
- Thumbnail renders on the right side of embed
- Title max: 256 characters
- Description max: 4096 characters

Our usage:
- Up to 5 embeds per message (one per search result)
- Well within Discord's 10 embed limit
- Simple content fits easily within character limits

### 2. Implementation Changes

#### Modify SearchBotCommandHandler.cs

Location: `ShitpostBot.Application/Features/BotCommands/Search/SearchBotCommandHandler.cs`

**Current code (lines 76-93):**
```csharp
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
```

**New code:**
```csharp
// Build message with multiple embeds (one per result)
var messageBuilder = new DiscordMessageBuilder();

for (int i = 0; i < similarPosts.Count; i++)
{
    var post = similarPosts[i];
    
    var embed = new DiscordEmbedBuilder()
        .WithTitle($"Result #{i + 1} - Match: {post.CosineSimilarity:0.00000000}")
        .WithDescription($"{post.ChatMessageIdentifier.GetUri()}\nPosted {chatClient.Utils.RelativeTimestamp(post.PostedOn)}")
        .WithThumbnail(post.Image.ImageUri.ToString());
    
    messageBuilder.AddEmbed(embed);
}

await chatClient.SendMessage(
    messageDestination,
    messageBuilder
);
```

**Required imports:**
```csharp
using DSharpPlus.Entities;  // Add to existing imports
```

**Code to remove:**
- Lines 15-16: `LowConfidenceThreshold` constant (no longer needed)
- Lines 76-82: Low confidence detection and header message logic

### 3. Data Requirements

Each `ImagePost` result needs:
- `CosineSimilarity` (double) - Already available from query
- `ChatMessageIdentifier` - Already available, has `GetUri()` method
- `PostedOn` (DateTimeOffset) - Already available
- `Image.ImageUri` (Uri) - **Need to include in query**

#### Update Database Query

The current query in `SearchBotCommandHandler.cs` (lines 61-65) uses:
```csharp
var similarPosts = await dbContext.ImagePost
    .AsNoTracking()
    .ImagePostsWithClosestFeatureVector(textEmbedding)
    .Take(ResultLimit)
    .ToListAsync();
```

The `ImagePostsWithClosestFeatureVector` extension returns `ClosestToImagePost` which includes image data. **Verify that `Image.ImageUri` is included in the query projection.**

If not included, modify the query extension or add `.Include(x => x.Image)` to eagerly load image data.

### 4. Error Handling & Edge Cases

**No changes needed for:**
- Empty query validation (line 41-48)
- No results case (line 67-74)
- ML service errors (line 54-56)

All error cases continue to use plain text messages (not embeds), which is appropriate.

**Edge case: Fewer than 5 results**
- Only create embeds for results that exist
- No padding or dummy embeds needed
- Message contains 1-5 embeds naturally

**Edge case: Image URL unavailable**
- Currently ignoring `IsPostAvailable` flag (per design decision)
- Discord will handle broken image URLs gracefully (shows broken image icon)
- Future work: Filter unavailable images or show placeholder

### 5. Comparison to NotSoBot Example

**Similarities adopted:**
- Rich embed format with thumbnails
- Multiple embeds in single message

**Differences (intentional):**
- No pagination buttons (simpler, always shows all 5 results)
- No author info in embed (not relevant for image search)
- No footer (cleaner, scores are self-explanatory)
- Simpler embed structure (title + description + thumbnail only)

**Why simpler is better:**
- Pagination requires component interaction handling (significant complexity)
- 5 results fit comfortably on screen without scrolling
- Search is typically one-shot (not exploratory browsing)
- Matches bot's existing simple command style

## Testing Strategy

### Integration Tests

Location: `ShitpostBot.Tests.Integration/WebApiIntegrationTests.cs`

**Update existing test** `SemanticSearch_ReturnsRelevantImages`:

Current assertion:
```csharp
resultMessage.Should().Contain("Higher is a closer match");
resultMessage.Should().Contain("Match of");
```

New assertion approach:
```csharp
// Verify embed structure instead of plain text
// May need to inspect response differently if embeds aren't captured in actions
// Consider testing via Discord client directly or extending test infrastructure
```

**Note**: Integration tests currently capture message content as strings. Rich embeds may require extending test infrastructure to verify embed structure. Consider this a follow-up task if needed.

### E2E Tests

Location: `test/e2e/scenarios/scenario-5-semantic-search.http`

**Update verification step:**

Current:
```http
### Verify results contain expected format
GET {{baseUrl}}/test/actions/{{messageId}}?expectedCount=2
```

Expected response structure should now contain embeds instead of plain text. Update assertions accordingly.

### Manual Testing

1. Run local docker compose environment
2. Post sample images (frenchcat.jpg, obsidianslop.webp, etc.)
3. Execute search commands:
   - `search cat` - Should show embeds with cat image thumbnails
   - `search landscape` - Should show landscape images
   - `search xyz random` - Should show embeds even with low similarity scores
4. Verify:
   - Each embed has correct title format: "Result #N - Match: X.XXXXXXXX"
   - Each embed shows thumbnail image
   - Description contains Discord link and timestamp
   - All 5 results render correctly in Discord client
   - Thumbnails are clickable and lead to original messages

## Files to Modify

1. **ShitpostBot.Application/Features/BotCommands/Search/SearchBotCommandHandler.cs**
   - Add `using DSharpPlus.Entities;` import
   - Remove `LowConfidenceThreshold` constant
   - Replace lines 76-93 with multi-embed builder code
   - Verify `Image.ImageUri` is available in query results

2. **Test files (optional, follow-up):**
   - `ShitpostBot.Tests.Integration/WebApiIntegrationTests.cs` - Update assertions
   - `test/e2e/scenarios/scenario-5-semantic-search.http` - Update expected response format

## Implementation Checklist

- [ ] Add `using DSharpPlus.Entities;` to SearchBotCommandHandler.cs
- [ ] Remove `LowConfidenceThreshold` constant (line 15)
- [ ] Replace plain text message building with embed builder loop (lines 76-93)
- [ ] Verify query includes `Image.ImageUri` (check query projection)
- [ ] Build solution and fix any compilation errors
- [ ] Run integration tests (update assertions if needed)
- [ ] Test locally with docker compose
- [ ] Verify embeds render correctly in Discord client
- [ ] Check thumbnails display for all 5 results
- [ ] Test edge cases: 1 result, 0 results, empty query

## Future Enhancements

- Add pagination with Previous/Next buttons for exploring more than 5 results
- Show original poster in embed (requires additional query data)
- Add embed color coding based on similarity score (green=high, yellow=medium, red=low)
- Filter out `IsPostAvailable = false` images before displaying
- Add "View Original" button to each embed
- Support custom result limits (e.g., `search cat --limit 10`)

---

**Ready for implementation!** This design transforms search results into rich visual embeds while maintaining simplicity and reusing existing infrastructure.
