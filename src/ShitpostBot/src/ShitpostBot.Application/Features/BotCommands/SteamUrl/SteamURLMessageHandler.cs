using DSharpPlus.Entities;
using MediatR;
using ShitpostBot.Application.Features.PostTracking;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.BotCommands.SteamUrl;

internal class SteamUrlMessageHandler(IChatClient chatClient) :
    INotificationHandler<LinkMessageCreated>
{
    private readonly List<string> steamUrls = ["steamcommunity.com", "https://store.steampowered.com/"];

    public async Task Handle(LinkMessageCreated notification, CancellationToken cancellationToken)
    {
        if (steamUrls.Contains(notification.LinkMessage.Embed.Uri.Host))
        {
            var messageIdentification = notification.LinkMessage;

            var messageContent = "steam://openurl/" + notification.LinkMessage.Embed.Uri;

            var embeddedMessage = new DiscordEmbedBuilder()
                .WithTitle("Open in Steam")
                .WithUrl(messageContent);

            await chatClient.SendEmbeddedMessage(
                new MessageDestination(messageIdentification.Identification.GuildId,
                    messageIdentification.Identification.ChannelId),
                embeddedMessage.Build()
            );
        }
    }
}