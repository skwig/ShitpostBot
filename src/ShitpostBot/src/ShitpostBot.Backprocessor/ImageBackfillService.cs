using MassTransit;
using Microsoft.EntityFrameworkCore;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Extensions;
using ShitpostBot.Infrastructure.Messages;

namespace ShitpostBot.Backprocessor;

public record ImageBackfillResult(int InsertedImages, bool Skipped);

public class ImageBackfillService(
    ILogger<ImageBackfillService> logger,
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IBus bus
)
{
    public virtual async Task<ImageBackfillResult> ProcessMessageAsync(
        HistoricalMessage message,
        CancellationToken cancellationToken = default
    )
    {
        if (message.IsBot)
        {
            logger.LogDebug("Skipping bot message {MessageId}", message.MessageId);
            return new ImageBackfillResult(0, true);
        }

        var insertedImages = 0;
        foreach (var attachment in message.Attachments)
        {
            var incomingAttachment = new Attachment(
                attachment.Id,
                attachment.Url,
                attachment.MediaType,
                attachment.Width,
                attachment.Height
            );

            if (
                (!incomingAttachment.IsImageOrVideo() && !HasKnownMediaExtension(attachment.Url))
                || attachment.Width < 300
                || attachment.Height < 300
            )
            {
                continue;
            }

            if (
                await dbContext.ImagePost.AnyAsync(
                    p => p.Image.ImageId == attachment.Id,
                    cancellationToken
                )
            )
            {
                logger.LogDebug(
                    "Skipping duplicate attachment {AttachmentId} on message {MessageId}",
                    attachment.Id,
                    message.MessageId
                );
                continue;
            }

            var trackedOn = DateTimeOffset.UtcNow;
            var image = Image.CreateOrDefault(
                attachment.Id,
                attachment.Url,
                attachment.MediaType,
                trackedOn
            );
            if (image == null)
            {
                continue;
            }

            var imagePost = ImagePost.Create(
                message.PostedOn,
                new ChatMessageIdentifier(message.GuildId, message.ChannelId, message.MessageId),
                new PosterIdentifier(message.AuthorId),
                trackedOn,
                image
            );

            dbContext.ImagePost.Add(imagePost);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await bus.Publish(
                new ImagePostTracked { ImagePostId = imagePost.Id, IsReevaluation = true },
                cancellationToken
            );
            insertedImages++;
        }

        return new ImageBackfillResult(insertedImages, insertedImages == 0);
    }

    private static bool HasKnownMediaExtension(Uri uri)
    {
        var extension = Path.GetExtension(uri.AbsolutePath);
        return extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".gif", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".webp", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".mp4", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".mov", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".webm", StringComparison.OrdinalIgnoreCase);
    }
}
