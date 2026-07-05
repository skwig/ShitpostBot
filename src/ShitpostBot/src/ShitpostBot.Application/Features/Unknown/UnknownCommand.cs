using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.Unknown;

public class UnknownCommand(IChatClient chatClient) : IMessageFeature
{
    public async Task<bool> TryHandleCreate(IncomingMessage created, CancellationToken ct)
    {
        if (created.Content == null)
        {
            return false;
        }

        var botId = chatClient.Utils.ShitpostBotId();
        var isMention = created.Content.StartsWith(chatClient.Utils.Mention(botId))
                        || created.Content.StartsWith(chatClient.Utils.Mention(botId, true));

        if (!isMention)
        {
            return false;
        }

        var afterMention = created.Content[(created.Content.IndexOf('>') + 1)..].Trim();

        var destination = new MessageDestination(created.Id.GuildId, created.Id.ChannelId, created.Id.MessageId);
        await chatClient.SendMessage(destination, $"I don't know how to '{afterMention}'");

        return true;
    }
}