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
    services.AddDiscordClient(hostContext.Configuration);
    services.AddShitpostBotMassTransit(hostContext.Configuration);
});

var host = builder.Build();

using var scope = host.Services.CreateScope();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
var dbContext = scope.ServiceProvider.GetRequiredService<IDbContext>();
var imageFeatureExtractorApi = scope.ServiceProvider.GetRequiredService<IImageFeatureExtractorApi>();
var bus = scope.ServiceProvider.GetRequiredService<IBus>();
var chatClient = scope.ServiceProvider.GetRequiredService<IChatClient>();
var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

const int chatThrottleDelayMs = 500;

logger.LogInformation("PostReevaluator starting at: {time}", DateTimeOffset.Now);

var modelNameResponse = await imageFeatureExtractorApi.GetModelNameAsync();
if (!modelNameResponse.IsSuccessful)
{
    throw modelNameResponse.Error;
}

var currentModelName = modelNameResponse.Content.ModelName;

logger.LogInformation("Current ML model: {ModelName}", currentModelName);

// Query ImagePosts with embeddings that don't match current model
var imagePosts = await dbContext.ImagePost
    .AsNoTracking()
    .Where(p => p.Image.ImageFeatures != null
                && p.Image.ImageFeatures.ModelName != currentModelName
                && p.IsPostAvailable)
    .OrderBy(p => p.Id)
    .ToArrayAsync(CancellationToken.None);

logger.LogInformation(
    "Found {Count} posts with outdated model embeddings. Starting interleaved URL refresh and re-evaluation queueing...",
    imagePosts.Length);

await RefreshUrlAndQueueForReevaluation(
    logger,
    chatClient,
    bus,
    imagePosts,
    unitOfWork,
    chatThrottleDelayMs,
    CancellationToken.None);

logger.LogInformation("PostReevaluator completed at: {time}", DateTimeOffset.Now);
return;

static async Task RefreshUrlAndQueueForReevaluation(
    ILogger<Program> logger,
    IChatClient chatClient,
    IBus bus,
    IReadOnlyCollection<ImagePost> imagePosts,
    IUnitOfWork unitOfWork,
    int chatThrottleDelayMs,
    CancellationToken cancellationToken)
{
    var refreshedCount = 0;
    var unchangedCount = 0;
    var unavailableCount = 0;
    var queuedCount = 0;
    var processedCount = 0;
    var totalCount = imagePosts.Count();

    foreach (var imagePost in imagePosts)
    {
        processedCount++;

        var urlBefore = imagePost.Image.ImageUri.ToString();

        // Step 1: Refresh Discord URL
        var isAvailable = await RefreshDiscordUrl(
            logger,
            chatClient,
            imagePost,
            unitOfWork,
            cancellationToken);

        if (!isAvailable)
        {
            unavailableCount++;
            await Task.Delay(chatThrottleDelayMs, cancellationToken);
            continue;
        }

        var urlAfter = imagePost.Image.ImageUri.ToString();
        if (urlBefore != urlAfter)
        {
            refreshedCount++;
        }
        else
        {
            unchangedCount++;
        }

        // Step 2: Immediately queue for re-evaluation
        await QueueReevaluation(logger, imagePost, bus, cancellationToken);
        queuedCount++;

        if (processedCount % 100 == 0)
        {
            logger.LogInformation(
                "Progress: {ProcessedCount}/{TotalCount} posts processed ({Percentage:F1}%), " +
                "{QueuedCount} queued, {UnavailableCount} unavailable",
                processedCount, totalCount, (processedCount * 100.0 / totalCount),
                queuedCount, unavailableCount);
        }

        await Task.Delay(chatThrottleDelayMs, cancellationToken);
    }

    logger.LogInformation(
        "Processing completed: {QueuedCount} posts queued for re-evaluation, " +
        "{RefreshedCount} URLs refreshed, {UnchangedCount} URLs unchanged, " +
        "{UnavailableCount} posts marked unavailable",
        queuedCount, refreshedCount, unchangedCount, unavailableCount);
}

static async Task<bool> RefreshDiscordUrl(
    ILogger<Program> logger,
    IChatClient chatClient,
    ImagePost imagePost,
    IUnitOfWork unitOfWork,
    CancellationToken cancellationToken)
{
    try
    {
        var messageIdentification = new MessageIdentification(
            imagePost.ChatGuildId,
            imagePost.ChatChannelId,
            imagePost.PosterId,
            imagePost.ChatMessageId);

        var fetchedMessage = await chatClient.GetMessageWithAttachmentsAsync(messageIdentification);
        if (fetchedMessage == null)
        {
            logger.LogWarning(
                "Message or channel unavailable for ImagePost {ImagePostId} (Guild: {GuildId}, Channel: {ChannelId}, Message: {MessageId})",
                imagePost.Id, imagePost.ChatGuildId, imagePost.ChatChannelId, imagePost.ChatMessageId);
            imagePost.MarkPostAsUnavailable();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return false;
        }

        var matchingAttachment = fetchedMessage.Attachments
            .FirstOrDefault(a => a.Id == imagePost.Image.ImageId);

        if (matchingAttachment == null)
        {
            logger.LogWarning(
                "Attachment unavailable for ImagePost {ImagePostId} (AttachmentId: {AttachmentId})",
                imagePost.Id, imagePost.Image.ImageId);
            imagePost.MarkPostAsUnavailable();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return false;
        }

        if (matchingAttachment.Url.ToString() != imagePost.Image.ImageUri.ToString())
        {
            logger.LogDebug(
                "Refreshing URL for ImagePost {ImagePostId}: {OldUrl} -> {NewUrl}",
                imagePost.Id, imagePost.Image.ImageUri, matchingAttachment.Url);
            imagePost.RefreshImageUrl(matchingAttachment.Url);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
    catch (Exception ex)
    {
        // Log unexpected errors but don't mark as unavailable - let it retry next time
        logger.LogError(ex,
            "Unexpected error refreshing URL for ImagePost {ImagePostId}. Skipping this post.",
            imagePost.Id);
        return false;
    }
}

static async Task QueueReevaluation(
    ILogger<Program> logger, 
    ImagePost imagePost, 
    IBus bus,
    CancellationToken cancellationToken)
{
    logger.LogDebug(
        "Queueing ImagePost {ImagePostId} for re-evaluation (current model: {CurrentModel})",
        imagePost.Id,
        imagePost.Image.ImageFeatures?.ModelName);

    await bus.Publish(new ImagePostTracked
    {
        ImagePostId = imagePost.Id,
        IsReevaluation = true
    }, cancellationToken);
}