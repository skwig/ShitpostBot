namespace ShitpostBot.Infrastructure;

public record MessageDestination(ulong GuildId, ulong ChannelId, ulong? ReplyToMessageId = null);

public record MessageIdentification(ulong GuildId, ulong ChannelId, ulong PosterId, ulong MessageId);