using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Worker
{
    public class ConfigBotCommandHandler : IBotCommandHandler
    {
        private readonly IChatClient chatClient;
        private readonly IOptions<RepostServiceOptions> repostServiceOptions;
        private readonly IHostEnvironment hostEnvironment;

        public ConfigBotCommandHandler(IChatClient chatClient, IOptions<RepostServiceOptions> repostServiceOptions, IHostEnvironment hostEnvironment)
        {
            this.chatClient = chatClient;
            this.repostServiceOptions = repostServiceOptions;
            this.hostEnvironment = hostEnvironment;
        }

        public string GetHelpMessage() => "`config` - prints relevant configuration values";

        public async Task<bool> TryHandle(MessageIdentification commandMessageIdentification, MessageIdentification? referencedMessageIdentification,
            BotCommand command)
        {
            if (command.Command != "config")
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
                $"`{nameof(hostEnvironment.EnvironmentName)}: {hostEnvironment.EnvironmentName}`\n"+
                $"`{nameof(repostServiceOptions.Value.RepostSimilarityThreshold)}: {repostServiceOptions.Value.RepostSimilarityThreshold}`\n"
            );
            return true;
        }
    }
}