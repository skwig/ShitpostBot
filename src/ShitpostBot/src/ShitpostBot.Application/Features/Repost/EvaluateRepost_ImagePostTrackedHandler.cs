using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pgvector;
using ShitpostBot.Application.Services;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Messages;
using ShitpostBot.Worker;

namespace ShitpostBot.Application.Features.Repost;

internal class EvaluateRepost_ImagePostTrackedHandler(
    ILogger<EvaluateRepost_ImagePostTrackedHandler> logger,
    IImageFeatureExtractorApi imageFeatureExtractorApi,
    IUnitOfWork unitOfWork,
    IOptions<RepostServiceOptions> options,
    IChatClient chatClient,
    IDateTimeProvider dateTimeProvider,
    IImagePostsReader imagePostsReader)
    : IConsumer<ImagePostTracked>
{
    private static readonly string[] RepostReactions =
    {
        ":police_car:",
        ":rotating_light:"
    };

    public async Task Consume(ConsumeContext<ImagePostTracked> context)
    {
        var postToBeEvaluated = await unitOfWork.ImagePostsRepository.GetById(context.Message.ImagePostId);
        if (postToBeEvaluated == null)
        {
            throw new InvalidOperationException($"ImagePost {context.Message.ImagePostId} not found");
        }

        var extractImageFeaturesResponse = await imageFeatureExtractorApi.ExtractImageFeaturesAsync(postToBeEvaluated.Image.ImageUri.ToString());

        var imageFeatures = new ImageFeatures(new Vector(extractImageFeaturesResponse.ImageFeatures));
        postToBeEvaluated.SetImageFeatures(imageFeatures, dateTimeProvider.UtcNow);

        await unitOfWork.SaveChangesAsync(context.CancellationToken);

        var mostSimilarWhitelisted = await imagePostsReader
            .ClosestWhitelistedToImagePostWithFeatureVector(postToBeEvaluated.PostedOn, postToBeEvaluated.Image.ImageFeatures!.FeatureVector)
            .FirstOrDefaultAsync(context.CancellationToken);

        if (mostSimilarWhitelisted?.CosineSimilarity >= (double)options.Value.RepostSimilarityThreshold)
        {
            logger.LogDebug("Similarity of {Similarity:0.00000000} with {ImagePostId}, which is whitelisted", mostSimilarWhitelisted?.CosineSimilarity,
                mostSimilarWhitelisted?.ImagePostId);
            return;
        }

        var mostSimilar = await imagePostsReader
            .ClosestToImagePostWithFeatureVector(postToBeEvaluated.PostedOn, postToBeEvaluated.Image.ImageFeatures!.FeatureVector)
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
