using CSharpFunctionalExtensions;
using Pgvector;

namespace ShitpostBot.Domain;

public sealed class ConversationFragment : Entity<long>
{
    public ulong GuildId { get; private set; }
    public ulong ChannelId { get; private set; }
    public ulong FirstMessageId { get; private set; }
    public ulong LastMessageId { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset EndedAt { get; private set; }
    public int MessageCount { get; private set; }
    public Vector Embedding { get; private set; }

    private ConversationFragment()
    {
        // For EF
        Embedding = null!;
    }

    private ConversationFragment(
        ulong guildId,
        ulong channelId,
        ulong firstMessageId,
        ulong lastMessageId,
        DateTimeOffset startedAt,
        DateTimeOffset endedAt,
        int messageCount,
        Vector embedding
    )
    {
        GuildId = guildId;
        ChannelId = channelId;
        FirstMessageId = firstMessageId;
        LastMessageId = lastMessageId;
        StartedAt = startedAt;
        EndedAt = endedAt;
        MessageCount = messageCount;
        Embedding = embedding;
    }

    public static ConversationFragment Create(
        ulong guildId,
        ulong channelId,
        ulong firstMessageId,
        ulong lastMessageId,
        DateTimeOffset startedAt,
        DateTimeOffset endedAt,
        int messageCount,
        Vector embedding
    )
    {
        if (messageCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(messageCount));
        }

        return new ConversationFragment(
            guildId,
            channelId,
            firstMessageId,
            lastMessageId,
            startedAt,
            endedAt,
            messageCount,
            embedding
        );
    }
}
