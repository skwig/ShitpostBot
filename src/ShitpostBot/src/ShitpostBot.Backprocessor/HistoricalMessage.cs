namespace ShitpostBot.Backprocessor;

public record HistoricalMessage(
    ulong GuildId,
    ulong ChannelId,
    ulong MessageId,
    ulong AuthorId,
    bool IsBot,
    DateTimeOffset PostedOn,
    string? Content,
    IReadOnlyList<HistoricalAttachment> Attachments
);

public record HistoricalAttachment(ulong Id, Uri Url, string? MediaType, int? Width, int? Height);
