namespace ShitpostBot.ConversationBackprocessor;

public sealed record DiscordExport(
    DiscordGuild Guild,
    DiscordChannel Channel,
    IReadOnlyList<DiscordExportMessage> Messages
);

public sealed record DiscordGuild(string Id, string Name);

public sealed record DiscordChannel(string Id, string Name);

public sealed record DiscordExportMessage(
    string Id,
    DateTimeOffset Timestamp,
    string? Content,
    DiscordExportAuthor Author
);

public sealed record DiscordExportAuthor(string Id, bool IsBot);
