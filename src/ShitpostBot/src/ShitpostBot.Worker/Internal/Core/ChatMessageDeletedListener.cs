using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using ShitpostBot.Application.Features.DeletedMessages;
using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Worker.Core;

public class ChatMessageDeletedListener(
    ILogger<ChatMessageDeletedListener> logger,
    MessageRouter router,
    DeletedMessageStore deletedMessageStore
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

        deletedMessageStore.Store(
            channelId,
            new DeletedMessage(
                message.Message.Author.Id,
                message.Message.Author.Username,
                message.Message.Content ?? "",
                message.Message.CreationTimestamp
            )
        );

        var messageIdentification = new MessageIdentification(
            guildId,
            channelId,
            message.Message.Author.Id,
            message.Message.Id
        );

        logger.LogDebug("Deleted: '{MessageId}'", message.Message.Id);

        await router.RouteDelete(messageIdentification);
    }
}
