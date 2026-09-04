namespace ShitpostBot.Backprocessor;

public class BackprocessorState
{
    public IReadOnlyList<BackprocessorChannelState> Channels { get; init; } = [];
}

public class BackprocessorChannelState
{
    public ulong GuildId { get; init; }
    public ulong ChannelId { get; init; }
    public required string Name { get; init; }
    public ulong? LastCompletedMessageId { get; init; }
    public DateTimeOffset? LastCompletedTimestamp { get; init; }
    public long ProcessedMessages { get; init; }
    public long InsertedImages { get; init; }
    public long SkippedMessages { get; init; }
    public long FailedMessages { get; init; }
}
