using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Worker
{
    public class HelpBotCommandHandler : IBotCommandHandler
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IChatClient chatClient;

        public HelpBotCommandHandler(IServiceProvider serviceProvider, IChatClient chatClient)
        {
            this.serviceProvider = serviceProvider;
            this.chatClient = chatClient;
        }

        public string GetHelpMessage() => "`help` - prints this help message";

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
                .OrderBy(m => m);

            var helpMessagesText = string.Join('\n', allHelpMessages);

            var finalMessage = $"{helpMessagesText}\n" +
                               $"\n" +
                               $"I'm also open source {chatClient.Utils.Emoji(":bugman:")} https://github.com/skwig/ShitpostBot";

            await chatClient.SendMessage(messageDestination, finalMessage);

            return true;
        }
    }
}