using Microsoft.EntityFrameworkCore;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

const int RunIntervalHours = 6;
const int FullRefreshCycleDays = 7;
const int ThrottleDelayMs = 500;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((hostContext, services) =>
{
    services.AddShitpostBotInfrastructure(hostContext.Configuration);
    services.AddDiscordClient(hostContext.Configuration);
});

var host = builder.Build();

using var scope = host.Services.CreateScope();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
var dbContext = scope.ServiceProvider.GetRequiredService<IDbContext>();
var chatClient = scope.ServiceProvider.GetRequiredService<IChatClient>();
var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

logger.LogInformation("ImageUrlRefresher starting at: {Time}", DateTimeOffset.UtcNow);
logger.LogInformation(
    "Configuration: RunInterval={RunIntervalHours}h, FullRefreshCycle={FullRefreshCycleDays}d, Throttle={ThrottleDelayMs}ms",
    RunIntervalHours, FullRefreshCycleDays, ThrottleDelayMs);

await RefreshImageUrls(logger, dbContext, chatClient, unitOfWork, RunIntervalHours, ThrottleDelayMs);

logger.LogInformation("ImageUrlRefresher completed at: {Time}", DateTimeOffset.UtcNow);
return;

static async Task RefreshImageUrls(
    ILogger<Program> logger,
    IDbContext dbContext,
    IChatClient chatClient,
    IUnitOfWork unitOfWork,
    int runIntervalHours,
    int throttleDelayMs)
{
    var cutoffTime = DateTimeOffset.UtcNow.AddHours(-runIntervalHours);
    
    var postsToRefresh = await dbContext.ImagePost
        .Where(p => p.IsPostAvailable 
                    && p.Image.ImageFeatures != null
                    && (p.Image.ImageUriFetchedAt == null || p.Image.ImageUriFetchedAt < cutoffTime))
        .OrderBy(p => p.Image.ImageUriFetchedAt ?? DateTimeOffset.MinValue)
        .ToArrayAsync();
    
    logger.LogInformation(
        "Found {Count} posts to refresh (older than {CutoffTime})", 
        postsToRefresh.Length, 
        cutoffTime);
    
    foreach (var imagePost in postsToRefresh)
    {
        var utcNow = DateTimeOffset.UtcNow;
        await RefreshSinglePost(logger, chatClient, imagePost, unitOfWork, utcNow);
        await Task.Delay(throttleDelayMs);
    }
    
    logger.LogInformation("Refresh completed");
}

static async Task RefreshSinglePost(
    ILogger<Program> logger,
    IChatClient chatClient,
    ImagePost imagePost,
    IUnitOfWork unitOfWork,
    DateTimeOffset utcNow)
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
                "Message or channel unavailable for ImagePost {ImagePostId}",
                imagePost.Id);
            imagePost.MarkPostAsUnavailable();
            await unitOfWork.SaveChangesAsync();
            return;
        }
        
        var matchingAttachment = fetchedMessage.Attachments
            .FirstOrDefault(a => a.Id == imagePost.Image.ImageId);
        
        if (matchingAttachment == null)
        {
            logger.LogWarning(
                "Attachment unavailable for ImagePost {ImagePostId}",
                imagePost.Id);
            imagePost.MarkPostAsUnavailable();
            await unitOfWork.SaveChangesAsync();
            return;
        }
        
        // Always refresh - updates URL and timestamp
        imagePost.RefreshImageUrl(matchingAttachment.Url, matchingAttachment.MediaType, utcNow);
        await unitOfWork.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        // Log error but don't update timestamp - will retry next run
        logger.LogError(ex,
            "Error refreshing URL for ImagePost {ImagePostId}. Will retry next run.",
            imagePost.Id);
    }
}
