using System.Threading.Tasks;
using ShitpostBot.Infrastructure;
using Unleash;

namespace ShitpostBot.Worker
{
    public class SugmaBallsBotCommandHandler : IBotCommandHandler
    {
        private readonly IChatClient chatClient;
        private readonly IUnleash unleashClient;

        public SugmaBallsBotCommandHandler(IChatClient chatClient, IUnleash unleashClient)
        {
            this.chatClient = chatClient;
            this.unleashClient = unleashClient;
        }

        public string GetHelpMessage() => $"`sugma balls` - {chatClient.Utils.Emoji(":weary:")}";

        public async Task<bool> TryHandle(MessageIdentification commandMessageIdentification, MessageIdentification? referencedMessageIdentification,
            BotCommand command)
        {
            if (!unleashClient.IsEnabled("ShitpostBot.Command.Sugma"))
            {
                return false;
            }
            
            if (command.Command != "sugma balls")
            {
                return false;
            }

            var messageDestination = new MessageDestination(
                commandMessageIdentification.GuildId,
                commandMessageIdentification.ChannelId,
                commandMessageIdentification.MessageId
            );

            await chatClient.SendMessage(
                messageDestination,
                chatClient.Utils.Emoji(":face_with_raised_eyebrow:")
            );

            return true;
        }
    }
}