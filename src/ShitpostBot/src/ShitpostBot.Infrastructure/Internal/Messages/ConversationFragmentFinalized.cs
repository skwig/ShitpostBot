namespace ShitpostBot.Infrastructure.Messages;

public record ConversationFragmentFinalized
{
    public ulong GuildId { get; init; }
    public ulong ChannelId { get; init; }
    public required IReadOnlyList<ConversationFragmentMessage> Messages { get; init; }
}

public record ConversationFragmentMessage
{
    public ulong MessageId { get; init; }
    public ulong AuthorId { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public required string Content { get; init; }
}
