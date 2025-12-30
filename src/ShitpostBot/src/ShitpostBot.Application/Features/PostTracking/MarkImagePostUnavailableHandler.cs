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
        var imagePosts = await dbContext.ImagePost.GetByChatMessageId(
            notification.Identification.MessageId,
            cancellationToken);

        if (imagePosts.Count == 0)
        {
            logger.LogDebug(
                "No ImagePosts found for deleted message {MessageId}. Ignoring.",
                notification.Identification.MessageId);
            return;
        }

        foreach (var imagePost in imagePosts)
        {
            imagePost.MarkPostAsUnavailable();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Marked {Count} ImagePost(s) as unavailable due to message {MessageId} deletion",
            imagePosts.Count,
            notification.Identification.MessageId);
    }
}