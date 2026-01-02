using Microsoft.EntityFrameworkCore;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.BotCommands.Stats;

public class StatsBotCommandHandler(
    IDbContext dbContext,
    IChatClient chatClient)
    : IBotCommandHandler
{
    public string? GetHelpMessage() => "`stats` - displays count of posts available for repost detection";

    public async Task<bool> TryHandle(
        MessageIdentification commandMessageIdentification,
        MessageIdentification? referencedMessageIdentification,
        BotCommand command,
        BotCommandEdit? edit)
    {
        if (command.Command != "stats")
        {
            return false;
        }

        var messageDestination = new MessageDestination(
            commandMessageIdentification.GuildId,
            commandMessageIdentification.ChannelId,
            commandMessageIdentification.MessageId
        );

        // Count ImagePosts that are available AND have features extracted
        var availableImagePostCount = await dbContext.ImagePost
            .AsNoTracking()
            .Where(p => p.IsPostAvailable && p.Image.ImageFeatures != null)
            .CountAsync();

        // Count all LinkPosts (they're always available)
        var availableLinkPostCount = await dbContext.LinkPost
            .AsNoTracking()
            .CountAsync();

        // Get oldest and newest post timestamps using SQL MIN/MAX
        DateTimeOffset? oldestPost = null;
        DateTimeOffset? newestPost = null;

        if (availableImagePostCount > 0 || availableLinkPostCount > 0)
        {
            var oldestImagePost = availableImagePostCount > 0
                ? await dbContext.ImagePost
                    .AsNoTracking()
                    .Where(p => p.IsPostAvailable && p.Image.ImageFeatures != null)
                    .MinAsync(p => (DateTimeOffset?)p.PostedOn)
                : null;

            var newestImagePost = availableImagePostCount > 0
                ? await dbContext.ImagePost
                    .AsNoTracking()
                    .Where(p => p.IsPostAvailable && p.Image.ImageFeatures != null)
                    .MaxAsync(p => (DateTimeOffset?)p.PostedOn)
                : null;

            var oldestLinkPost = availableLinkPostCount > 0
                ? await dbContext.LinkPost
                    .AsNoTracking()
                    .MinAsync(p => (DateTimeOffset?)p.PostedOn)
                : null;

            var newestLinkPost = availableLinkPostCount > 0
                ? await dbContext.LinkPost
                    .AsNoTracking()
                    .MaxAsync(p => (DateTimeOffset?)p.PostedOn)
                : null;

            var timestamps = new[] { oldestImagePost, oldestLinkPost, newestImagePost, newestLinkPost }
                .Where(d => d.HasValue)
                .Select(d => d!.Value)
                .ToList();

            if (timestamps.Any())
            {
                oldestPost = timestamps.Min();
                newestPost = timestamps.Max();
            }
        }

        var message = $"**ShitpostBot Stats**\n\n" +
                      $"Available ImagePosts: {availableImagePostCount}\n" +
                      $"Available LinkPosts: {availableLinkPostCount}\n" +
                      $"Total: {availableImagePostCount + availableLinkPostCount}";

        if (oldestPost.HasValue && newestPost.HasValue)
        {
            message += $"\n\nOldest post: {chatClient.Utils.RelativeTimestamp(oldestPost.Value)}\n" +
                       $"Newest post: {chatClient.Utils.RelativeTimestamp(newestPost.Value)}";
        }

        await chatClient.SendMessage(messageDestination, message);
        
        return true;
    }
}
