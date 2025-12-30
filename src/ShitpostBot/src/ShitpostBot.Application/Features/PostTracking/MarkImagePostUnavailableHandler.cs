using MediatR;
using ShitpostBot.Application.Features.BotCommands.Redacted;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Extensions;

namespace ShitpostBot.Application.Features.PostTracking;

internal class MarkImagePostUnavailableHandler(
    ILogger<MarkImagePostUnavailableHandler> logger,
    IDbContext dbContext,
    IUnitOfWork unitOfWork)
    : INotificationHandler<MessageDeleted>
{
    public async Task Handle(MessageDeleted notification, CancellationToken cancellationToken)
    {
        var imagePost = await dbContext.ImagePost.GetByChatMessageId(
            notification.Identification.MessageId, 
            cancellationToken);

        if (imagePost == null)
        {
            logger.LogDebug(
                "No ImagePost found for deleted message {MessageId}. Ignoring.", 
                notification.Identification.MessageId);
            return;
        }

        if (!imagePost.IsPostAvailable)
        {
            logger.LogDebug(
                "ImagePost {ImagePostId} for message {MessageId} is already unavailable. Ignoring.",
                imagePost.Id,
                notification.Identification.MessageId);
            return;
        }

        imagePost.MarkPostAsUnavailable();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Marked ImagePost {ImagePostId} as unavailable due to message {MessageId} deletion",
            imagePost.Id,
            notification.Identification.MessageId);
    }
}
