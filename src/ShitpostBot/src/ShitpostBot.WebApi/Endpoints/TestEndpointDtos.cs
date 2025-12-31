using ShitpostBot.WebApi.Services;

namespace ShitpostBot.WebApi.Endpoints;

public class GetActionsRequest
{
    public ulong MessageId { get; set; }
    public int ExpectedCount { get; set; } = 0;
    public int Timeout { get; set; } = 10000;
}

public record GetActionsResponse
{
    public required ulong MessageId { get; init; }
    public required IReadOnlyList<TestAction> Actions { get; init; }
    public required long WaitedMs { get; init; }
}