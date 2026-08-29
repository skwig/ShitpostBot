using System.Collections.Concurrent;

namespace ShitpostBot.Application.Features.UpdatedMessages;

public class UpdatedMessageStore
{
    private const int MaxMessagesPerChannel = 50;

    private readonly ConcurrentDictionary<ulong, List<UpdatedMessage>> channels = new();

    public void Store(UpdatedMessage message)
    {
        var channelId = message.Id.ChannelId;
        var list = channels.GetOrAdd(channelId, _ => new List<UpdatedMessage>());

        lock (list)
        {
            list.Add(message);
            if (list.Count > MaxMessagesPerChannel)
            {
                list.RemoveAt(0);
            }
        }
    }

    public IReadOnlyList<UpdatedMessage> GetLastN(ulong channelId, int n)
    {
        if (!channels.TryGetValue(channelId, out var list))
        {
            return Array.Empty<UpdatedMessage>();
        }

        lock (list)
        {
            var count = Math.Min(n, list.Count);
            return list.Skip(list.Count - count).ToList().AsReadOnly();
        }
    }
}
