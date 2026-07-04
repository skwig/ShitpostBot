using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.Wumpus;

public class WumpusFeature(IChatClient chatClient) : BotCommandFeature
{
    public override string? HelpMessage => null;

    protected override async Task<bool> TryHandleCommand(
        MessageIdentification commandMessageIdentification,
        string command,
        MessageIdentification? referenced,
        CancellationToken ct)
    {
        if (command != "what is your opinion on wumpus")
        {
            return false;
        }

        var destination = new MessageDestination(
            commandMessageIdentification.GuildId,
            commandMessageIdentification.ChannelId,
            commandMessageIdentification.MessageId
        );

        await chatClient.SendMessage(
            destination,
            "https://tenor.com/view/wumpus-discord-funny-meme-repost-gif-21342739"
        );

        return true;
    }
}