using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Worker.Core;

public class ChatMessageCreatedListener(
    ILogger<ChatMessageCreatedListener> logger,
    MessageRouter router)
    : IChatMessageCreatedListener
{
    public async Task HandleMessageCreatedAsync(MessageCreateEventArgs e)
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

        MessageIdentification? repliedTo = null;
        if (msg.Reference?.Message is { } referenced)
        {
            repliedTo = new MessageIdentification(
                guildId,
                channelId,
                referenced.Author.Id,
                referenced.Id
            );
        }

        logger.LogDebug("Created: '{MessageId}' '{MessageContent}'", msg.Id, msg.Content);

        var incoming = new IncomingMessage(
            identification,
            repliedTo,
            msg.Content,
            msg.Attachments
                .Select(a => new Attachment(a.Id, new Uri(a.Url), a.MediaType, a.Width, a.Height))
                .ToList(),
            msg.Embeds
                .Where(e => e.Url != null)
                .Select(e => new Embed(e.Url!))
                .ToList(),
            msg.CreationTimestamp
        );

        await router.RouteCreate(incoming);
    }
}