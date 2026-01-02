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

        var imagePostStats = await dbContext.ImagePost
            .AsNoTracking()
            .Where(p => p.IsPostAvailable && p.Image.ImageFeatures != null)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                OldestPostedOn = g.Min(p => p.PostedOn),
                NewestPostedOn = g.Max(p => p.PostedOn)
            })
            .FirstOrDefaultAsync();

        var linkPostStats = await dbContext.LinkPost
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                OldestPostedOn = g.Min(p => p.PostedOn),
                NewestPostedOn = g.Max(p => p.PostedOn)
            })
            .FirstOrDefaultAsync();

        var availableImagePostCount = imagePostStats?.Count ?? 0;
        var oldestImagePost = imagePostStats?.OldestPostedOn ?? DateTimeOffset.MinValue;
        var newestImagePost = imagePostStats?.NewestPostedOn ?? DateTimeOffset.MaxValue;

        var availableLinkPostCount = linkPostStats?.Count ?? 0;
        var oldestLinkPost = linkPostStats?.OldestPostedOn ?? DateTimeOffset.MinValue;
        var newestLinkPost = linkPostStats?.NewestPostedOn ?? DateTimeOffset.MaxValue;

        var message =
            $"Available ImagePosts: {availableImagePostCount} ({chatClient.Utils.RelativeTimestamp(oldestImagePost)} - {chatClient.Utils.RelativeTimestamp(newestImagePost)})\n" +
            $"Available LinkPosts: {availableLinkPostCount} ({chatClient.Utils.RelativeTimestamp(oldestLinkPost)} - {chatClient.Utils.RelativeTimestamp(newestLinkPost)})\n" +
            $"Total: {availableImagePostCount + availableLinkPostCount}";

        await chatClient.SendMessage(messageDestination, message);

        return true;
    }
}