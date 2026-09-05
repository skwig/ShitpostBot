using System.Diagnostics;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Extensions;
using ShitpostBot.Infrastructure.Messages;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.Repost;

public class EvaluateLinkRepostConsumer(
    IDbContext dbContext,
    IOptions<RepostServiceOptions> options,
    IChatClient chatClient
) : IConsumer<LinkPostTracked>
{
    private static readonly string[] RepostReactions = [":police_car:", ":rotating_light:"];

    public async Task Consume(ConsumeContext<LinkPostTracked> context)
    {
        using var activity = ShitpostBotActivitySource.Instance.StartActivity(
            nameof(EvaluateLinkRepostConsumer),
            ActivityKind.Consumer
        );
        Activity.Current?.SetTag(Tags.Messaging.System, "masstransit");
        Activity.Current?.SetTag(Tags.ShitpostBot.LinkPost.Id, context.Message.LinkPostId);

        var postToBeEvaluated = await dbContext.LinkPost.GetById(
            context.Message.LinkPostId,
            context.CancellationToken
        );
        if (postToBeEvaluated == null)
        {
            Activity.Current?.SetTag(Tags.ShitpostBot.Repost.Outcome, "missing_post");
            throw new InvalidOperationException($"LinkPost {context.Message.LinkPostId} not found");
        }

        Activity.Current?.SetTag(Tags.Discord.Guild.Id, postToBeEvaluated.ChatGuildId);
        Activity.Current?.SetTag(Tags.Discord.Channel.Id, postToBeEvaluated.ChatChannelId);
        Activity.Current?.SetTag(Tags.Discord.Message.Id, postToBeEvaluated.ChatMessageId);
        Activity.Current?.SetTag(Tags.Discord.User.Id, postToBeEvaluated.PosterId);

        var mostSimilar = await dbContext
            .LinkPost.AsNoTracking()
            .ClosestToLinkPostWithUri(
                postToBeEvaluated.PostedOn,
                postToBeEvaluated.Link.LinkProvider,
                postToBeEvaluated.Link.LinkUri
            )
            .FirstOrDefaultAsync(context.CancellationToken);

        if (mostSimilar?.Similarity >= (double)options.Value.RepostSimilarityThreshold)
        {
            Activity.Current?.SetTag(Tags.ShitpostBot.Repost.Outcome, "repost_detected");
            Activity.Current?.SetTag(
                Tags.ShitpostBot.Repost.Match.LinkPost.Id,
                mostSimilar.LinkPostId
            );
            Activity.Current?.SetTag(
                Tags.ShitpostBot.Repost.Match.Similarity,
                mostSimilar.Similarity
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
