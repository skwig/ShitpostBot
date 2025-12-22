using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ShitpostBot.Application.Extensions;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.BotCommands.Repost;

public class RepostMatchAndRepostWhereBotCommandHandler(
    IDbContext dbContext,
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

        var post = await dbContext.Post
            .AsNoTracking()
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
                    var mostSimilar = await dbContext.LinkPost
                        .AsNoTracking()
                        .ClosestToLinkPostWithUri(linkPost.PostedOn, linkPost.Link.LinkProvider, linkPost.Link.LinkUri)
                        .FirstOrDefaultAsync();

                    if (mostSimilar?.Similarity >= (double)options.Value.RepostSimilarityThreshold)
                    {
                        await chatClient.SendMessage(
                            messageDestination,
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
                        .FirstOrDefaultAsync();

                    if (mostSimilarWhitelisted?.CosineSimilarity >= (double)options.Value.RepostSimilarityThreshold)
                    {
                        await chatClient.SendMessage(
                            messageDestination,
                            $"Match of `{mostSimilarWhitelisted.CosineSimilarity:0.00000000}` with {mostSimilarWhitelisted.ChatMessageIdentifier.GetUri()}, which is whitelisted"
                        );
                        return true;
                    }

                    var mostSimilar = await dbContext.ImagePost
                        .AsNoTracking()
                        .ClosestToImagePostWithFeatureVector(imagePost.PostedOn, imagePost.Image.ImageFeatures!.FeatureVector)
                        .FirstOrDefaultAsync();

                    if (mostSimilar?.CosineSimilarity >= (double)options.Value.RepostSimilarityThreshold)
                    {
                        await chatClient.SendMessage(
                            messageDestination,
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
            messageDestination,
            $"Not a repost"
        );
        return true;
    }
}