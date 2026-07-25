using FastEndpoints;
using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.WebApi.Endpoints;

public class DeleteMessageEndpoint(MessageRouter router) : Endpoint<DeleteMessageRequest>
{
    public override void Configure()
    {
        Delete("/test/messages");
        Tags("Test");
        AllowAnonymous();
    }

    public override async Task HandleAsync(DeleteMessageRequest req, CancellationToken ct)
    {
        var deleted = new DeletedMessage(
            new MessageIdentification(req.GuildId, req.ChannelId, req.UserId, req.MessageId),
            "",
            DateTimeOffset.UtcNow
        );

        await router.RouteDelete(deleted, ct);
        await Send.OkAsync(cancellation: ct);
    }
}
