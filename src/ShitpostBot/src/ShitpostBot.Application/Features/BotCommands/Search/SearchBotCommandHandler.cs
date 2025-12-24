using DSharpPlus.Entities;
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
    private const int ResultLimit = 5;

    public string? GetHelpMessage() => 
        "`search <query>` - search for images using natural language";

    public async Task<bool> TryHandle(
        MessageIdentification commandMessageIdentification,
        MessageIdentification? referencedMessageIdentification,
        BotCommand command,
        bool isEdit = false,
        ulong? botResponseMessageId = null)
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

        // Build message with multiple embeds (one per result)
        var messageBuilder = new DiscordMessageBuilder();

        for (int i = 0; i < similarPosts.Count; i++)
        {
            var post = similarPosts[i];
            
            var embed = new DiscordEmbedBuilder()
                .WithTitle($"Result #{i + 1} - Match: {post.CosineSimilarity:0.00000000}")
                .WithDescription($"{post.ChatMessageIdentifier.GetUri()}\nPosted {chatClient.Utils.RelativeTimestamp(post.PostedOn)}")
                .WithThumbnail(post.ImageUri.ToString());
            
            messageBuilder.AddEmbed(embed);
        }

        // Update existing message if this is an edit and we found the response
        if (isEdit && botResponseMessageId.HasValue)
        {
            var responseMessageId = new MessageIdentification(
                commandMessageIdentification.GuildId,
                commandMessageIdentification.ChannelId,
                chatClient.Utils.ShitpostBotId(),
                botResponseMessageId.Value
            );

            var updated = await chatClient.UpdateMessage(responseMessageId, messageBuilder);
            
            if (!updated)
            {
                // Response message was deleted or not found, send new message instead
                await chatClient.SendMessage(messageDestination, messageBuilder);
            }
        }
        else
        {
            // Not an edit, or couldn't find original response - send new message
            await chatClient.SendMessage(messageDestination, messageBuilder);
        }

        return true;
    }
}
