using FastEndpoints;
using ShitpostBot.WebApi.Services;
using System.Diagnostics;

namespace ShitpostBot.WebApi.Endpoints;

public class GetActionsEndpoint : Endpoint<GetActionsRequest, GetActionsResponse>
{
    public override void Configure()
    {
        Get("/test/actions/{MessageId}");
        Tags("Test");
    }

    public override async Task HandleAsync(GetActionsRequest request, CancellationToken ct)
    {
        var store = Resolve<IBotActionStore>();
        var stopwatch = Stopwatch.StartNew();

        var actions = await store.WaitForActionsAsync(
            request.MessageId,
            request.ExpectedCount,
            TimeSpan.FromMilliseconds(request.Timeout)
        );

        await SendOkAsync(new GetActionsResponse
        {
            MessageId = request.MessageId,
            Actions = actions,
            WaitedMs = stopwatch.ElapsedMilliseconds
        }, ct);
    }
}