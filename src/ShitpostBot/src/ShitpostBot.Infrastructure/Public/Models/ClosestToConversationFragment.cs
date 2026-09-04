using ShitpostBot.Domain;

namespace ShitpostBot.Infrastructure;

public record ClosestToConversationFragment(
    long ConversationFragmentId,
    ulong GuildId,
    ulong ChannelId,
    ulong FirstMessageId,
    ulong LastMessageId,
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt,
    int MessageCount,
    double CosineDistance
)
{
    public ChatMessageIdentifier FirstMessageIdentifier => new(GuildId, ChannelId, FirstMessageId);
    public double CosineSimilarity => Math.Round(1 - CosineDistance, 8);
}
