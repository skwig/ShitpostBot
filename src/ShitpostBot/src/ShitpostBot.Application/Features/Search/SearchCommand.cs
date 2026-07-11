using DSharpPlus.Entities;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using ShitpostBot.Application.Extensions;
using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Extensions;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.Search;

public class SearchCommand(
    ILogger<SearchCommand> logger,
    IDbContext dbContext,
    IChatClient chatClient,
    ImageFeatureExtractor.ImageFeatureExtractorClient mlService
) : BotCommandFeature(chatClient)
{
    private const int ResultLimit = 5;

    public override string? HelpMessage =>
        "`search <query>` - search for images using natural language";

    protected override async Task<bool> TryHandleCommand(
        MessageIdentification commandMessageIdentification,
        string command,
        MessageIdentification? referenced,
        CancellationToken ct
    )
    {
        if (!command.StartsWith("search ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var destination = new MessageDestination(
            commandMessageIdentification.GuildId,
            commandMessageIdentification.ChannelId,
            commandMessageIdentification.MessageId
        );

        var query = command[7..].Trim();

        if (string.IsNullOrWhiteSpace(query))
        {
            await chatClient.SendMessage(
                destination,
                "Invalid usage: search query cannot be empty"
            );
            return true;
        }

        try
        {
            var embedResponse = await mlService.EmbedTextAsync(
                new EmbedTextRequest { Text = query },
                deadline: DateTime.UtcNow.AddSeconds(30),
                cancellationToken: ct
            );

            var textEmbedding = new Vector(embedResponse.Embedding.ToArray());

            var similarPosts = await dbContext
                .ImagePost.AsNoTracking()
                .ImagePostsWithClosestFeatureVector(textEmbedding)
                .Take(ResultLimit)
                .ToListAsync(ct);

            if (similarPosts.Count == 0)
            {
                await chatClient.SendMessage(destination, "No images available to search");
                return true;
            }

            var messageBuilder = new DiscordMessageBuilder();

            for (int i = 0; i < similarPosts.Count; i++)
            {
                var post = similarPosts[i];

                var embed = new DiscordEmbedBuilder()
                    .WithTitle($"Result #{i + 1} - Match: {post.CosineSimilarity:0.00000000}")
                    .WithDescription(
                        $"{post.ChatMessageIdentifier.GetUri()}\nPosted {chatClient.Utils.RelativeTimestamp(post.PostedOn)}"
                    )
                    .WithThumbnail(post.ImageUri.ToString());

                messageBuilder.AddEmbed(embed);
            }

            if (EditBotResponseMessageId is not null)
            {
                var responseMessageId = commandMessageIdentification with
                {
                    PosterId = chatClient.Utils.ShitpostBotId(),
                    MessageId = EditBotResponseMessageId.Value,
                };

                var updated = await chatClient.UpdateMessage(responseMessageId, messageBuilder);

                if (!updated)
                {
                    await chatClient.SendMessage(destination, messageBuilder);
                }
            }
            else
            {
                await chatClient.SendMessage(destination, messageBuilder);
            }

            return true;
        }
        catch (RpcException ex)
        {
            logger.LogWarning(
                "Failed to generate text embedding (status: {StatusCode}, detail: {Detail})",
                ex.StatusCode,
                ex.Status.Detail
            );
            await chatClient.SendMessage(
                destination,
                "Search unavailable, please try again later."
            );
            return true;
        }
    }
}
