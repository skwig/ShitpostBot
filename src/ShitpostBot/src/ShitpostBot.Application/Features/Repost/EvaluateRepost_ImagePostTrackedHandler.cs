using System.Net;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pgvector;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Extensions;
using ShitpostBot.Infrastructure.Messages;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.Repost;

public class EvaluateRepost_ImagePostTrackedHandler(
    ILogger<EvaluateRepost_ImagePostTrackedHandler> logger,
    IImageFeatureExtractorApi imageFeatureExtractorApi,
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IOptions<RepostServiceOptions> options,
    IChatClient chatClient,
    IDateTimeProvider dateTimeProvider)
    : IConsumer<ImagePostTracked>
{
    private static readonly string[] RepostReactions =
    [
        ":police_car:",
        ":rotating_light:"
    ];

    public async Task Consume(ConsumeContext<ImagePostTracked> context)
    {
        var postToBeEvaluated = await dbContext.ImagePost.GetById(context.Message.ImagePostId, context.CancellationToken);
        if (postToBeEvaluated == null)
        {
            throw new InvalidOperationException($"ImagePost {context.Message.ImagePostId} not found");
        }

        var response = await imageFeatureExtractorApi.ProcessImageAsync(new ProcessImageRequest
        {
            ImageUrl = postToBeEvaluated.Image.ImageUri.ToString(),
            Embedding = true,
            Caption = false,
            Ocr = false
        });

        if (!response.IsSuccessful)
        {
            // Special case: 404 means image is gone from Discord CDN
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogError(
                    "Image not found (404) for ImagePost {ImagePostId}, URL: {ImageUrl}. Clearing ImageFeatures.",
                    context.Message.ImagePostId, postToBeEvaluated.Image.ImageUri);

                postToBeEvaluated.ClearImageFeatures(dateTimeProvider.UtcNow);
                await unitOfWork.SaveChangesAsync(context.CancellationToken);
                return;
            }

            // Client errors (4xx except 404): Don't retry, likely invalid image format
            if (response.StatusCode >= HttpStatusCode.BadRequest && 
                response.StatusCode < HttpStatusCode.InternalServerError)
            {
                logger.LogError(
                    "ML service rejected image (client error {StatusCode}) for ImagePost {ImagePostId}, URL: {ImageUrl}. " +
                    "This is likely an invalid image format or ML service bug. Not retrying.",
                    response.StatusCode, context.Message.ImagePostId, postToBeEvaluated.Image.ImageUri);
                
                // Throw non-retryable exception
                throw new InvalidOperationException(
                    $"ML service client error: {response.StatusCode} for ImagePost {context.Message.ImagePostId}");
            }

            // Server errors (5xx), network issues, timeouts: Retry via MassTransit middleware
            logger.LogWarning(
                "ML service unavailable (transient failure, status: {StatusCode}) for ImagePost {ImagePostId}, URL: {ImageUrl}. " +
                "Will retry with exponential backoff.",
                response.StatusCode, context.Message.ImagePostId, postToBeEvaluated.Image.ImageUri);
            
            // Throw retryable exception - MassTransit middleware will handle retry
            if (response.Error != null)
            {
                throw response.Error;
            }
            throw new HttpRequestException(
                $"ML service returned {response.StatusCode} for ImagePost {context.Message.ImagePostId}");
        }

        var extractImageFeaturesResponse = response.Content;
        var embedding = extractImageFeaturesResponse.Embedding
                        ?? throw new InvalidOperationException("ML service did not return embedding");

        postToBeEvaluated.SetImageFeatures(
            new ImageFeatures(extractImageFeaturesResponse.ModelName, new Vector(embedding)),
            dateTimeProvider.UtcNow);

        await unitOfWork.SaveChangesAsync(context.CancellationToken);

        if (context.Message.IsReevaluation)
        {
            logger.LogDebug(
                "Skipping repost detection for ImagePost {ImagePostId} (re-evaluation mode)",
                context.Message.ImagePostId);
            return;
        }

        var mostSimilarWhitelisted = await dbContext.WhitelistedPost
            .AsNoTracking()
            .ClosestWhitelistedToImagePostWithFeatureVector(
                postToBeEvaluated.PostedOn,
                postToBeEvaluated.Image.ImageFeatures!.FeatureVector)
            .FirstOrDefaultAsync(context.CancellationToken);

        if (mostSimilarWhitelisted?.CosineSimilarity >= (double)options.Value.RepostSimilarityThreshold)
        {
            logger.LogDebug(
                "Similarity of {Similarity:0.00000000} with {ImagePostId}, which is whitelisted",
                mostSimilarWhitelisted?.CosineSimilarity,
                mostSimilarWhitelisted?.ImagePostId);
            return;
        }

        var mostSimilar = await dbContext.ImagePost
            .AsNoTracking()
            .ImagePostsWithClosestFeatureVector(
                postToBeEvaluated.PostedOn,
                postToBeEvaluated.Image.ImageFeatures!.FeatureVector)
            .FirstOrDefaultAsync(context.CancellationToken);

        if (mostSimilar?.CosineSimilarity >= (double)options.Value.RepostSimilarityThreshold)
        {
            var identification = new MessageIdentification(
                postToBeEvaluated.ChatGuildId,
                postToBeEvaluated.ChatChannelId,
                postToBeEvaluated.PosterId,
                postToBeEvaluated.ChatMessageId
            );

            foreach (var repostReaction in RepostReactions)
            {
                await chatClient.React(identification, repostReaction);
                await Task.Delay(TimeSpan.FromMilliseconds(500), context.CancellationToken);
            }
        }
    }
}