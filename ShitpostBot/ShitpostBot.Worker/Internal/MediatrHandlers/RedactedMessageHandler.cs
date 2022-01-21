using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Worker
{
    public record TextMessageCreated(TextMessage TextMessage) : INotification;

    public record MessageDeleted(MessageIdentification Identification) : INotification;

    internal class RedactedMessageHandler :
        INotificationHandler<TextMessageCreated>,
        INotificationHandler<MessageDeleted>
    {
        private readonly ILogger<RedactedMessageHandler> logger;
        private readonly IChatClient chatClient;
        private readonly IMemoryCache memoryCache;

        public RedactedMessageHandler(ILogger<RedactedMessageHandler> logger, IChatClient chatClient, IMemoryCache memoryCache)
        {
            this.logger = logger;
            this.chatClient = chatClient;
            this.memoryCache = memoryCache;
        }

        public async Task Handle(TextMessageCreated notification, CancellationToken cancellationToken)
        {
            if (notification.TextMessage.Content == null)
            {
                return;
            }
            
            memoryCache.Set($"TextMessage_{notification.TextMessage.Identification}", notification.TextMessage, TimeSpan.FromMinutes(30));
        }

        public async Task Handle(MessageDeleted notification, CancellationToken cancellationToken)
        {
            // var textMessage = memoryCache.Get<TextMessage>($"TextMessage_{notification.Identification}");
            // if (textMessage?.Content == null)
            // {
            //     return;
            // }
            //
            // var utcNow = DateTimeOffset.UtcNow;
            //
            // var posterMention = chatClient.Utils.Mention(textMessage.Identification.PosterId);
            // var roundedSecondsAgo = (decimal)Math.Round((utcNow - textMessage.PostedOn).TotalSeconds, 2);
            //
            // await chatClient.SendMessage(
            //     new MessageDestination(textMessage.Identification.GuildId, textMessage.Identification.ChannelId, textMessage.Identification.MessageId),
            //     $"{posterMention} [redacted] a message from {roundedSecondsAgo} seconds ago\n" +
            //     $"> {textMessage.Content}"
            // );
        }
    }
}