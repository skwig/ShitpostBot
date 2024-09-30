using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Messages;

namespace ShitpostBot.Worker.Features.PostTracking;

public record ImageMessageCreated(ImageMessage ImageMessage) : INotification;

internal class TrackImageMessageHandler(
    ILogger<TrackImageMessageHandler> logger,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    IBus bus)
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

        await unitOfWork.ImagePostsRepository.CreateAsync(newPost, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await bus.Publish(new ImagePostTracked { ImagePostId = newPost.Id }, cancellationToken: cancellationToken);

        logger.LogDebug("Tracked ImagePost {NewPost}", newPost);
    }
}