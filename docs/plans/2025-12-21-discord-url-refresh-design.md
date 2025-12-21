# Discord URL Refresh for PostReevaluator

**Date**: 2025-12-21  
**Status**: Design Complete

## Problem

Discord CDN URLs expire after a couple of days. We have 14K historical posts with expired URLs that need to be re-evaluated with the new ML model. Before re-evaluation, we need to refresh these URLs by fetching the original Discord messages.

## Solution Overview

Extend the PostReevaluator to add a URL refresh phase before the re-evaluation phase:

1. **Phase 1 - URL Refresh**: Fetch fresh Discord messages and update attachment URLs
2. **Phase 2 - Re-evaluation**: Queue posts with fresh URLs for ML processing

Add an `IsPostAvailable` flag to track posts where the Discord message/attachment is no longer accessible, so future runs can skip them.

## Design Details

### 1. Domain Changes

#### New Property on ImagePost

```csharp
public bool IsPostAvailable { get; private set; } = true;
```

#### New Domain Methods

```csharp
public void MarkPostAsUnavailable()
{
    IsPostAvailable = false;
}

public void RefreshImageUrl(Uri newImageUri)
{
    Image = new Image(Image.ImageId, newImageUri, Image.ImageFeatures);
    IsPostAvailable = true; // Mark as available since we successfully refreshed
}
```

#### Database Migration

- Add column `IsPostAvailable` (bool, NOT NULL, default: true)
- Existing posts default to `true` (available)
- No data migration needed

### 2. Infrastructure Changes

#### New Records for Message Data

Location: `ShitpostBot.Infrastructure/Public/Services/` or `Models/`

```csharp
namespace ShitpostBot.Infrastructure.Services;

public record MessageAttachment(ulong Id, Uri Url);

public record FetchedMessage(ulong MessageId, IReadOnlyList<MessageAttachment> Attachments);
```

#### New IChatClient Method

```csharp
/// <summary>
/// Fetches a Discord message and its attachments by message identification.
/// Returns null if channel or message not found.
/// </summary>
Task<FetchedMessage?> GetMessageWithAttachmentsAsync(MessageIdentification messageIdentification);
```

#### Implementation in DiscordChatClient

```csharp
public async Task<FetchedMessage?> GetMessageWithAttachmentsAsync(MessageIdentification messageIdentification)
{
    var guild = await discordClient.GetGuildAsync(messageIdentification.GuildId);
    var channel = guild?.GetChannel(messageIdentification.ChannelId);
    if (channel == null)
    {
        return null;
    }
    
    var message = await channel.GetMessageAsync(messageIdentification.MessageId);
    if (message == null)
    {
        return null;
    }
    
    var attachments = message.Attachments
        .Select(a => new MessageAttachment(a.Id, new Uri(a.Url)))
        .ToList();
    
    return new FetchedMessage(message.Id, attachments);
}
```

### 3. PostReevaluator Changes

#### URL Refresh Method

