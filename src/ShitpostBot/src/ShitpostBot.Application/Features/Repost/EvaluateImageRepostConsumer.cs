using System.Diagnostics;
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

public class EvaluateImageRepostConsumer(
    ILogger<EvaluateImageRepostConsumer> logger,
    IImageFeatureExtractorApi imageFeatureExtractorApi,
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IOptions<RepostServiceOptions> options,
    IChatClient chatClient,
    IDateTimeProvider dateTimeProvider,
    IMetrics metrics
) : IConsumer<ImagePostTracked>
{
    private static readonly string[] RepostReactions = [":police_car:", ":rotating_light:"];

    public async Task Consume(ConsumeContext<ImagePostTracked> context)
    {
        using var activity = ShitpostBotActivitySource.Instance.StartActivity(
            nameof(EvaluateImageRepostConsumer),
            ActivityKind.Consumer
        );
        Activity.Current?.SetTag(Tags.Messaging.System, "masstransit");
        Activity.Current?.SetTag(Tags.ShitpostBot.ImagePost.Id, context.Message.ImagePostId);
        Activity.Current?.SetTag(Tags.ShitpostBot.Reevaluation, context.Message.IsReevaluation);

        var postToBeEvaluated = await dbContext.ImagePost.GetById(
            context.Message.ImagePostId,
            context.CancellationToken
        );
        if (postToBeEvaluated == null)
        {
            Activity.Current?.SetTag(Tags.ShitpostBot.Repost.Outcome, "missing_post");
            throw new InvalidOperationException(
                $"ImagePost {context.Message.ImagePostId} not found"
            );
        }

        Activity.Current?.SetTag(Tags.Discord.Guild.Id, postToBeEvaluated.ChatGuildId);
        Activity.Current?.SetTag(Tags.Discord.Channel.Id, postToBeEvaluated.ChatChannelId);
        Activity.Current?.SetTag(Tags.Discord.Message.Id, postToBeEvaluated.ChatMessageId);
        Activity.Current?.SetTag(Tags.Discord.User.Id, postToBeEvaluated.PosterId);

        var response = await imageFeatureExtractorApi.ProcessImageAsync(
            new ProcessImageRequest
            {
                ImageUrl = postToBeEvaluated.Image.ImageUri.ToString(),
                Embedding = true,
                Caption = false,
                Ocr = false,
            }
        );

        if (!response.IsSuccessfulWithContent)
        {
            // Special case: 404 means image is gone from Discord CDN
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                Activity.Current?.SetTag(Tags.ShitpostBot.Repost.Outcome, "image_unavailable");
                logger.LogError(
                    "Image not found (404) for ImagePost {ImagePostId}, URL: {ImageUrl}. Clearing ImageFeatures.",
                    context.Message.ImagePostId,
                    postToBeEvaluated.Image.ImageUri
                );

                postToBeEvaluated.ClearImageFeatures(dateTimeProvider.UtcNow);
                await unitOfWork.SaveChangesAsync(context.CancellationToken);
                metrics.LastImageEvaluationTimestamp = dateTimeProvider.UtcNow;
                return;
            }

            logger.LogWarning(
                "ML service unavailable (transient failure, status: {StatusCode}) for ImagePost {ImagePostId}, URL: {ImageUrl}. Will retry with exponential backoff.",
                response.StatusCode,
                context.Message.ImagePostId,
                postToBeEvaluated.Image.ImageUri
            );

            if (response.Error != null)
            {
                throw response.Error;
            }

            throw new HttpRequestException(
                $"ML service returned {response.StatusCode} for ImagePost {context.Message.ImagePostId}"
            );
        }

        var extractImageFeaturesResponse = response.Content;
        var embedding =
            extractImageFeaturesResponse.Embedding
            ?? throw new InvalidOperationException("ML service did not return embedding");

        postToBeEvaluated.SetImageFeatures(
            new ImageFeatures(extractImageFeaturesResponse.ModelName, new Vector(embedding)),
            dateTimeProvider.UtcNow
        );

        await unitOfWork.SaveChangesAsync(context.CancellationToken);

        metrics.LastImageEvaluationTimestamp = dateTimeProvider.UtcNow;

        if (context.Message.IsReevaluation)
        {
            Activity.Current?.SetTag(Tags.ShitpostBot.Repost.Outcome, "skipped_reevaluation");
            logger.LogDebug(
                "Skipping repost detection for ImagePost {ImagePostId} (re-evaluation mode)",
                context.Message.ImagePostId
            );
            return;
        }

        var mostSimilarWhitelisted = await dbContext
            .WhitelistedPost.AsNoTracking()
            .ClosestWhitelistedToImagePostWithFeatureVector(
                postToBeEvaluated.PostedOn,
                postToBeEvaluated.Image.ImageFeatures!.FeatureVector
            )
            .FirstOrDefaultAsync(context.CancellationToken);

        if (
            mostSimilarWhitelisted?.CosineSimilarity
            >= (double)options.Value.RepostSimilarityThreshold
        )
        {
            Activity.Current?.SetTag(Tags.ShitpostBot.Repost.Outcome, "whitelisted_match");
            Activity.Current?.SetTag(
                Tags.ShitpostBot.Repost.Match.ImagePost.Id,
                mostSimilarWhitelisted.ImagePostId
            );
            Activity.Current?.SetTag(
                Tags.ShitpostBot.Repost.Match.Similarity,
                mostSimilarWhitelisted.CosineSimilarity
            );
            logger.LogDebug(
                "Similarity of {Similarity:0.00000000} with {ImagePostId}, which is whitelisted",
                mostSimilarWhitelisted?.CosineSimilarity,
                mostSimilarWhitelisted?.ImagePostId
            );
            return;
        }

        var mostSimilar = await dbContext
            .ImagePost.AsNoTracking()
            .ImagePostsWithClosestFeatureVector(
                postToBeEvaluated.PostedOn,
                postToBeEvaluated.Image.ImageFeatures!.FeatureVector
            )
            .FirstOrDefaultAsync(context.CancellationToken);

        if (mostSimilar?.CosineSimilarity >= (double)options.Value.RepostSimilarityThreshold)
        {
            Activity.Current?.SetTag(Tags.ShitpostBot.Repost.Outcome, "repost_detected");
            Activity.Current?.SetTag(
                Tags.ShitpostBot.Repost.Match.ImagePost.Id,
                mostSimilar.ImagePostId
            );
            Activity.Current?.SetTag(
                Tags.ShitpostBot.Repost.Match.Similarity,
                mostSimilar.CosineSimilarity
            );

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

            return;
        }

        Activity.Current?.SetTag(Tags.ShitpostBot.Repost.Outcome, "no_repost");
    }
}
