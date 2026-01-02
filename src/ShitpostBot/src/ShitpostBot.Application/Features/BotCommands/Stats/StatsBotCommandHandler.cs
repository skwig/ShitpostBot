using Microsoft.EntityFrameworkCore;
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
            .Where(p => p.IsPostAvailable)
            .Where(p => p.Image.ImageFeatures != null)
            .CountAsync();

        // Count all LinkPosts (they're always available)
        var availableLinkPostCount = await dbContext.LinkPost
            .CountAsync();

        var message = $"**ShitpostBot Stats**\n\n" +
                      $"Available ImagePosts: {availableImagePostCount}\n" +
                      $"Available LinkPosts: {availableLinkPostCount}\n" +
                      $"Total: {availableImagePostCount + availableLinkPostCount}";

        await chatClient.SendMessage(messageDestination, message);
        
        return true;
    }
}
