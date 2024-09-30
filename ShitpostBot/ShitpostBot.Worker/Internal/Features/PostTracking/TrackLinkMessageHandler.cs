using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Messages;

namespace ShitpostBot.Worker.Features.PostTracking;

public record LinkMessageCreated(LinkMessage LinkMessage) : INotification;

internal class TrackLinkMessageHandler(
    ILogger<TrackLinkMessageHandler> logger,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    IBus bus)
    : INotificationHandler<LinkMessageCreated>
{
    public async Task Handle(LinkMessageCreated notification, CancellationToken cancellationToken)
    {
        var utcNow = dateTimeProvider.UtcNow;

        var link = Link.CreateOrDefault(notification.LinkMessage.Embed.Uri);
        if (link == null)
        {
            logger.LogDebug("Link '{Uri}' is not interesting. Not tracking.", notification.LinkMessage.Embed.Uri);
            return;
        }

        var newPost = LinkPost.Create(
            notification.LinkMessage.PostedOn,
            new ChatMessageIdentifier(
                notification.LinkMessage.Identification.GuildId,
                notification.LinkMessage.Identification.ChannelId,
                notification.LinkMessage.Identification.MessageId
            ),
            new PosterIdentifier(
                notification.LinkMessage.Identification.PosterId
            ),
            utcNow,
            link
        );

        await unitOfWork.LinkPostsRepository.CreateAsync(newPost, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await bus.Publish(new LinkPostTracked { LinkPostId = newPost.Id }, cancellationToken: cancellationToken);

        logger.LogDebug("Tracked LinkPost {NewPost}", newPost);
    }
}