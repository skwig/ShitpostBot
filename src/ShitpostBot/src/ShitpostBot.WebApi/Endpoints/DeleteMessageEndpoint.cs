using FastEndpoints;
using ShitpostBot.Application.Core;
using ShitpostBot.WebApi.Services;

namespace ShitpostBot.WebApi.Endpoints;

public class DeleteMessageEndpoint(
    TestMessageFactory factory,
    IMessageProcessor messageProcessor)
    : Endpoint<DeleteMessageRequest, MessageResponse>
{
    public override void Configure()
    {
        Delete("/test/message/{MessageId}");
        Tags("Test");
    }

    public override async Task HandleAsync(DeleteMessageRequest req, CancellationToken ct)
    {
        var identification = factory.GenerateMessageIdentification(
            req.GuildId,
            req.ChannelId,
            req.UserId,
            req.MessageId
        );

        var currentMemberId = req.CurrentMemberId ?? 0;

        var messageData = new MessageData(
            GuildId: identification.GuildId,
            ChannelId: identification.ChannelId,
            UserId: identification.PosterId,
            MessageId: identification.MessageId,
            CurrentMemberId: currentMemberId,
            Content: null,
            Attachments: [],
            Embeds: [],
            ReferencedMessage: null,
            Timestamp: DateTimeOffset.UtcNow
        );

        await messageProcessor.ProcessDeletedMessageAsync(messageData, ct);

        await SendOkAsync(new MessageResponse
        {
            MessageId = identification.MessageId,
            Tracked = true
        }, ct);
    }
}
