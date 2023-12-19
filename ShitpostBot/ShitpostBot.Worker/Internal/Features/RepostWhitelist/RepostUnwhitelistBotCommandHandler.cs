using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Worker.Core;

namespace ShitpostBot.Worker.Features.RepostWhitelist;

public class RepostUnwhitelistBotCommandHandler(
    ILogger<RepostUnwhitelistBotCommandHandler> logger,
    IPostsReader postsReader,
    IChatClient chatClient,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : IBotCommandHandler
{
    public string? GetHelpMessage() => $"`repost unwhitelist` - removes a post from the whitelist";

    public async Task<bool> TryHandle(MessageIdentification commandMessageIdentification, MessageIdentification? referencedMessageIdentification,
        BotCommand command)
    {
        if (command.Command != "repost unwhitelist")
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
                "Invalid usage: you need to reply to a post to unwhitelist it"
            );

            return true;
        }

        var post = await postsReader.All()
            .Where(x => x.ChatMessageId == referencedMessageIdentification.MessageId)
            .SingleOrDefaultAsync();

        if (post is null)
        {
            await chatClient.SendMessage(
                messageDestination,
                "This post is not tracked"
            );

            return true;
        }

        var existingWhitelistedPost = await unitOfWork.WhitelistedPostsRepository.GetByPostId(post.Id);
        if (existingWhitelistedPost is null)
        {
            await chatClient.SendMessage(
                messageDestination,
                "This post is not whitelisted"
            );

            return true;
        }
        

        await unitOfWork.WhitelistedPostsRepository.RemoveAsync(existingWhitelistedPost);
        await unitOfWork.SaveChangesAsync();

        logger.LogDebug("Removed WhitelistedPost {WhitelistedPost}", existingWhitelistedPost);

        return true;
    }
}