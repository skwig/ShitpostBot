namespace ShitpostBot.Application.Core;

public record MessageData(
    ulong GuildId,
    ulong ChannelId,
    ulong UserId,
    ulong MessageId,
    ulong CurrentMemberId,
    string? Content,
    IReadOnlyList<MessageAttachmentData> Attachments,
    IReadOnlyList<MessageEmbedData> Embeds,
    MessageReferenceData? ReferencedMessage,
    DateTimeOffset Timestamp
);

public record MessageAttachmentData(
    ulong Id,
    string FileName,
    Uri Url,
    string? MediaType,
    int? Width,
    int? Height
);

public record MessageEmbedData(
    Uri? Url
);

public record MessageReferenceData(
    ulong GuildId,
    ulong ChannelId,
    ulong UserId,
    ulong MessageId
);
