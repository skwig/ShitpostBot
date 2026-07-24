using Microsoft.EntityFrameworkCore;
using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Extensions;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.Repost;

public class RepostWhitelistCommand(
    ILogger<RepostWhitelistCommand> logger,
    IDbContext dbContext,
    IChatClient chatClient,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider
) : BotCommandFeature(chatClient)
{
    public override string? HelpMessage =>
        "`repost whitelist` - whitelists a post, making posts similar to it not be marked as reposts";

    protected override async Task<bool> TryHandleCommand(
        MessageIdentification commandMessageIdentification,
        string command,
        MessageIdentification? referenced,
        CancellationToken ct
    )
    {
        if (command != "repost whitelist")
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
                "Invalid usage: you need to reply to a post to whitelist it"
            );

            return true;
        }

        var post = await dbContext
            .Post.AsNoTracking()
            .Where(x => x.ChatMessageId == referenced.MessageId)
            .SingleOrDefaultAsync(ct);

        if (post is null)
        {
            await chatClient.SendMessage(destination, "This post is not tracked");

            return true;
        }

        if (post is not ImagePost imagePost)
        {
            await chatClient.SendMessage(destination, "Non-image posts are not supported");

            return true;
        }

        var existingWhitelistedPost = await dbContext
            .WhitelistedPost.AsNoTracking()
            .GetByPostId(post.Id);
        if (existingWhitelistedPost is not null)
        {
            await chatClient.SendMessage(destination, "This post is already whitelisted");

            return true;
        }

        var newWhitelistedPost = WhitelistedPost.Create(
            imagePost,
            dateTimeProvider.UtcNow,
            commandMessageIdentification.PosterId
        );

        dbContext.WhitelistedPost.Add(newWhitelistedPost);
        await unitOfWork.SaveChangesAsync(ct);

        await chatClient.SendMessage(destination, "Whitelisted");

        logger.LogDebug("Tracked WhitelistedPost {NewWhitelistedPost}", newWhitelistedPost);

        return true;
    }
}
