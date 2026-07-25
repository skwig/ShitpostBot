using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.Features.DeletedMessages;

public class TrackDeletedMessagesFeature(DeletedMessageStore store) : IMessageFeature
{
    public Task<bool> TryHandleDelete(MessageIdentification deleted, CancellationToken ct)
    {
        store.Store(
            deleted.ChannelId,
            new DeletedMessage(
                deleted.PosterId,
                "",
                "",
                DateTimeOffset.UtcNow
            )
        );

        return Task.FromResult(false);
    }
}