```csharp
private static async Task RefreshDiscordUrlsForOutdatedPosts(
    ILogger<Program> logger,
    IChatClient chatClient,
    IEnumerable<ImagePost> imagePostsWithOutdatedModel,
    IUnitOfWork unitOfWork,
    CancellationToken cancellationToken)
{
    int refreshedCount = 0;
    int unchangedCount = 0;
    int unavailableCount = 0;
    
    foreach (var imagePost in imagePostsWithOutdatedModel)
    {
        try
        {
            var messageIdentification = new MessageIdentification(
                imagePost.ChatGuildId,
                imagePost.ChatChannelId,
                imagePost.PosterId,
                imagePost.ChatMessageId);
            
            // Fetch message from Discord
            var fetchedMessage = await chatClient.GetMessageWithAttachmentsAsync(messageIdentification);
            
            if (fetchedMessage == null)
            {
                logger.LogWarning(
                    "Message or channel unavailable for ImagePost {ImagePostId} (Guild: {GuildId}, Channel: {ChannelId}, Message: {MessageId})",
                    imagePost.Id, imagePost.ChatGuildId, imagePost.ChatChannelId, imagePost.ChatMessageId);
                imagePost.MarkPostAsUnavailable();
                unavailableCount++;
                await unitOfWork.SaveChangesAsync(cancellationToken);
                await Task.Delay(100, cancellationToken);
                continue;
            }
            
            // Find matching attachment by ID
            var attachment = fetchedMessage.Attachments
                .FirstOrDefault(a => a.Id == imagePost.Image.ImageId);
            
            if (attachment == null)
            {
                logger.LogWarning(
                    "Attachment unavailable for ImagePost {ImagePostId} (AttachmentId: {AttachmentId})",
                    imagePost.Id, imagePost.Image.ImageId);
                imagePost.MarkPostAsUnavailable();
                unavailableCount++;
                await unitOfWork.SaveChangesAsync(cancellationToken);
                await Task.Delay(100, cancellationToken);
                continue;
            }
            
            // Refresh URL if it changed
            if (attachment.Url.ToString() != imagePost.Image.ImageUri.ToString())
            {
                logger.LogDebug(
                    "Refreshing URL for ImagePost {ImagePostId}: {OldUrl} -> {NewUrl}",
                    imagePost.Id, imagePost.Image.ImageUri, attachment.Url);
                imagePost.RefreshImageUrl(attachment.Url);
                refreshedCount++;
            }
            else
            {
                unchangedCount++;
            }
            
            // Save after each message to avoid re-calling Discord API
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Log unexpected errors but don't mark as unavailable - let it retry next time
            logger.LogError(ex,
                "Unexpected error refreshing URL for ImagePost {ImagePostId}. Skipping this post.",
                imagePost.Id);
        }
        
        // Throttle to avoid Discord rate limits (100ms = ~2.7 hours for 14K posts)
        await Task.Delay(100, cancellationToken);
    }
    
    logger.LogInformation(
        "URL refresh completed: {RefreshedCount} URLs refreshed, {UnchangedCount} unchanged, {UnavailableCount} marked unavailable",
        refreshedCount, unchangedCount, unavailableCount);
}
```

#### Re-evaluation Queueing Method

```csharp
private static async Task QueuePostsForReevaluation(
    ILogger<Program> logger,
    IBus bus,
    IEnumerable<ImagePost> imagePosts,
    int throttleDelayMs,
    CancellationToken cancellationToken)
{
    int queuedCount = 0;
    
    foreach (var imagePost in imagePosts)
    {
        logger.LogDebug(
            "Publishing re-evaluation for ImagePost {ImagePostId} (current model: {CurrentModel})",
            imagePost.Id,
            imagePost.Image.ImageFeatures?.ModelName);

        await bus.Publish(new ImagePostTracked
        {
            ImagePostId = imagePost.Id,
            IsReevaluation = true
        }, cancellationToken);
        
        queuedCount++;

        // Throttle to avoid overwhelming the queue
        await Task.Delay(throttleDelayMs, cancellationToken);
    }
    
    logger.LogInformation(
        "PostReevaluator completed: {ProcessedCount} posts queued for re-evaluation",
        queuedCount);
}
```

#### Updated Program.cs Flow

