using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.DeletedMessages;

public class DeletedCommand(IChatClient chatClient, DeletedMessageStore store)
    : BotCommandFeature(chatClient)
{
    public override string? HelpMessage =>
        "`deleted [N]` - shows the last N deleted messages in this channel (default 10)";

    protected override async Task<bool> TryHandleCommand(
        MessageIdentification commandMessageIdentification,
        string command,
        MessageIdentification? referenced,
        CancellationToken ct
    )
    {
        if (!command.StartsWith("deleted"))
        {
            return false;
        }

        var destination = new MessageDestination(
            commandMessageIdentification.GuildId,
            commandMessageIdentification.ChannelId,
            commandMessageIdentification.MessageId
        );

        var channelId = commandMessageIdentification.ChannelId;

        var n = 10;
        var args = command["deleted".Length..].Trim();
        if (args.Length > 0 && int.TryParse(args, out var requested) && requested > 0)
        {
            n = Math.Min(requested, 50);
        }

        var messages = store
            .GetLastN(channelId, n)
            .OrderBy(m => m.PostedOn)
            .ToList();

        if (messages.Count == 0)
        {
            await chatClient.SendMessage(
                destination,
                "No deleted messages recorded in this channel yet."
            );
            return true;
        }

        var lines = messages.Select(
            (m, i) =>
            {
                var truncated = m.Content.Length > 100 ? m.Content[..97] + "..." : m.Content;
                var mention = chatClient.Utils.Mention(m.Id.PosterId);
                var posted = chatClient.Utils.RelativeTimestamp(m.PostedOn);
                var deleted = chatClient.Utils.RelativeTimestamp(m.DeletedOn);
                return $"{i + 1}.\n> {mention} {posted} (deleted {deleted})\n> {truncated}";
            }
        );

        var header = $"Last {messages.Count} deleted messages in <#{channelId}>:";
        var response = header + "\n" + string.Join("\n", lines);

        await chatClient.SendMessage(destination, response);
        return true;
    }
}
