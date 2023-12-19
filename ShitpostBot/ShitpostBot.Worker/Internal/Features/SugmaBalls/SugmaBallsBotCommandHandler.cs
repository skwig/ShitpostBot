using System.Threading.Tasks;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Worker
{
    public class SugmaBallsBotCommandHandler : IBotCommandHandler
    {
        private readonly IChatClient chatClient;

        public SugmaBallsBotCommandHandler(IChatClient chatClient)
        {
            this.chatClient = chatClient;
        }

        public string? GetHelpMessage() => null;

        public async Task<bool> TryHandle(MessageIdentification commandMessageIdentification, MessageIdentification? referencedMessageIdentification,
            BotCommand command)
        {
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