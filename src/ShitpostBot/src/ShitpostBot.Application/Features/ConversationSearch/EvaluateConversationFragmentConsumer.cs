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
        var messages = context
            .Message.Messages.OrderBy(message => message.Timestamp)
            .ThenBy(message => message.MessageId)
            .Select(message => new StagedMessage(
                context.Message.GuildId,
                context.Message.ChannelId,
                message.MessageId,
                message.AuthorId,
                message.Timestamp,
                message.Content
            ))
            .ToList();

        var text = BuildConversation(messages);
        if (string.IsNullOrWhiteSpace(text))
        {
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
            if (response.Error != null)
            {
                throw response.Error;
            }

            throw new HttpRequestException($"ML service returned {response.StatusCode}");
        }

        var first = messages.First();
        var last = messages.Last();

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
            return;
        }

        var fragment = ConversationFragment.Create(
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
