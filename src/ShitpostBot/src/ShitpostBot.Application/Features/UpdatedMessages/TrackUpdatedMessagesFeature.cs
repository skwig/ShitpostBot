using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.UpdatedMessages;

public class TrackUpdatedMessagesFeature(
    UpdatedMessageStore store,
    IDateTimeProvider dateTimeProvider
) : IMessageFeature
{
    public Task<bool> TryHandleUpdate(
        IncomingMessage old,
        IncomingMessage updated,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(old.Content) || string.IsNullOrWhiteSpace(updated.Content))
        {
            return Task.FromResult(false);
        }

        store.Store(
            new UpdatedMessage(
                updated.Id,
                old.Content,
                updated.Content,
                updated.PostedOn,
                dateTimeProvider.UtcNow
            )
        );

        return Task.FromResult(false);
    }
}
