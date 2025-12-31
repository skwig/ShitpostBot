using DSharpPlus.EventArgs;
using ShitpostBot.Infrastructure.Extensions;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Core;

public class ChatMessageCreatedListener(IMessageProcessor messageProcessor)
    : IChatMessageCreatedListener
{
    public async Task HandleMessageCreatedAsync(MessageCreateEventArgs message)
    {
        var cancellationToken = CancellationToken.None;

        var isPosterBot = message.Author.IsBot;
        if (isPosterBot)
        {
            return;
        }

        var messageData = MapToMessageData(message);
        await messageProcessor.ProcessCreatedMessageAsync(messageData, cancellationToken);
    }

    private static MessageData MapToMessageData(MessageCreateEventArgs message)
    {
        var attachments = message.Message.Attachments
            .Select(a => new MessageAttachmentData(
                a.Id,
                a.FileName,
                a.GetAttachmentUri(),
                a.MediaType,
                a.Width,
                a.Height
            ))
            .ToList();

        var embeds = message.Message.Embeds
            .Select(e => new MessageEmbedData(e.Url))
            .ToList();

        var referencedMessage = message.Message.Reference != null
            ? new MessageReferenceData(
                message.Message.Reference.Guild.Id,
                message.Message.Reference.Channel.Id,
                message.Message.Reference.Message.Author.Id,
                message.Message.Reference.Message.Id
            )
            : null;

        return new MessageData(
            message.Guild.Id,
            message.Channel.Id,
            message.Author.Id,
            message.Message.Id,
            message.Guild.CurrentMember.Id,
            message.Message.Content,
            attachments,
            embeds,
            referencedMessage,
            message.Message.CreationTimestamp
        );
    }
}