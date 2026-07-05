using Microsoft.EntityFrameworkCore;
using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Extensions;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.Repost;

public class RepostUnwhitelistCommand(
    ILogger<RepostUnwhitelistCommand> logger,
    IDbContext dbContext,
    IChatClient chatClient,
    IUnitOfWork unitOfWork)
    : BotCommandFeature(chatClient)
{
    public override string? HelpMessage => "`repost unwhitelist` - removes a post from the whitelist";

    protected override async Task<bool> TryHandleCommand(
        MessageIdentification commandMessageIdentification,
        string command,
        MessageIdentification? referenced,
        CancellationToken ct)
    {
        if (command != "repost unwhitelist")
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
                "Invalid usage: you need to reply to a post to unwhitelist it"
            );

            return true;
        }

        var post = await dbContext.Post
            .AsNoTracking()
            .Where(x => x.ChatMessageId == referenced.MessageId)
            .SingleOrDefaultAsync(ct);

        if (post is null)
        {
            await chatClient.SendMessage(
                destination,
                "This post is not tracked"
            );

            return true;
        }

        var existingWhitelistedPost = await dbContext.WhitelistedPost.GetByPostId(post.Id);
        if (existingWhitelistedPost is null)
        {
            await chatClient.SendMessage(
                destination,
                "This post is not whitelisted"
            );

            return true;
        }

        dbContext.WhitelistedPost.Remove(existingWhitelistedPost);
        await unitOfWork.SaveChangesAsync(ct);

        await chatClient.SendMessage(
            destination,
            "Unwhitelisted"
        );

        logger.LogDebug("Removed WhitelistedPost {WhitelistedPost}", existingWhitelistedPost);

        return true;
    }
}