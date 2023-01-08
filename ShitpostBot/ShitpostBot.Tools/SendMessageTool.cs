using System.Threading.Tasks;
using DSharpPlus;
using NUnit.Framework;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Tools;

public class SendMessageTool
{
    [Test]
    public async Task SendMessage()
    {
        var chatClient = new DiscordChatClient(new DiscordClient(new DiscordConfiguration
        {
            Token = "",
            TokenType = TokenType.Bot,
                    
            MessageCacheSize = 2048,
            Intents = DiscordIntents.All
        }));

        // "https://discord.com/channels/131669486305673216/655398447557640201/1061328503930368161";
        await chatClient.ConnectAsync();
        await chatClient.SendMessage(new MessageDestination(131669486305673216, 138031010951593984), "https://media.tenor.com/EoSoBWSjRQkAAAAC/paul-atreides-dune.gif");
    }
}