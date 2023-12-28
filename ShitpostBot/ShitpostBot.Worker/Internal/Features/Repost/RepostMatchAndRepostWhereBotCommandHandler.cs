using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Worker.Core;

namespace ShitpostBot.Worker.Features.Repost;

public class RepostMatchAndRepostWhereBotCommandHandler(
    IPostsReader postsReader,
    IImagePostsReader imagePostsReader,
    ILinkPostsReader linkPostsReader,
    IChatClient chatClient,
    IOptions<RepostServiceOptions> options)
    : IBotCommandHandler
{
    public string? GetHelpMessage() =>
        "`repost match` / `repost where` - shows maximum match value of the replied post with existing posts during the repost window";

    public async Task<bool> TryHandle(MessageIdentification commandMessageIdentification, MessageIdentification? referencedMessageIdentification,
        BotCommand command)
    {
        if (command.Command != "repost match" && command.Command != "repost where")
        {
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

        switch (post)
        {
            case LinkPost linkPost:
            {
                var mostSimilar = await linkPostsReader
                    .ClosestToLinkPostWithUri(linkPost.PostedOn, linkPost.Link.LinkProvider, linkPost.Link.LinkUri)
                    .FirstOrDefaultAsync();

                if (mostSimilar?.Similarity >= (double)options.Value.RepostSimilarityThreshold)
                {
                    await chatClient.SendMessage(
                        messageDestination,
                        $"Match of `{mostSimilar.Similarity}` with {mostSimilar.ChatMessageIdentifier.GetUri()} posted {chatClient.Utils.RelativeTimestamp(mostSimilar.PostedOn)}"
                    );
                }

                break;
            }
            case ImagePost imagePost:
            {
                var mostSimilarWhitelisted = await imagePostsReader
                    .ClosestWhitelistedToImagePostWithFeatureVector(imagePost.PostedOn, imagePost.Image.ImageFeatures!.FeatureVector)
                    .FirstOrDefaultAsync();

                if (mostSimilarWhitelisted?.CosineSimilarity >= (double)options.Value.RepostSimilarityThreshold)
                {
                    await chatClient.SendMessage(
                        messageDestination,
                        $"Match of `{mostSimilarWhitelisted.CosineSimilarity}` with {mostSimilarWhitelisted.ChatMessageIdentifier.GetUri()}, which is whitelisted"
                    );
                }

                var mostSimilar = await imagePostsReader
                    .ClosestToImagePostWithFeatureVector(imagePost.PostedOn, imagePost.Image.ImageFeatures!.FeatureVector)
                    .FirstOrDefaultAsync();

                if (mostSimilar?.CosineSimilarity >= (double)options.Value.RepostSimilarityThreshold)
                {
                    await chatClient.SendMessage(
                        messageDestination,
                        $"Match of `{mostSimilar.CosineSimilarity}` with {mostSimilar.ChatMessageIdentifier.GetUri()} posted {chatClient.Utils.RelativeTimestamp(mostSimilar.PostedOn)}"
                    );
                }

                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }

        return true;
    }
}