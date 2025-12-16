using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ShitpostBot.Application.Services;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Messages;

namespace ShitpostBot.PostReevaluator;

public class PostReevaluatorWorker(
    ILogger<PostReevaluatorWorker> logger,
    IServiceScopeFactory factory) : IHostedService
{
    private const int PageSize = 100;
    private const int ThrottleDelayMs = 75; // 50-100ms throttle

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("PostReevaluatorWorker starting at: {time}", DateTimeOffset.Now);

        using var serviceScope = factory.CreateScope();
        var applicationLifetime = serviceScope.ServiceProvider.GetRequiredService<IHostApplicationLifetime>();
        var imagePostsReader = serviceScope.ServiceProvider.GetRequiredService<IImagePostsReader>();
        var imageFeatureExtractorApi = serviceScope.ServiceProvider.GetRequiredService<IImageFeatureExtractorApi>();
        var bus = serviceScope.ServiceProvider.GetRequiredService<IBus>();

        try
        {
            // Fetch current model name from ML service
            var modelNameResponse = await imageFeatureExtractorApi.GetModelNameAsync();
            var currentModelName = modelNameResponse.ModelName;
            
            logger.LogInformation("Current ML model: {ModelName}", currentModelName);

            // Query all ImagePosts with embeddings that don't match current model
            var totalProcessedCount = 0;
            var pageNumber = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                var imagePosts = await imagePostsReader
                    .All()
                    .Where(p => p.Image.ImageFeatures != null 
                             && p.Image.ImageFeatures.ModelName != currentModelName)
                    .OrderBy(p => p.Id)
                    .Skip(pageNumber * PageSize)
                    .Take(PageSize)
                    .ToListAsync(cancellationToken);

                if (!imagePosts.Any())
                {
                    logger.LogInformation("No more outdated embeddings found. Migration complete.");
                    break;
                }

                foreach (var imagePost in imagePosts)
                {
                    logger.LogDebug("Publishing re-evaluation for ImagePost {ImagePostId} (current model: {CurrentModel})",
                        imagePost.Id, imagePost.Image.ImageFeatures?.ModelName ?? "null");

                    await bus.Publish(new ImagePostTracked
                    {
                        ImagePostId = imagePost.Id,
                        IsReEvaluation = true
                    }, cancellationToken);

                    totalProcessedCount++;

                    // Throttle to avoid overwhelming the queue (50-100ms)
                    await Task.Delay(ThrottleDelayMs, cancellationToken);
                }

                pageNumber++;
                logger.LogInformation("Processed page {PageNumber}: {BatchCount} posts queued, {TotalCount} total",
                    pageNumber, imagePosts.Count, totalProcessedCount);
            }

            logger.LogInformation("PostReevaluatorWorker completed: {ProcessedCount} posts queued for re-evaluation", totalProcessedCount);
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

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
