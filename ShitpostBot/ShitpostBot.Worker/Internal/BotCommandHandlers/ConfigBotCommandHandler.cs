using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Worker
{
    public class ConfigBotCommandHandler : IBotCommandHandler
    {
        private static readonly DateTimeOffset deployedOn;
        
        static ConfigBotCommandHandler()
        {
            deployedOn = DateTimeOffset.UtcNow;
        }

        private readonly IChatClient chatClient;
        private readonly IOptions<RepostServiceOptions> repostServiceOptions;
        private readonly IHostEnvironment hostEnvironment;

        public ConfigBotCommandHandler(IChatClient chatClient, IOptions<RepostServiceOptions> repostServiceOptions, IHostEnvironment hostEnvironment)
        {
            this.chatClient = chatClient;
            this.repostServiceOptions = repostServiceOptions;
            this.hostEnvironment = hostEnvironment;
        }

        public string? GetHelpMessage() => "`about` - prints information about the bot";

        public async Task<bool> TryHandle(MessageIdentification commandMessageIdentification, MessageIdentification? referencedMessageIdentification,
            BotCommand command)
        {
            if (command.Command != "about")
            {
                return false;
            }

            var messageDestination = new MessageDestination(
                commandMessageIdentification.GuildId,
                commandMessageIdentification.ChannelId,
                commandMessageIdentification.MessageId
            );

            var utcNow = DateTimeOffset.UtcNow;
            var message = $"Uptime: {(utcNow - deployedOn).TotalHours} hours\n" +
                          $"\n" +
                          $"I'm also open source {chatClient.Utils.Emoji(":bugman:")} https://github.com/skwig/ShitpostBot" +
                          $"\n" +
                          $"Config:\n" +
                          $"`{nameof(hostEnvironment.EnvironmentName)}: {hostEnvironment.EnvironmentName}`\n" +
                          $"`{nameof(repostServiceOptions.Value.RepostSimilarityThreshold)}: {repostServiceOptions.Value.RepostSimilarityThreshold}`\n";
            
            await chatClient.SendMessage(
                messageDestination,
                message
            );
            return true;
        }
    }
}