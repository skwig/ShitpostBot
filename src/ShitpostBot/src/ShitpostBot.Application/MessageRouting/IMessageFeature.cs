using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.MessageRouting;

public interface IMessageFeature
{
    Task<bool> TryHandleCreate(IncomingMessage created, CancellationToken ct) =>
        Task.FromResult(false);
    Task<bool> TryHandleUpdate(
        IncomingMessage old,
        IncomingMessage updated,
        CancellationToken ct
    ) => Task.FromResult(false);
    Task<bool> TryHandleDelete(DeletedMessage deleted, CancellationToken ct) =>
        Task.FromResult(false);
}
