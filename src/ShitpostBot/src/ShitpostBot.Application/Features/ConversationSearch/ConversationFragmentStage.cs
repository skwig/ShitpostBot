using System.Collections.Concurrent;

namespace ShitpostBot.Application.Features.ConversationSearch;

public sealed record ConversationFragmentStagingResult(
    ActiveConversationFragment? FinalizedFragment
);

public sealed class ConversationFragmentStage
{
    private readonly ConcurrentDictionary<ulong, ChannelFragmentState> channels = new();

    public ConversationFragmentStagingResult Stage(StagedMessage message, TimeSpan fragmentGap)
    {
        var channel = channels.GetOrAdd(message.ChannelId, _ => new ChannelFragmentState());

        lock (channel.SyncRoot)
        {
            if (channel.Fragment is null)
            {
                channel.Fragment = CreateFragment(message);
                return new ConversationFragmentStagingResult(null);
            }

            var gap = message.Timestamp - channel.Fragment.LastMessageAt;
            if (gap <= fragmentGap)
            {
                channel.Fragment.Messages.Add(message);
                channel.Fragment.LastMessageAt = message.Timestamp;
                return new ConversationFragmentStagingResult(null);
            }

            var finalized = new ActiveConversationFragment
            {
                GuildId = channel.Fragment.GuildId,
                ChannelId = channel.Fragment.ChannelId,
                StartedAt = channel.Fragment.StartedAt,
                LastMessageAt = channel.Fragment.LastMessageAt,
                Messages = [.. channel.Fragment.Messages],
            };

            channel.Fragment = CreateFragment(message);
            return new ConversationFragmentStagingResult(finalized);
        }
    }

    private static ActiveConversationFragment CreateFragment(StagedMessage message)
    {
        return new ActiveConversationFragment
        {
            GuildId = message.GuildId,
            ChannelId = message.ChannelId,
            StartedAt = message.Timestamp,
            LastMessageAt = message.Timestamp,
            Messages = [message],
        };
    }

    private sealed class ChannelFragmentState
    {
        public object SyncRoot { get; } = new();
        public ActiveConversationFragment? Fragment { get; set; }
    }
}
