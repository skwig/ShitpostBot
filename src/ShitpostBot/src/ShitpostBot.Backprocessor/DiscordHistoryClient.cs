using DSharpPlus;

namespace ShitpostBot.Backprocessor;

public class DiscordHistoryClient(DiscordClient discordClient) : IDiscordHistoryClient
{
    public async Task<IReadOnlyList<HistoricalMessage>> GetMessagesBeforeAsync(
        BackprocessorChannelOptions channelOptions,
        ulong? beforeMessageId,
        int pageSize,
        CancellationToken cancellationToken = default
    )
    {
        var guild = await discordClient.GetGuildAsync(channelOptions.GuildId);
        var channel =
            guild.Channels.GetValueOrDefault(channelOptions.ChannelId)
            ?? guild.Threads.GetValueOrDefault(channelOptions.ChannelId);
        if (channel == null)
        {
            throw new InvalidOperationException(
                $"Channel {channelOptions.ChannelId} not found in guild {channelOptions.GuildId}"
            );
        }

        var messages = beforeMessageId.HasValue
            ? await channel.GetMessagesBeforeAsync(beforeMessageId.Value, pageSize)
            : await channel.GetMessagesAsync(pageSize);

        cancellationToken.ThrowIfCancellationRequested();

        return messages
            .OrderByDescending(message => message.Id)
            .Select(message => new HistoricalMessage(
                channelOptions.GuildId,
                channelOptions.ChannelId,
                message.Id,
                message.Author.Id,
                message.Author.IsBot,
                message.CreationTimestamp,
                message.Content,
                message
                    .Attachments.Select(attachment => new HistoricalAttachment(
                        attachment.Id,
                        new Uri(attachment.Url),
                        attachment.MediaType,
                        attachment.Width,
                        attachment.Height
                    ))
                    .ToList()
            ))
            .ToList();
    }
}
