using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Messages;

namespace ShitpostBot.Worker
{
    public record LinkMessageCreated(LinkMessage LinkMessage) : INotification;
    
    internal class TrackLinkMessageHandler : INotificationHandler<LinkMessageCreated>
    {
        private readonly ILogger<TrackLinkMessageHandler> logger;

        private readonly IUnitOfWork unitOfWork;

        private readonly IDateTimeProvider dateTimeProvider;
        private readonly IMessageSession messageSession;

        public TrackLinkMessageHandler(ILogger<TrackLinkMessageHandler> logger,
            IUnitOfWork unitOfWork,
            IDateTimeProvider dateTimeProvider, IMessageSession messageSession)
        {
            this.logger = logger;
            this.unitOfWork = unitOfWork;
            this.dateTimeProvider = dateTimeProvider;
            this.messageSession = messageSession;
        }

        public async Task Handle(LinkMessageCreated notification, CancellationToken cancellationToken)
        {
            var utcNow = dateTimeProvider.UtcNow;

            var link = Link.CreateOrDefault(notification.LinkMessage.Embed.Uri);

            if (link == null)
            {
                logger.LogDebug($"Link '{notification.LinkMessage.Embed.Uri}' is not interesting. Not tracking.");
                return;
            }
            
            var newPost = new LinkPost(
                notification.LinkMessage.PostedOn,
                notification.LinkMessage.Identification.GuildId,
                notification.LinkMessage.Identification.ChannelId,
                notification.LinkMessage.Identification.MessageId,
                notification.LinkMessage.Identification.PosterId,
                utcNow,
                new LinkPostContent(link)
            );

            await unitOfWork.LinkPostsRepository.CreateAsync(newPost, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await messageSession.Publish(new LinkPostTracked {LinkPostId = newPost.Id});

            logger.LogDebug($"Tracked LinkPost {newPost}");
        }
    }
}