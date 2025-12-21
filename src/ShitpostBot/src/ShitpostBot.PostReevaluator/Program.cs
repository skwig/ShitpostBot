using MassTransit;
using Microsoft.EntityFrameworkCore;
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

const int pageSize = 100;
const int throttleDelayMs = 50;

logger.LogInformation("PostReevaluator starting at: {time}", DateTimeOffset.Now);

var modelNameResponse = await imageFeatureExtractorApi.GetModelNameAsync();
if (!modelNameResponse.IsSuccessful)
{
    throw modelNameResponse.Error;
}

var currentModelName = modelNameResponse.Content.ModelName;

logger.LogInformation("Current ML model: {ModelName}", currentModelName);

var totalProcessedCount = 0;
var pageNumber = 0;

while (true)
{
    // Query ImagePosts with embeddings that don't match current model
    var imagePosts = await imagePostsReader
        .All()
        .Where(p => p.Image.ImageFeatures != null
                    && p.Image.ImageFeatures.ModelName != currentModelName)
        .OrderBy(p => p.Id)
        .Skip(pageNumber * pageSize)
        .Take(pageSize)
        .ToListAsync(CancellationToken.None);

    if (imagePosts.Count == 0)
    {
        break;
    }

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
        }, CancellationToken.None);

        totalProcessedCount++;

        // Throttle to avoid overwhelming the queue
        await Task.Delay(throttleDelayMs, CancellationToken.None);
    }

    pageNumber++;
    logger.LogInformation(
        "Processed page {PageNumber}: {BatchCount} posts queued, {TotalCount} total",
        pageNumber,
        imagePosts.Count,
        totalProcessedCount);
}

logger.LogInformation(
    "PostReevaluator completed: {ProcessedCount} posts queued for re-evaluation",
    totalProcessedCount);