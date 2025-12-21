using MassTransit;
using Microsoft.EntityFrameworkCore;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Messages;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.PostReevaluator;

public class PostReevaluatorWorker(
    ILogger<PostReevaluatorWorker> logger,
    IServiceScopeFactory factory) : BackgroundService
{
    private const int PageSize = 100;
    private const int ThrottleDelayMs = 50;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("PostReevaluatorWorker starting at: {time}", DateTimeOffset.Now);

        using var serviceScope = factory.CreateScope();
        var applicationLifetime = serviceScope.ServiceProvider.GetRequiredService<IHostApplicationLifetime>();
        var imagePostsReader = serviceScope.ServiceProvider.GetRequiredService<IImagePostsReader>();
        var imageFeatureExtractorApi = serviceScope.ServiceProvider.GetRequiredService<IImageFeatureExtractorApi>();
        var bus = serviceScope.ServiceProvider.GetRequiredService<IBus>();

        try
        {
            var modelNameResponse = await imageFeatureExtractorApi.GetModelNameAsync();
            if (!modelNameResponse.IsSuccessful)
            {
                throw modelNameResponse.Error;
            }

            var currentModelName = modelNameResponse.Content.ModelName;

            logger.LogInformation("Current ML model: {ModelName}", currentModelName);

            var totalProcessedCount = 0;
            var pageNumber = 0;

            while (!stoppingToken.IsCancellationRequested)
            {
                // Query ImagePosts with embeddings that don't match current model
                var imagePosts = await imagePostsReader
                    .All()
                    .Where(p => p.Image.ImageFeatures != null
                                && p.Image.ImageFeatures.ModelName != currentModelName)
                    .OrderBy(p => p.Id)
                    .Skip(pageNumber * PageSize)
                    .Take(PageSize)
                    .ToListAsync(stoppingToken);

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
                    }, stoppingToken);

                    totalProcessedCount++;

                    // Throttle to avoid overwhelming the queue
                    await Task.Delay(ThrottleDelayMs, stoppingToken);
                }

                pageNumber++;
                logger.LogInformation(
                    "Processed page {PageNumber}: {BatchCount} posts queued, {TotalCount} total",
                    pageNumber,
                    imagePosts.Count,
                    totalProcessedCount);
            }

            logger.LogInformation(
                "PostReevaluatorWorker completed: {ProcessedCount} posts queued for re-evaluation",
                totalProcessedCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PostReevaluatorWorker failed");
            throw;
        }
        finally
        {
            applicationLifetime.StopApplication();
        }
    }
}