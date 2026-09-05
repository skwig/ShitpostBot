using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using ShitpostBot.Application.Extensions;
using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Extensions;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.ConversationSearch;

public sealed class ConversationSearchCommand(
    IDbContext dbContext,
    IChatClient chatClient,
    IImageFeatureExtractorApi mlService
) : BotCommandFeature(chatClient)
{
    public override string? HelpMessage => "`csearch <query>` - search conversation fragments";

    protected override async Task<bool> TryHandleCommand(
        MessageIdentification commandMessageIdentification,
        string command,
        MessageIdentification? referenced,
        CancellationToken ct
    )
    {
        if (!command.StartsWith("csearch", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (command.Length > 7 && !char.IsWhiteSpace(command[7]))
        {
            return false;
        }

        var destination = new MessageDestination(
            commandMessageIdentification.GuildId,
            commandMessageIdentification.ChannelId,
            commandMessageIdentification.MessageId
        );

        var query = command.Length > 7 ? command[7..].Trim() : string.Empty;
        if (string.IsNullOrWhiteSpace(query))
        {
            await chatClient.SendMessage(
                destination,
                "Invalid usage: csearch query cannot be empty"
            );
            return true;
        }

        var response = await mlService.EmbedConversationTextAsync(
            new ConversationTextEmbedRequest
            {
                Text = query,
                Mode = ConversationTextEmbedMode.Query,
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

        var queryEmbedding = new Vector(response.Content.Embedding);
        var results = await dbContext
            .ConversationFragment.AsNoTracking()
            .Where(fragment => fragment.GuildId == commandMessageIdentification.GuildId)
            .ConversationFragmentsWithClosestEmbedding(queryEmbedding)
            .Take(ConversationSearchOptions.ResultCount)
            .ToListAsync(ct);

        if (results.Count == 0)
        {
            await chatClient.SendMessage(
                destination,
                "No conversation fragments available to search"
            );
            return true;
        }

        var builder = new DiscordMessageBuilder();

        for (var i = 0; i < results.Count; i++)
        {
            var result = results[i];
            var lastMessageIdentifier = new ChatMessageIdentifier(
                result.GuildId,
                result.ChannelId,
                result.LastMessageId
            );
            var embed = new DiscordEmbedBuilder()
                .WithTitle($"Result #{i + 1} - Match: {result.CosineSimilarity:0.00000000}")
                .WithDescription(
                    $"{result.StartedAt:yyyy-MM-dd HH:mm}-{result.EndedAt:HH:mm}\n"
                        + $"{result.MessageCount} messages\n"
                        + $"Start: {result.FirstMessageIdentifier.GetUri()}\n"
                        + $"End: {lastMessageIdentifier.GetUri()}"
                );

            builder.AddEmbed(embed);
        }

        if (EditBotResponseMessageId is not null)
        {
            var responseMessageId = commandMessageIdentification with
            {
                PosterId = chatClient.Utils.ShitpostBotId(),
                MessageId = EditBotResponseMessageId.Value,
            };

            var updated = await chatClient.UpdateMessage(responseMessageId, builder);
            if (!updated)
            {
                await chatClient.SendMessage(destination, builder);
            }
        }
        else
        {
            await chatClient.SendMessage(destination, builder);
        }

        return true;
    }
}
