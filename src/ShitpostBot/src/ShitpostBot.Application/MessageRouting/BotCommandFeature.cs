using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.MessageRouting;

public abstract class BotCommandFeature(IChatClient chatClient) : IMessageFeature
{
    public virtual string? HelpMessage => null;

    protected ulong? EditBotResponseMessageId { get; private set; }

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

        return await TryHandleCommand(created.Id, command, created.RepliedToId, ct);
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

        EditBotResponseMessageId = await chatClient.FindReplyToMessage(updated.Id);

        return await TryHandleCommand(updated.Id, command, updated.RepliedToId, ct);
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
        return content[(content.IndexOf('>') + 1)..].Trim();
    }
}