using DSharpPlus.EventArgs;
using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Worker.Core;

public class ChatMessageDeletedListener(
    ILogger<ChatMessageDeletedListener> logger,
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

        var deleted = new DeletedMessage(
            new MessageIdentification(
                guildId,
                channelId,
                message.Message.Author.Id,
                message.Message.Id
            ),
            message.Message.Content ?? "",
            message.Message.CreationTimestamp,
            DateTimeOffset.UtcNow
        );

        logger.LogDebug("Deleted: '{MessageId}'", message.Message.Id);

        await router.RouteDelete(deleted);
    }
}
