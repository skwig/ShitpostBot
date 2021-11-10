using NServiceBus;

namespace ShitpostBot.Infrastructure.Messages
{
    public class LinkPostTracked : IEvent
    {
        public long LinkPostId { get; init; }
    }
}