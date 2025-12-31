using System.Text.RegularExpressions;
using MediatR;
using ShitpostBot.Application.Features.BotCommands;
using ShitpostBot.Application.Features.BotCommands.Redacted;
using ShitpostBot.Application.Features.PostTracking;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Core;

public class MessageProcessor(
    ILogger<MessageProcessor> logger,
    IChatClient chatClient,
    IMediator mediator)
    : IMessageProcessor
{
    public async Task ProcessCreatedMessageAsync(MessageData messageData, CancellationToken cancellationToken = default)
    {
        var messageIdentification = new MessageIdentification(
            messageData.GuildId,
            messageData.ChannelId,
            messageData.UserId,
            messageData.MessageId);
        var referencedMessageIdentification = CreateReferencedMessageIdentification(messageData.ReferencedMessage);

        logger.LogDebug("Created: '{MessageId}' '{MessageContent}'", messageData.MessageId, messageData.Content);

        if (await TryHandleBotCommandAsync(messageIdentification, referencedMessageIdentification, messageData,
                cancellationToken)) return;
        if (await TryHandleImageAsync(messageIdentification, messageData, cancellationToken)) return;
        if (await TryHandleLinkAsync(messageIdentification, messageData, cancellationToken)) return;
        if (await TryHandleTextAsync(messageIdentification, messageData, cancellationToken)) return;
    }

    public async Task ProcessUpdatedMessageAsync(MessageData messageData, CancellationToken cancellationToken = default)
    {
        var messageIdentification = new MessageIdentification(
            messageData.GuildId,
            messageData.ChannelId,
            messageData.UserId,
            messageData.MessageId);

        logger.LogDebug("Updated: '{MessageId}' '{MessageContent}'",
            messageData.MessageId, messageData.Content);

        // Check if edited message is a bot command
        var startsWithThisBotTag = StartsWithBotMention(messageData.Content, messageData.CurrentMemberId);

        if (!startsWithThisBotTag)
        {
            return;
        }

        // Extract command (same logic as ChatMessageCreatedListener)
        var command = ExtractCommandFromContent(messageData.Content);

        if (string.IsNullOrWhiteSpace(command))
        {
            return;
        }

        // Try to find the bot's response to this message
        var botResponseMessageId = await chatClient.FindReplyToMessage(messageIdentification);

        // Get referenced message if any
        var referencedMessageIdentification = CreateReferencedMessageIdentification(messageData.ReferencedMessage);

        await mediator.Send(
            new ExecuteBotCommand(
                messageIdentification,
                referencedMessageIdentification,
                new BotCommand(command),
                botResponseMessageId is not null
                    ? new BotCommandEdit(botResponseMessageId.Value)
                    : null
            ),
            cancellationToken
        );
    }

    public async Task ProcessDeletedMessageAsync(MessageData messageData, CancellationToken cancellationToken = default)
    {
        var messageIdentification = new MessageIdentification(
            messageData.GuildId,
            messageData.ChannelId,
            messageData.UserId,
            messageData.MessageId);

        logger.LogDebug("Deleted: '{MessageId}' '{MessageContent}'", messageData.MessageId, messageData.Content);

        await mediator.Publish(new MessageDeleted(messageIdentification), cancellationToken);
    }

    private async Task<bool> TryHandleBotCommandAsync(MessageIdentification messageIdentification,
        MessageIdentification? referencedMessageIdentification,
        MessageData messageData,
        CancellationToken cancellationToken)
    {
        var startsWithThisBotTag = StartsWithBotMention(messageData.Content, messageData.CurrentMemberId);
        if (!startsWithThisBotTag)
        {
            return false;
        }

        var command = ExtractCommandFromContent(messageData.Content);

        if (string.IsNullOrWhiteSpace(command))
        {
            return false;
        }

        await mediator.Send(
            new ExecuteBotCommand(messageIdentification, referencedMessageIdentification, new BotCommand(command)),
            cancellationToken
        );

        return true;
    }

    private async Task<bool> TryHandleImageAsync(MessageIdentification messageIdentification,
        MessageData messageData,
        CancellationToken cancellationToken)
    {
        var imageAttachments = messageData.Attachments
            .Where(a => IsImageOrVideo(a.MediaType))
            .Where(a => a.Height >= 299 && a.Width >= 299)
            .ToArray();
        if (!imageAttachments.Any())
        {
            return false;
        }

        // attachment url pattern: channelId/messageId/attachmentId
        foreach (var i in imageAttachments)
        {
            var attachment = new ImageMessageAttachment(
                i.Id,
                i.FileName,
                i.Url,
                i.MediaType
            );
            await mediator.Publish(
                new ImageMessageCreated(new ImageMessage(messageIdentification, attachment,
                    messageData.Timestamp)),
                cancellationToken
            );
        }

        return true;
    }

    private async Task<bool> TryHandleLinkAsync(MessageIdentification messageIdentification,
        MessageData messageData,
        CancellationToken cancellationToken)
    {
        var embedUrls = messageData.Embeds.Where(e => e?.Url != null).Select(e => e.Url!).ToList();

        if (!embedUrls.Any() && !string.IsNullOrWhiteSpace(messageData.Content))
        {
            // try regexing as fallback
            var regexMatches = Regex.Matches(
                messageData.Content,
                @"(?:http(s)?:\/\/)?[\w.-]+(?:\.[\w\.-]+)+[\w\-\._~:/?#[\]@!\$&'\(\)\*\+,;=.]+");

            foreach (Match regexMatch in regexMatches)
            {
                embedUrls.Add(new Uri(regexMatch.Value));
            }
        }

        if (!embedUrls.Any())
        {
            return false;
        }

        foreach (var embedUrl in embedUrls)
        {
            var attachment = new LinkMessageEmbed(embedUrl);
            await mediator.Publish(
                new LinkMessageCreated(new LinkMessage(messageIdentification, attachment,
                    messageData.Timestamp)),
                cancellationToken
            );
        }

        return true;
    }

    private async Task<bool> TryHandleTextAsync(
        MessageIdentification messageIdentification,
        MessageData messageData,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(messageData.Content))
        {
            return false;
        }

        await mediator.Publish(
            new TextMessageCreated(new TextMessage(messageIdentification, messageData.Content,
                messageData.Timestamp)),
            cancellationToken
        );

        return true;
    }

    /// <summary>
    /// Determines if the media type is an image or video suitable for processing.
    /// </summary>
    private static bool IsImageOrVideo(string? mediaType)
    {
        return mediaType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true
            || mediaType?.StartsWith("video/", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static MessageIdentification? CreateReferencedMessageIdentification(MessageReferenceData? reference)
    {
        return reference != null
            ? new MessageIdentification(
                reference.GuildId,
                reference.ChannelId,
                reference.UserId,
                reference.MessageId)
            : null;
    }

    private static string ExtractCommandFromContent(string? content)
    {
        return string.Join(' ',
            (content ?? string.Empty).Split(" ", StringSplitOptions.RemoveEmptyEntries).Skip(1));
    }

    private bool StartsWithBotMention(string? content, ulong currentMemberId)
    {
        return content?.StartsWith(chatClient.Utils.Mention(currentMemberId, true)) == true
            || content?.StartsWith(chatClient.Utils.Mention(currentMemberId, false)) == true;
    }
}
