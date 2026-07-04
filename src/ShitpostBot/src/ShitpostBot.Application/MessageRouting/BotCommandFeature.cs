using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.MessageRouting;

public abstract class BotCommandFeature(IChatClient chatClient) : IMessageFeature
{
    public virtual string? HelpMessage => null;

    public async Task<bool> TryHandleCreate(IncomingMessage created, CancellationToken ct)
    {
        if (created.Content == null)
        {
            return false;
        }

        if (!IsBotMention(created.Content))
        {
            return false;
        }

        var command = ParseCommand(created.Content);

        if (string.IsNullOrWhiteSpace(command))
        {
            return false;
        }

        var handled = await TryHandleCommand(created.Id, command, created.RepliedToId, ct);

        if (!handled)
        {
            var destination = new MessageDestination(created.Id.GuildId, created.Id.ChannelId, created.Id.MessageId);
            await chatClient.SendMessage(destination, $"I don't know how to '{command}'");
        }

        return true;
    }

    public async Task<bool> TryHandleUpdate(IncomingMessage old, IncomingMessage updated, CancellationToken ct)
    {
        if (updated.Content == null)
        {
            return false;
        }

        if (!IsBotMention(updated.Content))
        {
            return false;
        }

        var command = ParseCommand(updated.Content);

        if (string.IsNullOrWhiteSpace(command))
        {
            return false;
        }

        var handled = await TryHandleCommand(updated.Id, command, updated.RepliedToId, ct);

        if (!handled)
        {
            var destination = new MessageDestination(updated.Id.GuildId, updated.Id.ChannelId, updated.Id.MessageId);
            await chatClient.SendMessage(destination, $"I don't know how to '{command}'");
        }

        return true;
    }

    protected abstract Task<bool> TryHandleCommand(
        MessageIdentification commandMessageIdentification,
        string command,
        MessageIdentification? referenced,
        CancellationToken ct);

    private bool IsBotMention(string content)
    {
        var botId = chatClient.Utils.ShitpostBotId();
        return content.StartsWith(chatClient.Utils.Mention(botId)) || content.StartsWith(chatClient.Utils.Mention(botId, true));
    }

    private static string ParseCommand(string content)
    {
        var afterMention = content[(content.IndexOf('>') + 1)..].Trim();
        return afterMention;
    }
}