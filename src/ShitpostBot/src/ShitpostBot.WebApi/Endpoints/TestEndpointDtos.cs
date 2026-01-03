using ShitpostBot.WebApi.Services;

namespace ShitpostBot.WebApi.Endpoints;

public record PostImageMessageRequest
{
    public required string ImageUrl { get; init; }
    public ulong? GuildId { get; init; }
    public ulong? ChannelId { get; init; }
    public ulong? UserId { get; init; }
    public ulong? MessageId { get; init; }
    public DateTimeOffset? Timestamp { get; init; }
}

public record PostLinkMessageRequest
{
    public required string LinkUrl { get; init; }
    public ulong? GuildId { get; init; }
    public ulong? ChannelId { get; init; }
    public ulong? UserId { get; init; }
    public ulong? MessageId { get; init; }
    public DateTimeOffset? Timestamp { get; init; }
}

public record PostBotCommandRequest
{
    public required string Command { get; init; }
    public ulong? GuildId { get; init; }
    public ulong? ChannelId { get; init; }
    public ulong? UserId { get; init; }
    public ulong? MessageId { get; init; }
    public ulong? ReferencedMessageId { get; init; }
    public ulong? ReferencedUserId { get; init; }
}

public record PostMessageResponse
{
    public required ulong MessageId { get; init; }
    public required bool Tracked { get; init; }
}

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