using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using MediatR;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Worker
{
    internal class SteamURLMessageHander :
        INotificationHandler<LinkMessageCreated>
    {
        private readonly IChatClient chatClient;

        private List<string> steamURLs = new List<string> {"steamcommunity.com", "https://store.steampowered.com/"};

        public SteamURLMessageHander(IChatClient chatClient)
        {
            this.chatClient = chatClient;
        }

        public async Task Handle(LinkMessageCreated notification, CancellationToken cancellationToken)
        {
            if (steamURLs.Contains(notification.LinkMessage.Embed.Uri.Host))
            {
                var messageIdentification = notification.LinkMessage;

                string messageContent = "steam://openurl/" + notification.LinkMessage.Embed.Uri;

                var embeddedMessage = new DiscordEmbedBuilder()
                    .WithTitle("Open in Steam")
                    .WithUrl(messageContent);

                await chatClient.SendEmbeddedMessage(
                    new MessageDestination(messageIdentification.Identification.GuildId,
                        messageIdentification.Identification.ChannelId),
                    embeddedMessage
                );
            }
        }
    }
}