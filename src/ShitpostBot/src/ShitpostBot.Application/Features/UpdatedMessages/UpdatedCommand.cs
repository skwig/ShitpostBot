using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.UpdatedMessages;

public class UpdatedCommand(IChatClient chatClient, UpdatedMessageStore store)
    : BotCommandFeature(chatClient)
{
    public override string? HelpMessage =>
        "`updated [N]` - shows the last N updated messages in this channel (default 10)";

    protected override async Task<bool> TryHandleCommand(
        MessageIdentification commandMessageIdentification,
        string command,
        MessageIdentification? referenced,
        CancellationToken ct
    )
    {
        if (!command.StartsWith("updated"))
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
        var args = command["updated".Length..].Trim();
        if (args.Length > 0 && int.TryParse(args, out var requested) && requested > 0)
        {
            n = Math.Min(requested, 50);
        }

        var messages = store.GetLastN(channelId, n).OrderByDescending(m => m.UpdatedOn).ToList();

        if (messages.Count == 0)
        {
            await chatClient.SendMessage(
                destination,
                "No updated messages recorded in this channel yet."
            );
            return true;
        }

        var lines = messages.Select(
            (m, i) =>
            {
                var before = Truncate(m.BeforeContent);
                var after = Truncate(m.AfterContent);
                var mention = chatClient.Utils.Mention(m.Id.PosterId);
                var posted = chatClient.Utils.RelativeTimestamp(m.PostedOn);
                var updated = chatClient.Utils.RelativeTimestamp(m.UpdatedOn);
                return $"{i + 1}.\n> {mention} {posted} (updated {updated})\n> Before: {before}\n> After: {after}";
            }
        );

        var header = $"Last {messages.Count} updated messages in <#{channelId}>:";
        var response = header + "\n" + string.Join("\n", lines);

        await chatClient.SendMessage(destination, response);
        return true;
    }

    private static string Truncate(string content)
    {
        return content.Length > 100 ? content[..97] + "..." : content;
    }
}
