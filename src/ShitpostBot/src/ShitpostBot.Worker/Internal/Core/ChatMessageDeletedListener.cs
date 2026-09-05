using System.Diagnostics;
using DSharpPlus.EventArgs;
using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Worker.Core;

public class ChatMessageDeletedListener(
    ILogger<ChatMessageDeletedListener> logger,
    IDateTimeProvider dateTimeProvider,
    MessageRouter router
) : IChatMessageDeletedListener
{
    public async Task HandleMessageDeletedAsync(MessageDeleteEventArgs message)
    {
        if (message.Message?.Author == null)
        {
            return;
        }

        if (message.Message.Author.IsBot)
        {
            return;
        }

        var guildId = message.Guild?.Id ?? 0;
        var channelId = message.Channel.Id;

        using var activity = ShitpostBotActivitySource.Instance.StartActivity(
            nameof(ChatMessageDeletedListener),
            ActivityKind.Consumer
        );
        Activity.Current?.SetTag(Tags.Messaging.System, "discord");
        Activity.Current?.SetTag(Tags.Discord.Guild.Id, guildId);
        Activity.Current?.SetTag(Tags.Discord.Channel.Id, channelId);
        Activity.Current?.SetTag(Tags.Discord.Message.Id, message.Message.Id);
        Activity.Current?.SetTag(Tags.Discord.User.Id, message.Message.Author.Id);

        var deleted = new DeletedMessage(
            new MessageIdentification(
                guildId,
                channelId,
                message.Message.Author.Id,
                message.Message.Id
            ),
            message.Message.Content ?? "",
            message.Message.CreationTimestamp,
            dateTimeProvider.UtcNow
        );

        logger.LogDebug("Deleted: '{MessageId}'", message.Message.Id);

        await router.RouteDelete(deleted);
    }
}
