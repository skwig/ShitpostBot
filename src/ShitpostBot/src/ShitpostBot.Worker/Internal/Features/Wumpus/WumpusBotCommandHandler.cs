using System.Threading.Tasks;
using ShitpostBot.Infrastructure;
using ShitpostBot.Application.Features.BotCommands;

namespace ShitpostBot.Worker.Features.Wumpus;

public class WumpusBotCommandHandler(IChatClient chatClient) : IBotCommandHandler
{
    public string? GetHelpMessage() => null;

    public async Task<bool> TryHandle(MessageIdentification commandMessageIdentification, MessageIdentification? referencedMessageIdentification,
        BotCommand command)
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