using Microsoft.EntityFrameworkCore;
using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.Stats;

public class StatsCommand(IDbContext dbContext, IChatClient chatClient, IMetrics metrics)
    : BotCommandFeature(chatClient)
{
    public override string? HelpMessage =>
        "`stats` - displays count of posts available for repost detection";

    protected override async Task<bool> TryHandleCommand(
        MessageIdentification commandMessageIdentification,
        string command,
        MessageIdentification? referenced,
        CancellationToken ct
    )
    {
        if (command != "stats")
        {
            return false;
        }

        var destination = new MessageDestination(
            commandMessageIdentification.GuildId,
            commandMessageIdentification.ChannelId,
            commandMessageIdentification.MessageId
        );

        var imagePostStats = await dbContext
            .ImagePost.AsNoTracking()
            .Where(p => p.IsPostAvailable && p.Image.ImageFeatures != null)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                OldestPostedOn = g.Min(p => p.PostedOn),
                NewestPostedOn = g.Max(p => p.PostedOn),
            })
            .FirstOrDefaultAsync(ct);

        var linkPostStats = await dbContext
            .LinkPost.AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                OldestPostedOn = g.Min(p => p.PostedOn),
                NewestPostedOn = g.Max(p => p.PostedOn),
            })
            .FirstOrDefaultAsync(ct);

        var availableImagePostCount = imagePostStats?.Count ?? 0;
        var oldestImagePost = imagePostStats?.OldestPostedOn ?? DateTimeOffset.MinValue;
        var newestImagePost = imagePostStats?.NewestPostedOn ?? DateTimeOffset.MaxValue;

        var availableLinkPostCount = linkPostStats?.Count ?? 0;
        var oldestLinkPost = linkPostStats?.OldestPostedOn ?? DateTimeOffset.MinValue;
        var newestLinkPost = linkPostStats?.NewestPostedOn ?? DateTimeOffset.MaxValue;

        var message =
            $"Available ImagePosts: {availableImagePostCount} ({chatClient.Utils.RelativeTimestamp(oldestImagePost)} - {chatClient.Utils.RelativeTimestamp(newestImagePost)})"
            + $"\nAvailable LinkPosts: {availableLinkPostCount} ({chatClient.Utils.RelativeTimestamp(oldestLinkPost)} - {chatClient.Utils.RelativeTimestamp(newestLinkPost)})"
            + $"\nTotal: {availableImagePostCount + availableLinkPostCount}"
            + $"\n"
            + $"\nDeployed: {chatClient.Utils.RelativeTimestamp(metrics.DeployedOn)}"
            + $"\nLast link save: {(metrics.LastLinkSaveTimestamp is not null ? chatClient.Utils.RelativeTimestamp(metrics.LastLinkSaveTimestamp.Value) : "never")}"
            + $"\nLast image save: {(metrics.LastImageSaveTimestamp is not null ? chatClient.Utils.RelativeTimestamp(metrics.LastImageSaveTimestamp.Value) : "never")}"
            + $"\nLast image evaluation: {(metrics.LastImageEvaluationTimestamp is not null ? chatClient.Utils.RelativeTimestamp(metrics.LastImageEvaluationTimestamp.Value) : "never")}";

        await chatClient.SendMessage(destination, message);

        return true;
    }
}