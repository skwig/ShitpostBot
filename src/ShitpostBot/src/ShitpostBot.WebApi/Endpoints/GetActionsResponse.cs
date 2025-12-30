using ShitpostBot.WebApi.Services;

namespace ShitpostBot.WebApi.Endpoints;

public record GetActionsResponse
{
    public required ulong MessageId { get; init; }
    public required IReadOnlyList<TestAction> Actions { get; init; }
    public required long WaitedMs { get; init; }
}
