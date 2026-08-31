namespace ShitpostBot.Backprocessor;

public interface IBackprocessorStateStore
{
    Task<BackprocessorState> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(BackprocessorState state, CancellationToken cancellationToken = default);
}
