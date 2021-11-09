using NServiceBus;

namespace ShitpostBot.Infrastructure.Messages
{
    public class ImagePostTracked : IEvent
    {
        public long ImagePostId { get; init; }
    }
}