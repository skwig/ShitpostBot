using FastEndpoints;
using ShitpostBot.Application.Core;
using ShitpostBot.WebApi.Services;

namespace ShitpostBot.WebApi.Endpoints;

public class PostMessageEndpoint(
    TestMessageFactory factory,
    IMessageProcessor messageProcessor)
    : Endpoint<PostMessageRequest, MessageResponse>
{
    public override void Configure()
    {
        Post("/test/message");
        Tags("Test");
    }

    public override async Task HandleAsync(PostMessageRequest req, CancellationToken ct)
    {
        var identification = factory.GenerateMessageIdentification(
            req.GuildId,
            req.ChannelId,
            req.UserId,
            req.MessageId
        );

        var currentMemberId = req.CurrentMemberId ?? 0;
        var timestamp = req.Timestamp ?? DateTimeOffset.UtcNow;

        var attachments = req.Attachments.Select(a =>
        {
            var url = new Uri(a.Url);
            var fileName = a.FileName ?? Path.GetFileName(url.LocalPath) ?? "attachment";
            var mediaType = a.MediaType ?? InferMediaTypeFromFileName(fileName);

            return new MessageAttachmentData(
                Id: factory.GenerateAttachmentId(),
                FileName: fileName,
                Url: url,
                MediaType: mediaType,
                Width: a.Width,
                Height: a.Height
            );
        }).ToList();

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

        await messageProcessor.ProcessCreatedMessageAsync(messageData, ct);

        await SendOkAsync(new MessageResponse
        {
            MessageId = identification.MessageId,
            Tracked = true
        }, ct);
    }

    private static string? InferMediaTypeFromFileName(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".mov" => "video/quicktime",
            _ => null
        };
    }
}
