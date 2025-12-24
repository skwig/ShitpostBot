using Microsoft.EntityFrameworkCore;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.BotCommands.RepostWhitelist;

public class RepostUnwhitelistBotCommandHandler(
    ILogger<RepostUnwhitelistBotCommandHandler> logger,
    IDbContext dbContext,
    IChatClient chatClient,
    IUnitOfWork unitOfWork)
    : IBotCommandHandler
{
    public string? GetHelpMessage() => $"`repost unwhitelist` - removes a post from the whitelist";

    public async Task<bool> TryHandle(
        MessageIdentification commandMessageIdentification,
        MessageIdentification? referencedMessageIdentification,
        BotCommand command,
        bool isEdit = false,
        ulong? botResponseMessageId = null)
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

        var post = await dbContext.Post
            .AsNoTracking()
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

        var existingWhitelistedPost = await dbContext.WhitelistedPost.GetByPostId(post.Id);
        if (existingWhitelistedPost is null)
        {
            await chatClient.SendMessage(
                messageDestination,
                "This post is not whitelisted"
            );

            return true;
        }

        dbContext.WhitelistedPost.Remove(existingWhitelistedPost);
        await unitOfWork.SaveChangesAsync();

        await chatClient.SendMessage(
            messageDestination,
            "Unwhitelisted"
        );

        logger.LogDebug("Removed WhitelistedPost {WhitelistedPost}", existingWhitelistedPost);

        return true;
    }
}