using System.Collections.Concurrent;

namespace ShitpostBot.Application.Features.DeletedMessages;

public record DeletedMessage(
    ulong AuthorId,
    string AuthorName,
    string Content,
    DateTimeOffset Timestamp
);

public class DeletedMessageStore
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

    public IReadOnlyList<DeletedMessage> GetLastN(ulong channelId, int n)
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