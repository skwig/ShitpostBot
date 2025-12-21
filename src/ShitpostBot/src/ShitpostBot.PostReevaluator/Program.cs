using MassTransit;
using Microsoft.EntityFrameworkCore;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Messages;
using ShitpostBot.Infrastructure.Services;

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

static async Task RefreshDiscordUrlsForOutdatedPosts(
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

static async Task QueuePostsForReevaluation(
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