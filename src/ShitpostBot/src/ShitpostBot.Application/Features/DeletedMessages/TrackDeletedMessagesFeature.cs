using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.Features.DeletedMessages;

public class TrackDeletedMessagesFeature(DeletedMessageStore store) : IMessageFeature
{
    public Task<bool> TryHandleDelete(DeletedMessage deleted, CancellationToken ct)
    {
        store.Store(deleted);
        return Task.FromResult(false);
    }
}
