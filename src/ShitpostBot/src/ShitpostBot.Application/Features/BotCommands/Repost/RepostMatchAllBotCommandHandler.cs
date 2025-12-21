using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ShitpostBot.Application.Extensions;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.BotCommands.Repost;

public class RepostMatchAllBotCommandHandler(
    IPostsReader postsReader,
    IImagePostsReader imagePostsReader,
    ILinkPostsReader linkPostsReader,
    IChatClient chatClient,
    IOptions<RepostServiceOptions> options)
    : IBotCommandHandler
{
    public string? GetHelpMessage() => $"`repost match all [cos|l2]` - shows maximum cosine similarity of the replied post with existing posts";

    public async Task<bool> TryHandle(MessageIdentification commandMessageIdentification, MessageIdentification? referencedMessageIdentification,
        BotCommand command)
    {
        const int resultCount = 5;

        OrderBy orderBy;
        switch (command.Command)
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

        var messageDestination = new MessageDestination(
            commandMessageIdentification.GuildId,
            commandMessageIdentification.ChannelId,
            commandMessageIdentification.MessageId
        );

        if (referencedMessageIdentification == null)
        {
            await chatClient.SendMessage(
                messageDestination,
                "Invalid usage: you need to reply to a post to get the match value"
            );

            return true;
        }

        var post = await postsReader.All()
            .Where(x => x.ChatMessageId == referencedMessageIdentification.MessageId)
            .SingleOrDefaultAsync();

        if (post == null)
        {
            await chatClient.SendMessage(
                messageDestination,
                "This post is not tracked"
            );

            return true;
        }

        await chatClient.SendMessage(messageDestination, $"Starting to match. Čekej píčo {chatClient.Utils.Emoji(":PauseChamp:")} ...");

        switch (post)
        {
            case LinkPost linkPost:
                {
                    var similarPosts = await linkPostsReader
                        .ClosestToLinkPostWithUri(linkPost.PostedOn, linkPost.Link.LinkProvider, linkPost.Link.LinkUri)
                        .Take(resultCount)
                        .ToListAsync();

                    await chatClient.SendMessage(
                        messageDestination,
                        "Higher is a closer match:\n" +
                        string.Join("\n",
                            similarPosts.Select((p, i) =>
                                $"{i + 1}. Match of `{p.Similarity:0.00000000}` with {p.ChatMessageIdentifier.GetUri()} posted {chatClient.Utils.RelativeTimestamp(p.PostedOn)}"
                            )
                        )
                    );

                    break;
                }
            case ImagePost imagePost:
                {
                    var similarPosts = await imagePostsReader
                        .ClosestToImagePostWithFeatureVector(imagePost.PostedOn, imagePost.Image.ImageFeatures!.FeatureVector, orderBy)
                        .Take(resultCount)
                        .ToListAsync();

                    switch (orderBy)
                    {
                        case OrderBy.CosineDistance:
                            {
                                var similarWhitelisted = (
                                        await imagePostsReader
                                            .ClosestWhitelistedToImagePostWithFeatureVector(imagePost.PostedOn, imagePost.Image.ImageFeatures!.FeatureVector)
                                            .Take(resultCount)
                                            .ToListAsync()
                                    )
                                    // Do this on the client side, as EF has issues with working with similarities after .Select(), which is done in .ClosestWhitelistedToImagePostWithFeatureVector()
                                    .Where(x => x.CosineSimilarity >= (double)options.Value.RepostSimilarityThreshold)
                                    .ToList();

                                var whitelistedAppendix = similarWhitelisted.Any()
                                    ? "\n" +
                                      "Additionally, it is similar to whitelisted posts:\n" +
                                      string.Join("\n",
                                          similarWhitelisted.Select((p, i) =>
                                              $"{i + 1}. Match of `{p.CosineSimilarity:0.00000000}` with {p.ChatMessageIdentifier.GetUri()} posted {chatClient.Utils.RelativeTimestamp(p.PostedOn)}"
                                          )
                                      )
                                    : string.Empty;

                                await chatClient.SendMessage(
                                    messageDestination,
                                    "Higher is a closer match (cosine distance):\n" +
                                    string.Join("\n",
                                        similarPosts.Select((p, i) =>
                                            $"{i + 1}. Match of `{p.CosineSimilarity:0.00000000}` with {p.ChatMessageIdentifier.GetUri()} posted {chatClient.Utils.RelativeTimestamp(p.PostedOn)}"
                                        )
                                    ) + whitelistedAppendix
                                );
                                break;
                            }
                        case OrderBy.L2Distance:
                            await chatClient.SendMessage(
                                messageDestination,
                                "Lower is a closer match (L2 distance):\n" +
                                string.Join("\n",
                                    similarPosts.Select((p, i) =>
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