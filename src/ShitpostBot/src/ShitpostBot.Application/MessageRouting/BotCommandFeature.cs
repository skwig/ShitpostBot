using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.MessageRouting;

public abstract class BotCommandFeature(IChatClient chatClient) : IMessageFeature
{
    public virtual string? HelpMessage => null;

    protected ulong? EditBotResponseMessageId { get; private set; }

    public async Task<bool> TryHandleCreate(IncomingMessage created, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(created.Content))
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

        try
        {
            return await TryHandleCommand(created.Id, command, created.RepliedToId, ct);
        }
        catch (Exception e)
        {
            await chatClient.SendMessage(
                new MessageDestination(
                    created.Id.GuildId,
                    created.Id.ChannelId,
                    created.Id.MessageId),
                e.ToString()
            );

            return true;
        }
    }

    public async Task<bool> TryHandleUpdate(IncomingMessage old, IncomingMessage updated, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(updated.Content))
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

        try
        {
            return await TryHandleCommand(updated.Id, command, updated.RepliedToId, ct);
        }
        catch (Exception e)
        {
            await chatClient.SendMessage(
                new MessageDestination(
                    updated.Id.GuildId,
                    updated.Id.ChannelId,
                    updated.Id.MessageId),
                e.ToString()
            );

            return true;
        }
    }

    protected abstract Task<bool> TryHandleCommand(
        MessageIdentification commandMessageIdentification,
        string command,
        MessageIdentification? referenced,
        CancellationToken ct);

    private bool IsBotMention(string content)
    {
        var botId = chatClient.Utils.ShitpostBotId();
        return content.StartsWith(chatClient.Utils.Mention(botId)) ||
               content.StartsWith(chatClient.Utils.Mention(botId, true));
    }

    private static string ParseCommand(string content)
    {
        return content[(content.IndexOf('>') + 1)..].Trim();
    }
}