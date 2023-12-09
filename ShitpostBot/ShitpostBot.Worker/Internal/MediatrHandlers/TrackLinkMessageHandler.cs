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

public record LinkMessageCreated(LinkMessage LinkMessage) : INotification;

internal class TrackLinkMessageHandler(
    ILogger<TrackLinkMessageHandler> logger,
    IUnitOfWork ofWork,
    IDateTimeProvider dateTimeProvider,
    IMessageSession session)
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

        await ofWork.LinkPostsRepository.CreateAsync(newPost, cancellationToken);
        await ofWork.SaveChangesAsync(cancellationToken);
        await session.Publish(new LinkPostTracked { LinkPostId = newPost.Id }, cancellationToken: cancellationToken);

        logger.LogDebug("Tracked LinkPost {NewPost}", newPost);
    }
}