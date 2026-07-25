using System.Collections.Concurrent;
using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.DeletedMessages;

public record DeletedMessage(
    ulong AuthorId,
    string AuthorName,
    string Content,
    DateTimeOffset Timestamp
);

public class DeletedMessagesFeature(IChatClient chatClient) : IMessageFeature
{
    private const int MaxMessagesPerChannel = 50;

    private readonly ConcurrentDictionary<ulong, List<DeletedMessage>> channels = new();

    public void Store(ulong channelId, DeletedMessage message)
    {
        var list = channels.GetOrAdd(channelId, _ => new List<DeletedMessage>());

        lock (list)
        {
            list.Add(message);
            if (list.Count > MaxMessagesPerChannel)
            {
                list.RemoveAt(0);
            }
        }
    }

    public async Task<bool> TryHandleCreate(IncomingMessage created, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(created.Content))
        {
            return false;
        }

        var botId = chatClient.Utils.ShitpostBotId();
        var isMention =
            created.Content.StartsWith(chatClient.Utils.Mention(botId))
            || created.Content.StartsWith(chatClient.Utils.Mention(botId, true));

        if (!isMention)
        {
            return false;
        }

        var command = created.Content[(created.Content.IndexOf('>') + 1)..].Trim();

        if (!command.StartsWith("deleted"))
        {
            return false;
        }

        var destination = new MessageDestination(
            created.Id.GuildId,
            created.Id.ChannelId,
            created.Id.MessageId
        );

        var channelId = created.Id.ChannelId;

        var n = 10;
        var args = command["deleted".Length..].Trim();
        if (args.Length > 0 && int.TryParse(args, out var requested) && requested > 0)
        {
            n = Math.Min(requested, 50);
        }

        var messages = GetLastN(channelId, n);

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
                var mention = chatClient.Utils.Mention(m.AuthorId);
                var timestamp = chatClient.Utils.RelativeTimestamp(m.Timestamp);
                return $"{i + 1}. {mention} — \"{truncated}\" {timestamp}";
            }
        );

        var header = $"Last {messages.Count} deleted messages in <#{channelId}>:";
        var response = header + "\n" + string.Join("\n", lines);

        await chatClient.SendMessage(destination, response);
        return true;
    }

    private IReadOnlyList<DeletedMessage> GetLastN(ulong channelId, int n)
    {
        if (!channels.TryGetValue(channelId, out var list))
        {
            return Array.Empty<DeletedMessage>();
        }

        lock (list)
        {
            var count = Math.Min(n, list.Count);
            return list.Skip(list.Count - count).ToList().AsReadOnly();
        }
    }
}
