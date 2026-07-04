using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ShitpostBot.Application.Extensions;
using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Extensions;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.Repost;

public class RepostMatchFeature(
    IDbContext dbContext,
    IChatClient chatClient,
    IOptions<RepostServiceOptions> options)
    : BotCommandFeature(chatClient)
{
    public override string? HelpMessage =>
        "`repost match` / `repost where` - shows maximum match value of the replied post with existing posts during the repost window";

    protected override async Task<bool> TryHandleCommand(
        MessageIdentification commandMessageIdentification,
        string command,
        MessageIdentification? referenced,
        CancellationToken ct)
    {
        if (command != "repost match" && command != "repost where")
        {
            return false;
        }

        var destination = new MessageDestination(
            commandMessageIdentification.GuildId,
            commandMessageIdentification.ChannelId,
            commandMessageIdentification.MessageId
        );

        if (referenced == null)
        {
            await chatClient.SendMessage(
                destination,
                "Invalid usage: you need to reply to a post to get the match value"
            );

            return true;
        }

        var post = await dbContext.Post
            .AsNoTracking()
            .Where(x => x.ChatMessageId == referenced.MessageId)
            .SingleOrDefaultAsync(ct);

        if (post == null)
        {
            await chatClient.SendMessage(
                destination,
                "This post is not tracked"
            );

            return true;
        }

        switch (post)
        {
            case LinkPost linkPost:
                {
                    var mostSimilar = await dbContext.LinkPost
                        .AsNoTracking()
                        .ClosestToLinkPostWithUri(linkPost.PostedOn, linkPost.Link.LinkProvider, linkPost.Link.LinkUri)
                        .FirstOrDefaultAsync(ct);

                    if (mostSimilar?.Similarity >= (double)options.Value.RepostSimilarityThreshold)
                    {
                        await chatClient.SendMessage(
                            destination,
                            $"Match of `{mostSimilar.Similarity:0.00000000}` with {mostSimilar.ChatMessageIdentifier.GetUri()} posted {chatClient.Utils.RelativeTimestamp(mostSimilar.PostedOn)}"
                        );
                        return true;
                    }

                    break;
                }
            case ImagePost imagePost:
                {
                    var mostSimilarWhitelisted = await dbContext.WhitelistedPost
                        .AsNoTracking()
                        .ClosestWhitelistedToImagePostWithFeatureVector(imagePost.PostedOn, imagePost.Image.ImageFeatures!.FeatureVector)
                        .FirstOrDefaultAsync(ct);

                    if (mostSimilarWhitelisted?.CosineSimilarity >= (double)options.Value.RepostSimilarityThreshold)
                    {
                        await chatClient.SendMessage(
                            destination,
                            $"Match of `{mostSimilarWhitelisted.CosineSimilarity:0.00000000}` with {mostSimilarWhitelisted.ChatMessageIdentifier.GetUri()}, which is whitelisted"
                        );
                        return true;
                    }

                    var mostSimilar = await dbContext.ImagePost
                        .AsNoTracking()
                        .ImagePostsWithClosestFeatureVector(imagePost.PostedOn, imagePost.Image.ImageFeatures!.FeatureVector)
                        .FirstOrDefaultAsync(ct);

                    if (mostSimilar?.CosineSimilarity >= (double)options.Value.RepostSimilarityThreshold)
                    {
                        await chatClient.SendMessage(
                            destination,
                            $"Match of `{mostSimilar.CosineSimilarity:0.00000000}` with {mostSimilar.ChatMessageIdentifier.GetUri()} posted {chatClient.Utils.RelativeTimestamp(mostSimilar.PostedOn)}"
                        );
                        return true;
                    }

                    break;
                }
            default:
                throw new ArgumentOutOfRangeException();
        }

        await chatClient.SendMessage(
            destination,
            "Not a repost"
        );
        return true;
    }
}