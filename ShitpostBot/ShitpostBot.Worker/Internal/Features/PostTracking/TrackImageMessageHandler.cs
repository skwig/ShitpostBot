using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Messages;

namespace ShitpostBot.Worker;

public record ImageMessageCreated(ImageMessage ImageMessage) : INotification;

internal class TrackImageMessageHandler(
    ILogger<TrackImageMessageHandler> logger,
    IUnitOfWork ofWork,
    IDateTimeProvider dateTimeProvider,
    IMessageSession session)
    : INotificationHandler<ImageMessageCreated>
{
    public async Task Handle(ImageMessageCreated notification, CancellationToken cancellationToken)
    {
        var utcNow = dateTimeProvider.UtcNow;

        var image = Image.CreateOrDefault(notification.ImageMessage.Attachment.Id, notification.ImageMessage.Attachment.Uri);
        if (image == null)
        {
            logger.LogDebug("Image '{Uri}' is not interesting. Not tracking.", notification.ImageMessage.Attachment.Uri);
            return;
        }

        var newPost = ImagePost.Create(
            notification.ImageMessage.PostedOn,
            new ChatMessageIdentifier(
                notification.ImageMessage.Identification.GuildId,
                notification.ImageMessage.Identification.ChannelId,
                notification.ImageMessage.Identification.MessageId
            ),
            new PosterIdentifier(
                notification.ImageMessage.Identification.PosterId
            ),
            utcNow,
            image
        );

        await ofWork.ImagePostsRepository.CreateAsync(newPost, cancellationToken);
        await ofWork.SaveChangesAsync(cancellationToken);
        await session.Publish(new ImagePostTracked { ImagePostId = newPost.Id }, cancellationToken: cancellationToken);

        logger.LogDebug("Tracked ImagePost {NewPost}", newPost);
    }
}