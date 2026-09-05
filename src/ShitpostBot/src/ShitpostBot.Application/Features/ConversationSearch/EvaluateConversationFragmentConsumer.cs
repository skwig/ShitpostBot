using System.Diagnostics;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Messages;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.ConversationSearch;

public sealed class EvaluateConversationFragmentConsumer(
    IImageFeatureExtractorApi mlService,
    IDbContext dbContext,
    IUnitOfWork unitOfWork
) : IConsumer<ConversationFragmentFinalized>
{
    public async Task Consume(ConsumeContext<ConversationFragmentFinalized> context)
    {
        using var activity = ShitpostBotActivitySource.Instance.StartActivity(
            nameof(EvaluateConversationFragmentConsumer),
            ActivityKind.Consumer
        );
        Activity.Current?.SetTag(Tags.Messaging.System, "masstransit");
        Activity.Current?.SetTag(Tags.Discord.Guild.Id, context.Message.GuildId);
        Activity.Current?.SetTag(Tags.Discord.Channel.Id, context.Message.ChannelId);

        var messages = context
            .Message.Messages.OrderBy(message => message.Timestamp)
            .ThenBy(message => message.MessageId)
            .Where(message => !string.IsNullOrWhiteSpace(message.Content))
            .Select(message => new StagedMessage(
                context.Message.GuildId,
                context.Message.ChannelId,
                message.MessageId,
                message.AuthorId,
                message.Timestamp,
                message.Content
            ))
            .ToList();

        Activity.Current?.SetTag(
            Tags.ShitpostBot.ConversationFragment.MessageCount,
            messages.Count
        );

        if (
            messages.Count < ConversationSearchOptions.MinFragmentMessageCount
            || messages.Count > ConversationSearchOptions.MaxFragmentMessageCount
        )
        {
            Activity.Current?.SetTag(
                Tags.ShitpostBot.ConversationFragment.Outcome,
                "skipped_message_count"
            );
            return;
        }

        var first = messages.First();
        var last = messages.Last();
        Activity.Current?.SetTag(Tags.Discord.Message.Id, first.MessageId);
        Activity.Current?.SetTag(Tags.Discord.User.Id, first.AuthorId);
        Activity.Current?.SetTag(
            Tags.ShitpostBot.ConversationFragment.FirstMessageId,
            first.MessageId
        );
        Activity.Current?.SetTag(
            Tags.ShitpostBot.ConversationFragment.LastMessageId,
            last.MessageId
        );

        var text = BuildConversation(messages);
        if (string.IsNullOrWhiteSpace(text))
        {
            Activity.Current?.SetTag(
                Tags.ShitpostBot.ConversationFragment.Outcome,
                "skipped_empty_text"
            );
            return;
        }

        var response = await mlService.EmbedConversationTextAsync(
            new ConversationTextEmbedRequest
            {
                Text = text,
                Mode = ConversationTextEmbedMode.Passage,
            }
        );

        if (!response.IsSuccessfulWithContent)
        {
            Activity.Current?.SetTag(Tags.ShitpostBot.ConversationFragment.Outcome, "ml_failed");
            if (response.Error != null)
            {
                throw response.Error;
            }

            throw new HttpRequestException($"ML service returned {response.StatusCode}");
        }

        Activity.Current?.SetTag(
            Tags.ShitpostBot.ConversationFragment.TokenCount,
            response.Content.TokenCount
        );
        Activity.Current?.SetTag(
            Tags.ShitpostBot.ConversationFragment.MaxTokenCount,
            response.Content.MaxTokenCount
        );
        Activity.Current?.SetTag(
            Tags.ShitpostBot.ConversationFragment.Truncated,
            response.Content.Truncated
        );

        if (
            await dbContext.ConversationFragment.AnyAsync(
                fragment =>
                    fragment.GuildId == context.Message.GuildId
                    && fragment.ChannelId == context.Message.ChannelId
                    && fragment.FirstMessageId == first.MessageId,
                context.CancellationToken
            )
        )
        {
            Activity.Current?.SetTag(
                Tags.ShitpostBot.ConversationFragment.Outcome,
                "skipped_duplicate"
            );
            return;
        }

        var fragment = ShitpostBot.Domain.ConversationFragment.Create(
            context.Message.GuildId,
            context.Message.ChannelId,
            first.MessageId,
            last.MessageId,
            first.Timestamp,
            last.Timestamp,
            messages.Count,
            new Vector(response.Content.Embedding)
        );

        dbContext.ConversationFragment.Add(fragment);
        await unitOfWork.SaveChangesAsync(context.CancellationToken);
        Activity.Current?.SetTag(Tags.ShitpostBot.ConversationFragment.Outcome, "persisted");
    }

    public static string BuildConversation(IEnumerable<StagedMessage> messages)
    {
        return string.Join(
            '\n',
            messages
                .Where(message => !string.IsNullOrWhiteSpace(message.Content))
                .Select(message => message.Content.Trim())
        );
    }
}

public sealed record StagedMessage(
    ulong GuildId,
    ulong ChannelId,
    ulong MessageId,
    ulong AuthorId,
    DateTimeOffset Timestamp,
    string Content
);
