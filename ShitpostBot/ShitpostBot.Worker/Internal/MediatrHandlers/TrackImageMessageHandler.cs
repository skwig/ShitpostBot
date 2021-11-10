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
    public record ImageMessageCreated(ImageMessage ImageMessage) : INotification;
    
    internal class TrackImageMessageHandler : INotificationHandler<ImageMessageCreated>
    {
        private readonly ILogger<TrackImageMessageHandler> logger;

        private readonly IUnitOfWork unitOfWork;

        private readonly IDateTimeProvider dateTimeProvider;
        private readonly IMessageSession messageSession;

        public TrackImageMessageHandler(ILogger<TrackImageMessageHandler> logger,
            IUnitOfWork unitOfWork,
            IDateTimeProvider dateTimeProvider, IMessageSession messageSession)
        {
            this.logger = logger;
            this.unitOfWork = unitOfWork;
            this.dateTimeProvider = dateTimeProvider;
            this.messageSession = messageSession;
        }

        public async Task Handle(ImageMessageCreated notification, CancellationToken cancellationToken)
        {
            var utcNow = dateTimeProvider.UtcNow;

            var newPost = new ImagePost(
                notification.ImageMessage.PostedOn,
                notification.ImageMessage.Identification.GuildId,
                notification.ImageMessage.Identification.ChannelId,
                notification.ImageMessage.Identification.MessageId,
                notification.ImageMessage.Identification.PosterId,
                utcNow,
                new ImagePostContent(new Image(notification.ImageMessage.Attachment.Id, notification.ImageMessage.Attachment.Uri, null))
            );

            await unitOfWork.ImagePostsRepository.CreateAsync(newPost, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await messageSession.Publish(new ImagePostTracked {ImagePostId = newPost.Id});

            logger.LogDebug($"Tracked ImagePost {newPost}");
        }
    }
}