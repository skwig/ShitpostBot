using Microsoft.Extensions.DependencyInjection;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.BotCommands.Help;

public class HelpBotCommandHandler(IServiceProvider serviceProvider, IChatClient chatClient) : IBotCommandHandler
{
    public string? GetHelpMessage() => "`help` - prints this help message";

    public async Task<bool> TryHandle(
        MessageIdentification commandMessageIdentification,
        MessageIdentification? referencedMessageIdentification,
        BotCommand command,
        bool isEdit = false,
        ulong? botResponseMessageId = null)
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