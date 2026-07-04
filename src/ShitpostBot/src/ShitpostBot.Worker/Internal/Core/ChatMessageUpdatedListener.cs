using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Worker.Core;

public class ChatMessageUpdatedListener(
    ILogger<ChatMessageUpdatedListener> logger,
    MessageRouter router)
    : IChatMessageUpdatedListener
{
    public async Task HandleMessageUpdatedAsync(MessageUpdateEventArgs e)
    {
        var msg = e.Message;

        if (msg.Author.IsBot)
        {
            return;
        }

        var guildId = e.Guild?.Id ?? 0;
        var channelId = e.Channel.Id;

        var identification = new MessageIdentification(
            guildId,
            channelId,
            msg.Author.Id,
            msg.Id
        );

        logger.LogDebug("Updated: '{MessageId}' '{MessageContent}'", msg.Id, msg.Content);

        var incoming = new IncomingMessage(
            identification,
            null,
            msg.Content,
            msg.Attachments
                .Select(a => new Attachment(a.Id, new Uri(a.Url), a.MediaType))
                .ToList(),
            msg.Embeds
                .Where(e => e.Url != null)
                .Select(e => new Embed(new Uri(e.Url.ToString())))
                .ToList(),
            msg.CreationTimestamp
        );

        await router.RouteCreate(incoming);
    }
}
