using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ShitpostBot.Backprocessor;

public class BackprocessorRunner(
    ILogger<BackprocessorRunner> logger,
    IOptions<BackprocessorOptions> options,
    IBackprocessorStateStore stateStore,
    IDiscordHistoryClient historyClient,
    ImageBackfillService imageBackfillService
)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var state = await stateStore.LoadAsync(cancellationToken);

        foreach (var channel in options.Value.Channels)
        {
            logger.LogInformation(
                "Starting backprocess for {ChannelName} ({ChannelId})",
                channel.Name,
                channel.ChannelId
            );

            state = EnsureChannelState(state, channel);
            var channelState = GetChannelState(state, channel);
            var beforeMessageId =
                channelState.LastCompletedMessageId ?? channel.StartBeforeMessageId;

            while (!cancellationToken.IsCancellationRequested)
            {
                var page = await historyClient.GetMessagesBeforeAsync(
                    channel,
                    beforeMessageId,
                    options.Value.PageSize,
                    cancellationToken
                );
                if (page.Count == 0)
                {
                    break;
                }

                foreach (var message in page.OrderByDescending(m => m.MessageId))
                {
                    if (message.MessageId <= channel.OldestMessageId)
                    {
                        logger.LogInformation(
                            "Reached oldest boundary {OldestMessageId} for {ChannelName}",
                            channel.OldestMessageId,
                            channel.Name
                        );
                        return;
                    }

                    var result = await imageBackfillService.ProcessMessageAsync(
                        message,
                        cancellationToken
                    );

                    state = UpdateChannelState(state, channel, message, result);
                    await stateStore.SaveAsync(state, cancellationToken);

                    beforeMessageId = message.MessageId;

                    if (options.Value.MessageDelay > TimeSpan.Zero)
                    {
                        await Task.Delay(options.Value.MessageDelay, cancellationToken);
                    }
                }

                if (options.Value.PageDelay > TimeSpan.Zero)
                {
                    await Task.Delay(options.Value.PageDelay, cancellationToken);
                }
            }
        }
    }

    private static BackprocessorState EnsureChannelState(
        BackprocessorState state,
        BackprocessorChannelOptions channel
    )
    {
        if (
            state.Channels.Any(s =>
                s.GuildId == channel.GuildId && s.ChannelId == channel.ChannelId
            )
        )
        {
            return state;
        }

        return new BackprocessorState
        {
            Channels = state
                .Channels.Append(
                    new BackprocessorChannelState
                    {
                        GuildId = channel.GuildId,
                        ChannelId = channel.ChannelId,
                        Name = channel.Name,
                    }
                )
                .ToList(),
        };
    }

    private static BackprocessorChannelState GetChannelState(
        BackprocessorState state,
        BackprocessorChannelOptions channel
    ) =>
        state.Channels.Single(s =>
            s.GuildId == channel.GuildId && s.ChannelId == channel.ChannelId
        );

    private static BackprocessorState UpdateChannelState(
        BackprocessorState state,
        BackprocessorChannelOptions channel,
        HistoricalMessage message,
        ImageBackfillResult result
    )
    {
        return new BackprocessorState
        {
            Channels = state
                .Channels.Select(existing =>
                {
                    if (
                        existing.GuildId != channel.GuildId
                        || existing.ChannelId != channel.ChannelId
                    )
                    {
                        return existing;
                    }

                    return new BackprocessorChannelState
                    {
                        GuildId = existing.GuildId,
                        ChannelId = existing.ChannelId,
                        Name = channel.Name,
                        LastCompletedMessageId = message.MessageId,
                        LastCompletedTimestamp = message.PostedOn,
                        ProcessedMessages = existing.ProcessedMessages + 1,
                        InsertedImages = existing.InsertedImages + result.InsertedImages,
                        SkippedMessages = existing.SkippedMessages + (result.Skipped ? 1 : 0),
                        FailedMessages = existing.FailedMessages,
                    };
                })
                .ToList(),
        };
    }
}
