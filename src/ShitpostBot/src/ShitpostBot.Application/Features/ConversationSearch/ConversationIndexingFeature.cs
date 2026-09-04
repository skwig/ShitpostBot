using MassTransit;
using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Messages;

namespace ShitpostBot.Application.Features.ConversationSearch;

public sealed class ConversationIndexingFeature(ConversationFragmentStage stage, IBus bus)
    : IMessageFeature
{
    public async Task<bool> TryHandleCreate(IncomingMessage created, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(created.Content))
        {
            return false;
        }

        var staged = new StagedMessage(
            created.Id.GuildId,
            created.Id.ChannelId,
            created.Id.MessageId,
            created.Id.PosterId,
            created.PostedOn,
            created.Content
        );

        var result = stage.Stage(
            staged,
            TimeSpan.FromMinutes(ConversationSearchOptions.FragmentGapMinutes)
        );

        if (result.FinalizedFragment is not null)
        {
            await bus.Publish(ToMessage(result.FinalizedFragment), cancellationToken: ct);
        }

        return false;
    }

    private static ConversationFragmentFinalized ToMessage(ActiveConversationFragment fragment)
    {
        return new ConversationFragmentFinalized
        {
            GuildId = fragment.GuildId,
            ChannelId = fragment.ChannelId,
            Messages = fragment
                .Messages.Select(message => new ConversationFragmentMessage
                {
                    MessageId = message.MessageId,
                    AuthorId = message.AuthorId,
                    Timestamp = message.Timestamp,
                    Content = message.Content,
                })
                .ToList(),
        };
    }
}