```csharp
var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((hostContext, services) =>
{
    services.AddShitpostBotInfrastructure(hostContext.Configuration);
    services.AddShitpostBotMassTransit(hostContext.Configuration);
});

var host = builder.Build();

using var scope = host.Services.CreateScope();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
var imagePostsReader = scope.ServiceProvider.GetRequiredService<IImagePostsReader>();
var imageFeatureExtractorApi = scope.ServiceProvider.GetRequiredService<IImageFeatureExtractorApi>();
var bus = scope.ServiceProvider.GetRequiredService<IBus>();
var chatClient = scope.ServiceProvider.GetRequiredService<IChatClient>();
var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

const int throttleDelayMs = 50;

logger.LogInformation("PostReevaluator starting at: {time}", DateTimeOffset.Now);

var modelNameResponse = await imageFeatureExtractorApi.GetModelNameAsync();
if (!modelNameResponse.IsSuccessful)
{
    throw modelNameResponse.Error;
}

var currentModelName = modelNameResponse.Content.ModelName;

logger.LogInformation("Current ML model: {ModelName}", currentModelName);

// Query ImagePosts with embeddings that don't match current model
var imagePosts = await imagePostsReader
    .All()
    .Where(p => p.Image.ImageFeatures != null
                && p.Image.ImageFeatures.ModelName != currentModelName
                && p.IsPostAvailable)
    .OrderBy(p => p.Id)
    .ToArrayAsync(CancellationToken.None);

logger.LogInformation(
    "Found {Count} posts with outdated model embeddings", 
    imagePosts.Length);

// Phase 1: Refresh Discord URLs
logger.LogInformation("Phase 1: Refreshing Discord URLs...");
await RefreshDiscordUrlsForOutdatedPosts(
    logger, 
    chatClient, 
    imagePosts, 
    unitOfWork, 
    CancellationToken.None);

// Re-query to get only available posts after URL refresh
var availablePosts = await imagePostsReader
    .All()
    .Where(p => p.Image.ImageFeatures != null
                && p.Image.ImageFeatures.ModelName != currentModelName
                && p.IsPostAvailable)
    .OrderBy(p => p.Id)
    .ToArrayAsync(CancellationToken.None);

// Phase 2: Queue posts for re-evaluation
logger.LogInformation("Phase 2: Queueing {Count} available posts for re-evaluation...", availablePosts.Length);
await QueuePostsForReevaluation(
    logger,
    bus,
    availablePosts,
    throttleDelayMs,
    CancellationToken.None);
```

### 4. Updated Queries

All PostReevaluator queries now filter out unavailable posts:

```csharp
.Where(p => p.IsPostAvailable)
```

## Edge Cases Handled

1. **Concurrent runs**: Safe due to database transactions and idempotent operations
2. **Partial failures**: Saves after each message, restarts pick up where left off
3. **Discord rate limiting**: Job crashes (no retry logic)
4. **Attachment removed from message**: Marks post as unavailable
5. **URL hasn't expired yet**: Refreshes all URLs for consistency
6. **Existing posts**: Migration defaults `IsPostAvailable = true`
7. **404 after URL refresh**: Existing handler clears features on 404

## Testing Strategy

### Unit Tests
- `ImagePost.RefreshImageUrl()` updates Image with new URI and sets `IsPostAvailable = true`
- `ImagePost.MarkPostAsUnavailable()` sets `IsPostAvailable = false`
- `ImagePost.MarkPostAsUnavailable()` does NOT update `EvaluatedOn`

### Integration Tests
- `IChatClient.GetMessageWithAttachmentsAsync()` returns null when channel doesn't exist
- `IChatClient.GetMessageWithAttachmentsAsync()` returns null when message doesn't exist
- `IChatClient.GetMessageWithAttachmentsAsync()` returns FetchedMessage with attachments when successful

### Manual Testing
- Monitor logs during production run
- Verify database changes: URLs updated, unavailable posts marked correctly

## Performance Characteristics

- **14K posts** with 100ms Discord API throttle = **~2.7 hours** for Phase 1
- **Phase 2** queue publishing with 50ms throttle = **~12 minutes**
- **Total runtime**: ~3 hours for full 14K post refresh + re-evaluation queueing

## Files to Modify

1. `ShitpostBot.Domain/Posts/ImagePost.cs` - Add flag and methods
2. `ShitpostBot.Infrastructure/Internal/Configurations/ImagePostConfiguration.cs` - EF Core config (may auto-detect)
3. `ShitpostBot.Infrastructure/Public/Services/IChatClient.cs` - Add new method
4. `ShitpostBot.Infrastructure/Internal/Services/DiscordChatClient.cs` - Implement method
5. `ShitpostBot.Infrastructure/Public/Models/` or `Services/` - Add new records
6. `ShitpostBot.PostReevaluator/Program.cs` - Add two new methods and refactor main flow
7. New migration file - `AddIsPostAvailable` migration

## Future Considerations

- Could add a scheduled job to periodically refresh URLs before they expire
- Could add metrics/monitoring for unavailable post counts
- Could add admin command to manually trigger URL refresh for specific posts
