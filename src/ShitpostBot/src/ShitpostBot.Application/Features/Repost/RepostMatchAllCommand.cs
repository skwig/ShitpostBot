using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ShitpostBot.Application.Extensions;
using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Extensions;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.Repost;

public class RepostMatchAllCommand(
    IDbContext dbContext,
    IChatClient chatClient,
    IOptions<RepostServiceOptions> options
) : BotCommandFeature(chatClient)
{
    private const int ResultCount = 5;

    public override string? HelpMessage =>
        "`repost match all [cos|l2]` - shows maximum cosine similarity of the replied post with existing posts";

    protected override async Task<bool> TryHandleCommand(
        MessageIdentification commandMessageIdentification,
        string command,
        MessageIdentification? referenced,
        CancellationToken ct
    )
    {
        OrderBy orderBy;
        switch (command)
        {
            case "repost match all":
            case "repost match all cos":
                orderBy = OrderBy.CosineDistance;
                break;
            case "repost match all l2":
                orderBy = OrderBy.L2Distance;
                break;
            default:
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

        var post = await dbContext
            .Post.AsNoTracking()
            .Where(x => x.ChatMessageId == referenced.MessageId)
            .SingleOrDefaultAsync(ct);

        if (post == null)
        {
            await chatClient.SendMessage(destination, "This post is not tracked");

            return true;
        }

        await chatClient.SendMessage(
            destination,
            $"Starting to match. Čekej píčo {chatClient.Utils.Emoji(":PauseChamp:")} ..."
        );

        switch (post)
        {
            case LinkPost linkPost:
                {
                    var similarPosts = await dbContext
                        .LinkPost.AsNoTracking()
                        .ClosestToLinkPostWithUri(
                            linkPost.PostedOn,
                            linkPost.Link.LinkProvider,
                            linkPost.Link.LinkUri
                        )
                        .Take(ResultCount)
                        .ToListAsync(ct);

                    await chatClient.SendMessage(
                        destination,
                        "Higher is a closer match:\n"
                            + string.Join(
                                "\n",
                                similarPosts.Select(
                                    (p, i) =>
                                        $"{i + 1}. Match of `{p.Similarity:0.00000000}` with {p.ChatMessageIdentifier.GetUri()} posted {chatClient.Utils.RelativeTimestamp(p.PostedOn)}"
                                )
                            )
                    );

                    break;
                }
            case ImagePost imagePost:
                {
                    var similarPosts = await dbContext
                        .ImagePost.AsNoTracking()
                        .ImagePostsWithClosestFeatureVector(
                            imagePost.PostedOn,
                            imagePost.Image.ImageFeatures!.FeatureVector,
                            orderBy
                        )
                        .Take(ResultCount)
                        .ToListAsync(ct);

                    switch (orderBy)
                    {
                        case OrderBy.CosineDistance:
                            {
                                var similarWhitelisted = (
                                    await dbContext
                                        .WhitelistedPost.AsNoTracking()
                                        .ClosestWhitelistedToImagePostWithFeatureVector(
                                            imagePost.PostedOn,
                                            imagePost.Image.ImageFeatures!.FeatureVector
                                        )
                                        .Take(ResultCount)
                                        .ToListAsync(ct)
                                )
                                    // Do this on the client side, as EF has issues with working with similarities after .Select(), which is done in .ClosestWhitelistedToImagePostWithFeatureVector()
                                    .Where(x =>
                                        x.CosineSimilarity
                                        >= (double)options.Value.RepostSimilarityThreshold
                                    )
                                    .ToList();

                                var whitelistedAppendix = similarWhitelisted.Any()
                                    ? "\n"
                                        + "Additionally, it is similar to whitelisted posts:\n"
                                        + string.Join(
                                            "\n",
                                            similarWhitelisted.Select(
                                                (p, i) =>
                                                    $"{i + 1}. Match of `{p.CosineSimilarity:0.00000000}` with {p.ChatMessageIdentifier.GetUri()} posted {chatClient.Utils.RelativeTimestamp(p.PostedOn)}"
                                            )
                                        )
                                    : string.Empty;

                                await chatClient.SendMessage(
                                    destination,
                                    "Higher is a closer match (cosine distance):\n"
                                        + string.Join(
                                            "\n",
                                            similarPosts.Select(
                                                (p, i) =>
                                                    $"{i + 1}. Match of `{p.CosineSimilarity:0.00000000}` with {p.ChatMessageIdentifier.GetUri()} posted {chatClient.Utils.RelativeTimestamp(p.PostedOn)}"
                                            )
                                        )
                                        + whitelistedAppendix
                                );
                                break;
                            }
                        case OrderBy.L2Distance:
                            await chatClient.SendMessage(
                                destination,
                                "Lower is a closer match (L2 distance):\n"
                                    + string.Join(
                                        "\n",
                                        similarPosts.Select(
                                            (p, i) =>
                                                $"{i + 1}. Match of `{p.L2Distance}` with {p.ChatMessageIdentifier.GetUri()} posted {chatClient.Utils.RelativeTimestamp(p.PostedOn)}"
                                        )
                                    )
                            );
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    break;
                }
            default:
                throw new ArgumentOutOfRangeException();
        }

        return true;
    }
}