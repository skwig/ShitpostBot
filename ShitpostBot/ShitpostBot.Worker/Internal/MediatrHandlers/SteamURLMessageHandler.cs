using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
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
                var (messageIdentification) = notification;

                string messageContent = "steam://openurl/" + notification.LinkMessage.Embed.Uri;

                await chatClient.SendMessage(
                    new MessageDestination(messageIdentification.Identification.GuildId,
                        messageIdentification.Identification.ChannelId),
                    messageContent
                );
            }
        }
    }
}