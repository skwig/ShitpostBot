using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ShitpostBot.Infrastructure;
using ShitpostBot.Worker.Core;

namespace ShitpostBot.Worker.Features.Help;

public class HelpBotCommandHandler(IServiceProvider serviceProvider, IChatClient chatClient) : IBotCommandHandler
{
    public string? GetHelpMessage() => "`help` - prints this help message";

    public async Task<bool> TryHandle(MessageIdentification commandMessageIdentification, MessageIdentification? referencedMessageIdentification,
        BotCommand command)
    {
        if (command.Command != "help")
        {
            return false;
        }

        var messageDestination = new MessageDestination(
            commandMessageIdentification.GuildId,
            commandMessageIdentification.ChannelId,
            commandMessageIdentification.MessageId
        );

        var allHelpMessages = serviceProvider.GetServices<IBotCommandHandler>()
            .Select(h => h.GetHelpMessage())
            .Where(h => h != null)
            .OrderBy(m => m);

        var helpMessagesText = string.Join('\n', allHelpMessages);

        await chatClient.SendMessage(messageDestination, helpMessagesText);

        return true;
    }
}