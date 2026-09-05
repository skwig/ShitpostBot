using System.Diagnostics;
using DSharpPlus.EventArgs;
using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Worker.Core;

public class ChatMessageUpdatedListener(
    ILogger<ChatMessageUpdatedListener> logger,
    MessageRouter router
) : IChatMessageUpdatedListener
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

        using var activity = ShitpostBotActivitySource.Instance.StartActivity(
            nameof(ChatMessageUpdatedListener),
            ActivityKind.Consumer
        );
        Activity.Current?.SetTag(Tags.Messaging.System, "discord");
        Activity.Current?.SetTag(Tags.Discord.Guild.Id, guildId);
        Activity.Current?.SetTag(Tags.Discord.Channel.Id, channelId);
        Activity.Current?.SetTag(Tags.Discord.Message.Id, msg.Id);
        Activity.Current?.SetTag(Tags.Discord.User.Id, msg.Author.Id);

        var identification = new MessageIdentification(guildId, channelId, msg.Author.Id, msg.Id);

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

        logger.LogDebug("Updated: '{MessageId}' '{MessageContent}'", msg.Id, msg.Content);

        var old = e.MessageBefore is not null
            ? new IncomingMessage(
                identification,
                repliedTo,
                e.MessageBefore.Content,
                e.MessageBefore.Attachments.Select(a => new Attachment(
                        a.Id,
                        new Uri(a.Url),
                        a.MediaType,
                        a.Width,
                        a.Height
                    ))
                    .ToList(),
                e.MessageBefore.Embeds.Where(e => e.Url != null)
                    .Select(e => new Embed(e.Url!))
                    .ToList(),
                e.MessageBefore.CreationTimestamp
            )
            : new IncomingMessage(identification, repliedTo, null, [], [], msg.CreationTimestamp);

        var updated = new IncomingMessage(
            identification,
            repliedTo,
            msg.Content,
            msg.Attachments.Select(a => new Attachment(
                    a.Id,
                    new Uri(a.Url),
                    a.MediaType,
                    a.Width,
                    a.Height
                ))
                .ToList(),
            msg.Embeds.Where(e => e.Url != null).Select(e => new Embed(e.Url!)).ToList(),
            msg.CreationTimestamp
        );

        await router.RouteUpdate(old, updated);
    }
}
