using System.Text.Json;
using MassTransit;
using Microsoft.Extensions.Options;
using ShitpostBot.Application.Features.ConversationSearch;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Messages;

namespace ShitpostBot.ConversationBackprocessor;

public sealed class ConversationDumpBackprocessor(
    ILogger<ConversationDumpBackprocessor> logger,
    IOptions<ConversationBackprocessorOptions> options,
    ConversationFragmentStage stage,
    IBus bus
)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task Run(CancellationToken cancellationToken = default)
    {
        var inputPath = Path.GetFullPath(options.Value.InputPath, Environment.CurrentDirectory);
        logger.LogInformation("Loading Discord export from {InputPath}", inputPath);

        await using var stream = File.OpenRead(inputPath);
        var export =
            await JsonSerializer.DeserializeAsync<DiscordExport>(
                stream,
                JsonOptions,
                cancellationToken
            ) ?? throw new InvalidOperationException("Discord export JSON was empty");

        var guildId = ulong.Parse(export.Guild.Id);
        var channelId = ulong.Parse(export.Channel.Id);
        var fragmentGap = TimeSpan.FromMinutes(ConversationSearchOptions.FragmentGapMinutes);
        var messages = export
            .Messages.Where(message =>
                !message.Author.IsBot && !string.IsNullOrWhiteSpace(message.Content)
            )
            .OrderBy(message => message.Timestamp)
            .ThenBy(message => ulong.Parse(message.Id))
            .ToList();

        logger.LogInformation(
            "Backprocessing {MessageCount} conversation messages from {GuildName}/#{ChannelName}",
            messages.Count,
            export.Guild.Name,
            export.Channel.Name
        );

        var publishedFragments = 0;
        foreach (var message in messages)
        {
            var result = stage.Stage(
                new StagedMessage(
                    guildId,
                    channelId,
                    ulong.Parse(message.Id),
                    ulong.Parse(message.Author.Id),
                    message.Timestamp.ToUniversalTime(),
                    message.Content!.Trim()
                ),
                fragmentGap
            );

            if (result.FinalizedFragment is not null)
            {
                await Publish(result.FinalizedFragment, cancellationToken);
                publishedFragments++;
            }
        }

        if (messages.Count > 0)
        {
            var last = messages[^1];
            var flush = stage.Stage(
                new StagedMessage(
                    guildId,
                    channelId,
                    ulong.Parse(last.Id) + 1,
                    0,
                    last.Timestamp.ToUniversalTime() + fragmentGap + TimeSpan.FromSeconds(1),
                    "."
                ),
                fragmentGap
            );

            if (flush.FinalizedFragment is not null)
            {
                await Publish(flush.FinalizedFragment, cancellationToken);
                publishedFragments++;
            }
        }

        logger.LogInformation(
            "Published {FragmentCount} finalized conversation fragments",
            publishedFragments
        );
    }

    private async Task Publish(
        ActiveConversationFragment fragment,
        CancellationToken cancellationToken
    )
    {
        var messages = fragment
            .Messages.Where(message => !string.IsNullOrWhiteSpace(message.Content))
            .ToList();
        if (messages.Count < ConversationSearchOptions.MinFragmentMessageCount)
        {
            return;
        }

        await bus.Publish(
            new ConversationFragmentFinalized
            {
                GuildId = fragment.GuildId,
                ChannelId = fragment.ChannelId,
                Messages = messages
                    .Select(message => new ConversationFragmentMessage
                    {
                        MessageId = message.MessageId,
                        AuthorId = message.AuthorId,
                        Timestamp = message.Timestamp,
                        Content = message.Content,
                    })
                    .ToList(),
            },
            cancellationToken
        );
    }
}
