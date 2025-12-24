using Microsoft.EntityFrameworkCore;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.BotCommands.RepostWhitelist;

public class RepostWhitelistBotCommandHandler(
    ILogger<RepostWhitelistBotCommandHandler> logger,
    IDbContext dbContext,
    IChatClient chatClient,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : IBotCommandHandler
{
    public string? GetHelpMessage() => $"`repost whitelist` - whitelists a post, making posts similar to it not be marked as reposts";

    public async Task<bool> TryHandle(
        MessageIdentification commandMessageIdentification,
        MessageIdentification? referencedMessageIdentification,
        BotCommand command,
        bool isEdit = false,
        ulong? botResponseMessageId = null)
    {
        if (command.Command != "repost whitelist")
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
                "Invalid usage: you need to reply to a post to whitelist it"
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

        if (post is not ImagePost imagePost)
        {
            await chatClient.SendMessage(
                messageDestination,
                "Non-image posts are not supported"
            );

            return true;
        }

        var existingWhitelistedPost = await dbContext.WhitelistedPost.AsNoTracking().GetByPostId(post.Id);
        if (existingWhitelistedPost is not null)
        {
            await chatClient.SendMessage(
                messageDestination,
                "This post is already whitelisted"
            );

            return true;
        }

        var newWhitelistedPost = WhitelistedPost.Create(
            imagePost,
            dateTimeProvider.UtcNow,
            commandMessageIdentification.PosterId
        );

        dbContext.WhitelistedPost.Add(newWhitelistedPost);
        await unitOfWork.SaveChangesAsync();

        await chatClient.SendMessage(
            messageDestination,
            "Whitelisted"
        );

        logger.LogDebug("Tracked WhitelistedPost {NewWhitelistedPost}", newWhitelistedPost);

        return true;
    }
}