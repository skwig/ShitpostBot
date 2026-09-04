namespace ShitpostBot.Backprocessor;

public interface IDiscordHistoryClient
{
    Task<IReadOnlyList<HistoricalMessage>> GetMessagesBeforeAsync(
        BackprocessorChannelOptions channelOptions,
        ulong? beforeMessageId,
        int pageSize,
        CancellationToken cancellationToken = default
    );
}
