using FastEndpoints;
using ShitpostBot.Application.MessageRouting;

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
        await Send.OkAsync(ct);
    }
}
