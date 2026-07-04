using ShitpostBot.WebApi.Services;

namespace ShitpostBot.WebApi.Endpoints;

public record PostMessageRequest
{
    public required ulong GuildId { get; init; }
    public required ulong ChannelId { get; init; }
    public required ulong UserId { get; init; }
    public required ulong MessageId { get; init; }
    public string? Content { get; init; }
    public ulong? RepliedToMessageId { get; init; }
    public ulong? RepliedToUserId { get; init; }
    public DateTimeOffset? Timestamp { get; init; }
    public IReadOnlyList<AttachmentDto>? Attachments { get; init; }
    public IReadOnlyList<EmbedDto>? Embeds { get; init; }
}

public record AttachmentDto
{
    public ulong Id { get; init; }
    public string? Url { get; init; }
    public string? MediaType { get; init; }
    public int? Width { get; init; }
    public int? Height { get; init; }
}

public record EmbedDto
{
    public string? Url { get; init; }
}

public record UpdateMessageRequest
{
    public required ulong GuildId { get; init; }
    public required ulong ChannelId { get; init; }
    public required ulong UserId { get; init; }
    public required ulong MessageId { get; init; }
    public string? Content { get; init; }
    public IReadOnlyList<AttachmentDto>? Attachments { get; init; }
    public IReadOnlyList<EmbedDto>? Embeds { get; init; }
}

public record DeleteMessageRequest
{
    public required ulong GuildId { get; init; }
    public required ulong ChannelId { get; init; }
    public required ulong UserId { get; init; }
    public required ulong MessageId { get; init; }
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