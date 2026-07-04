using FastEndpoints;
using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.WebApi.Endpoints;

public class DeleteMessageEndpoint(MessageRouter router)
    : Endpoint<DeleteMessageRequest>
{
    public override void Configure()
    {
        Delete("/test/messages/{MessageId}");
        Tags("Test");
        AllowAnonymous();
    }

    public override async Task HandleAsync(DeleteMessageRequest req, CancellationToken ct)
    {
        var id = new MessageIdentification(
            req.GuildId ?? 0,
            req.ChannelId ?? 0,
            req.UserId ?? 0,
            req.MessageId
        );

        await router.RouteDelete(id);
        await Send.OkAsync(ct);
    }
}