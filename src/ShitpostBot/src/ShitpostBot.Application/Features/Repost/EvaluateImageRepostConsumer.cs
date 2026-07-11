using Grpc.Core;
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
    ImageFeatureExtractor.ImageFeatureExtractorClient imageFeatureExtractorClient,
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
        var postToBeEvaluated = await dbContext.ImagePost.GetById(
            context.Message.ImagePostId,
            context.CancellationToken
        );
        if (postToBeEvaluated == null)
        {
            throw new InvalidOperationException(
                $"ImagePost {context.Message.ImagePostId} not found"
            );
        }

        try
        {
            var response = await imageFeatureExtractorClient.ProcessImageAsync(
                new ProcessImageRequest
                {
                    ImageUrl = postToBeEvaluated.Image.ImageUri.ToString(),
                    Embedding = true,
                    Caption = false,
                    Ocr = false,
                },
                deadline: DateTime.UtcNow.AddSeconds(30),
                cancellationToken: context.CancellationToken
            );

            var embedding = response.Embedding.Count > 0
                ? response.Embedding.ToArray()
                : throw new InvalidOperationException("ML service did not return embedding");

            postToBeEvaluated.SetImageFeatures(
                new ImageFeatures(response.ModelName, new Vector(embedding)),
                dateTimeProvider.UtcNow
            );

            await unitOfWork.SaveChangesAsync(context.CancellationToken);

            metrics.LastImageEvaluationTimestamp = dateTimeProvider.UtcNow;

            if (context.Message.IsReevaluation)
            {
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
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            logger.LogError(
                "Image not found (404) for ImagePost {ImagePostId}, URL: {ImageUrl}. Clearing ImageFeatures.",
                context.Message.ImagePostId,
                postToBeEvaluated.Image.ImageUri
            );

            postToBeEvaluated.ClearImageFeatures(dateTimeProvider.UtcNow);
            await unitOfWork.SaveChangesAsync(context.CancellationToken);
            metrics.LastImageEvaluationTimestamp = dateTimeProvider.UtcNow;
        }
    }
}