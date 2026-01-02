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

        var oldestImagePost = await dbContext.ImagePost
            .AsNoTracking()
            .Where(p => p.IsPostAvailable && p.Image.ImageFeatures != null)
            .MinAsync(p => p.PostedOn);

        var newestImagePost = await dbContext.ImagePost
            .AsNoTracking()
            .Where(p => p.IsPostAvailable && p.Image.ImageFeatures != null)
            .MaxAsync(p => p.PostedOn);

        var oldestLinkPost =
            await dbContext.LinkPost
                .AsNoTracking()
                .MinAsync(p => p.PostedOn);

        var newestLinkPost = await dbContext.LinkPost
            .AsNoTracking()
            .MaxAsync(p => p.PostedOn);

        var message =
            $"Available ImagePosts: {availableImagePostCount} ({chatClient.Utils.RelativeTimestamp(oldestImagePost)} - {chatClient.Utils.RelativeTimestamp(newestImagePost)})\n" +
            $"Available LinkPosts: {availableLinkPostCount} ({chatClient.Utils.RelativeTimestamp(oldestLinkPost)} - {chatClient.Utils.RelativeTimestamp(newestLinkPost)})\n" +
            $"Total: {availableImagePostCount + availableLinkPostCount}";

        await chatClient.SendMessage(messageDestination, message);

        return true;
    }
}