using FastEndpoints;
using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.WebApi.Endpoints;

public class UpdateMessageEndpoint(MessageRouter router)
    : Endpoint<UpdateMessageRequest>
{
    public override void Configure()
    {
        Put("/test/messages/{MessageId}");
        Tags("Test");
        AllowAnonymous();
    }

    public override async Task HandleAsync(UpdateMessageRequest req, CancellationToken ct)
    {
        var id = new MessageIdentification(req.GuildId, req.ChannelId, req.UserId, req.MessageId);

        var old = new IncomingMessage(
            id,
            null,
            null,
            [],
            [],
            DateTimeOffset.UtcNow
        );

        var updated = new IncomingMessage(
            id,
            null,
            req.Content,
            req.Attachments?.Where(a => a.Url != null).Select(a => new Attachment(a.Id, new Uri(a.Url!), a.MediaType)).ToList() ?? [],
            req.Embeds?.Where(e => e.Url != null).Select(e => new Embed(new Uri(e.Url!))).ToList() ?? [],
            DateTimeOffset.UtcNow
        );

        await router.RouteUpdate(old, updated, ct);

        await Send.OkAsync(ct);
    }
}