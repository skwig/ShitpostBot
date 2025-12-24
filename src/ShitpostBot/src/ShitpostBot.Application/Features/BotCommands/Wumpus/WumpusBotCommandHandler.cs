using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.BotCommands.Wumpus;

public class WumpusBotCommandHandler(IChatClient chatClient) : IBotCommandHandler
{
    public string? GetHelpMessage() => null;

    public async Task<bool> TryHandle(
        MessageIdentification commandMessageIdentification, 
        MessageIdentification? referencedMessageIdentification,
        BotCommand command,
        BotCommandEdit? edit)
    {
        if (command.Command != "what is your opinion on wumpus")
        {
            return false;
        }

        var messageDestination = new MessageDestination(
            commandMessageIdentification.GuildId,
            commandMessageIdentification.ChannelId,
            commandMessageIdentification.MessageId
        );

        var message = $"https://tenor.com/view/wumpus-discord-funny-meme-repost-gif-21342739";

        await chatClient.SendMessage(
            messageDestination,
            message
        );
        return true;
    }
}