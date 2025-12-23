using Microsoft.EntityFrameworkCore;
using Pgvector;
using ShitpostBot.Application.Extensions;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.BotCommands.Search;

public class SearchBotCommandHandler(
    IDbContext dbContext,
    IChatClient chatClient,
    IImageFeatureExtractorApi mlService)
    : IBotCommandHandler
{
    private const decimal LowConfidenceThreshold = 0.8m;
    private const int ResultLimit = 5;

    public string? GetHelpMessage() => 
        "`search <query>` - search for images using natural language";

    public async Task<bool> TryHandle(
        MessageIdentification commandMessageIdentification,
        MessageIdentification? referencedMessageIdentification,
        BotCommand command)
    {
        // Only handle "search <query>" commands
        if (!command.Command.StartsWith("search ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var messageDestination = new MessageDestination(
            commandMessageIdentification.GuildId,
            commandMessageIdentification.ChannelId,
            commandMessageIdentification.MessageId
        );

        // Extract query text after "search "
        var query = command.Command[7..].Trim();
        
        if (string.IsNullOrWhiteSpace(query))
        {
            await chatClient.SendMessage(
                messageDestination,
                "Invalid usage: search query cannot be empty"
            );
            return true;
        }

        // Get text embedding from ML service
        var embedResponse = await mlService.EmbedTextAsync(new TextEmbedRequest { Text = query });
        
        if (!embedResponse.IsSuccessful)
        {
            throw embedResponse.Error ?? new Exception("Failed to generate text embedding");
        }

        var textEmbedding = new Vector(embedResponse.Content.Embedding);

        // Query database for similar images
        var similarPosts = await dbContext.ImagePost
            .AsNoTracking()
            .ImagePostsWithClosestFeatureVector(textEmbedding)
            .Take(ResultLimit)
            .ToListAsync();

        if (similarPosts.Count == 0)
        {
            await chatClient.SendMessage(
                messageDestination,
                "No images available to search"
            );
            return true;
        }

        // Determine if results are low confidence
        var bestMatch = similarPosts.First();
        var isLowConfidence = bestMatch.CosineSimilarity < (double)LowConfidenceThreshold;
        
        var headerMessage = isLowConfidence
            ? "Best matches found (low confidence):\nHigher is a closer match:\n"
            : "Higher is a closer match:\n";

        var resultsMessage = string.Join("\n",
            similarPosts.Select((p, i) =>
                $"{i + 1}. Match of `{p.CosineSimilarity:0.00000000}` with {p.ChatMessageIdentifier.GetUri()} posted {chatClient.Utils.RelativeTimestamp(p.PostedOn)}"
            )
        );

        await chatClient.SendMessage(
            messageDestination,
            headerMessage + resultsMessage
        );

        return true;
    }
}
