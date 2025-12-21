using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Messages;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.Repost;

public class EvaluateRepost_LinkPostTrackedHandler(
    IUnitOfWork unitOfWork,
    IOptions<RepostServiceOptions> options,
    ILinkPostsReader linkPostsReader,
    IChatClient chatClient)
    : IConsumer<LinkPostTracked>
{
    private static readonly string[] RepostReactions =
    {
        ":police_car:",
        ":rotating_light:"
    };

    public async Task Consume(ConsumeContext<LinkPostTracked> context)
    {
        var postToBeEvaluated = await unitOfWork.LinkPostsRepository.GetById(context.Message.LinkPostId);
        if (postToBeEvaluated == null)
        {
            throw new InvalidOperationException($"LinkPost {context.Message.LinkPostId} not found");
        }

        var mostSimilar = await linkPostsReader
            .ClosestToLinkPostWithUri(postToBeEvaluated.PostedOn, postToBeEvaluated.Link.LinkProvider, postToBeEvaluated.Link.LinkUri)
            .FirstOrDefaultAsync(context.CancellationToken);

        if (mostSimilar?.Similarity >= (double)options.Value.RepostSimilarityThreshold)
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
