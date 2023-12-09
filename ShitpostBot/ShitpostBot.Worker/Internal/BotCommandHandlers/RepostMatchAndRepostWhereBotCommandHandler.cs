using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Worker;

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

        string message;
        double? similarity;
        switch (post)
        {
            case LinkPost linkPost:
            {
                var mostSimilar = linkPostsReader
                    .ClosestToLinkPostWithUri(linkPost.PostedOn, linkPost.Link.LinkProvider, linkPost.Link.LinkUri)
                    .FirstOrDefault();

                similarity = mostSimilar?.Similarity;
                message = $"Match of `{similarity}` with {mostSimilar?.ChatMessageIdentifier.GetUri()}";

                break;
            }
            case ImagePost imagePost:
            {
                var mostSimilar = imagePostsReader
                    .ClosestToImagePostWithFeatureVector(imagePost.PostedOn, imagePost.Image.ImageFeatures!.FeatureVector)
                    .FirstOrDefault();

                similarity = mostSimilar?.CosineSimilarity;
                message = $"Match of `{similarity}` with {mostSimilar?.ChatMessageIdentifier.GetUri()}";

                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (similarity >= (double)options.Value.RepostSimilarityThreshold)
        {
            await chatClient.SendMessage(
                messageDestination,
                message
            );
        }

        return true;
    }
}