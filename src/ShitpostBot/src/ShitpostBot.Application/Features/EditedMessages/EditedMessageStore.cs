using System.Collections.Concurrent;

namespace ShitpostBot.Application.Features.EditedMessages;

public class EditedMessageStore
{
    private const int MaxMessagesPerChannel = 50;

    private readonly ConcurrentDictionary<ulong, List<EditedMessage>> channels = new();

    public void Store(EditedMessage message)
    {
        var channelId = message.Id.ChannelId;
        var list = channels.GetOrAdd(channelId, _ => new List<EditedMessage>());

        lock (list)
        {
            list.Add(message);
            if (list.Count > MaxMessagesPerChannel)
            {
                list.RemoveAt(0);
            }
        }
    }

    public IReadOnlyList<EditedMessage> GetLastN(ulong channelId, int n)
    {
        if (!channels.TryGetValue(channelId, out var list))
        {
            return Array.Empty<EditedMessage>();
        }

        lock (list)
        {
            var count = Math.Min(n, list.Count);
            return list.Skip(list.Count - count).ToList().AsReadOnly();
        }
    }
}
