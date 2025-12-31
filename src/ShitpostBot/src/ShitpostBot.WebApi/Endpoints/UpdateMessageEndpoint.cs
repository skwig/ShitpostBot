using FastEndpoints;
using ShitpostBot.Application.Core;
using ShitpostBot.WebApi.Services;

namespace ShitpostBot.WebApi.Endpoints;

public class UpdateMessageEndpoint(
    TestMessageFactory factory,
    IMessageProcessor messageProcessor)
    : Endpoint<UpdateMessageRequest, MessageResponse>
{
    public override void Configure()
    {
        Put("/test/message/{MessageId}");
        Tags("Test");
    }

    public override async Task HandleAsync(UpdateMessageRequest req, CancellationToken ct)
    {
        var identification = factory.GenerateMessageIdentification(
            req.GuildId,
            req.ChannelId,
            req.UserId,
            req.MessageId
        );

        var currentMemberId = req.CurrentMemberId ?? 0;
        var timestamp = req.Timestamp ?? DateTimeOffset.UtcNow;

        var attachments = req.Attachments.Select(a => new MessageAttachmentData(
            Id: factory.GenerateAttachmentId(),
            FileName: a.FileName ?? "attachment",
            Url: new Uri(a.Url),
            MediaType: a.MediaType,
            Width: a.Width,
            Height: a.Height
        )).ToList();

        var embeds = req.Embeds
            .Where(e => e.Url != null)
            .Select(e => new MessageEmbedData(new Uri(e.Url!)))
            .ToList();

        MessageReferenceData? referencedMessage = null;
        if (req.ReferencedMessage != null)
        {
            referencedMessage = new MessageReferenceData(
                GuildId: req.ReferencedMessage.GuildId ?? identification.GuildId,
                ChannelId: req.ReferencedMessage.ChannelId ?? identification.ChannelId,
                UserId: req.ReferencedMessage.UserId ?? identification.PosterId,
                MessageId: req.ReferencedMessage.MessageId
            );
        }

        var messageData = new MessageData(
            GuildId: identification.GuildId,
            ChannelId: identification.ChannelId,
            UserId: identification.PosterId,
            MessageId: identification.MessageId,
            CurrentMemberId: currentMemberId,
            Content: req.Content,
            Attachments: attachments,
            Embeds: embeds,
            ReferencedMessage: referencedMessage,
            Timestamp: timestamp
        );

        await messageProcessor.ProcessUpdatedMessageAsync(messageData, ct);

        await SendOkAsync(new MessageResponse
        {
            MessageId = identification.MessageId,
            Tracked = true
        }, ct);
    }
}
