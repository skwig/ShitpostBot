namespace ShitpostBot.Application.Features.ConversationSearch;

public sealed record ActiveConversationFragment
{
    public required ulong GuildId { get; init; }
    public required ulong ChannelId { get; init; }
    public required List<StagedMessage> Messages { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public required DateTimeOffset LastMessageAt { get; set; }
}
