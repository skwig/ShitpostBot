namespace ShitpostBot.WebApi.Endpoints;

public record PostMessageRequest
{
    public string? Content { get; init; }
    public List<MessageAttachmentRequest> Attachments { get; init; } = [];
    public List<MessageEmbedRequest> Embeds { get; init; } = [];
    public MessageReferenceRequest? ReferencedMessage { get; init; }
    public ulong? GuildId { get; init; }
    public ulong? ChannelId { get; init; }
    public ulong? UserId { get; init; }
    public ulong? MessageId { get; init; }
    public ulong? CurrentMemberId { get; init; }
    public DateTimeOffset? Timestamp { get; init; }
}

public record MessageAttachmentRequest
{
    public required string Url { get; init; }
    public string? FileName { get; init; }
    public string? MediaType { get; init; }
    public int? Width { get; init; }
    public int? Height { get; init; }
}

public record MessageEmbedRequest
{
    public string? Url { get; init; }
}

public record MessageReferenceRequest
{
    public ulong? GuildId { get; init; }
    public ulong? ChannelId { get; init; }
    public ulong? UserId { get; init; }
    public required ulong MessageId { get; init; }
}

public record UpdateMessageRequest
{
    public required ulong MessageId { get; init; }
    public string? Content { get; init; }
    public List<MessageAttachmentRequest> Attachments { get; init; } = [];
    public List<MessageEmbedRequest> Embeds { get; init; } = [];
    public MessageReferenceRequest? ReferencedMessage { get; init; }
    public ulong? GuildId { get; init; }
    public ulong? ChannelId { get; init; }
    public ulong? UserId { get; init; }
    public ulong? CurrentMemberId { get; init; }
    public DateTimeOffset? Timestamp { get; init; }
}

public record DeleteMessageRequest
{
    public required ulong MessageId { get; init; }
    public ulong? GuildId { get; init; }
    public ulong? ChannelId { get; init; }
    public ulong? UserId { get; init; }
    public ulong? CurrentMemberId { get; init; }
}

public record MessageResponse
{
    public required ulong MessageId { get; init; }
    public required bool Tracked { get; init; }
}
