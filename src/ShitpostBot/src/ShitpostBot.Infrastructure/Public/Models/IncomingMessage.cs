namespace ShitpostBot.Infrastructure;

public record Attachment(
    ulong Id,
    Uri Url,
    string? MediaType,
    int? Width = null,
    int? Height = null
);

public record Embed(Uri Url);

public record IncomingMessage(
    MessageIdentification Id,
    MessageIdentification? RepliedToId,
    string? Content,
    IReadOnlyList<Attachment> Attachments,
    IReadOnlyList<Embed> Embeds,
    DateTimeOffset PostedOn
);
