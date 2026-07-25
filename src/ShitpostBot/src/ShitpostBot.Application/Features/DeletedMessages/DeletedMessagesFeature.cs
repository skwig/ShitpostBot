using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.Features.DeletedMessages;

public class DeletedMessagesFeature : IMessageFeature
{
    public Task<bool> TryHandleDelete(MessageIdentification deleted, CancellationToken ct)
    {
        return Task.FromResult(false);
    }
}
