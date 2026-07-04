using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.MessageRouting;

public abstract class BotCommandFeature : IMessageFeature
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
        return await TryHandleCommand(updated.Id, command, updated.RepliedToId, ct);
    }

    protected abstract Task<bool> TryHandleCommand(
        MessageIdentification commandMessageIdentification,
        string command,
        MessageIdentification? referenced,
        CancellationToken ct);

    private static bool IsBotMention(string content)
    {
        return content.StartsWith("<@") && content.Contains('>');
    }

    private static string ParseCommand(string content)
    {
        var afterMention = content[(content.IndexOf('>') + 1)..].Trim();
        return afterMention;
    }
}
