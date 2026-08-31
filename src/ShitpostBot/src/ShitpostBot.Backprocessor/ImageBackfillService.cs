using MassTransit;
using Microsoft.EntityFrameworkCore;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Extensions;
using ShitpostBot.Infrastructure.Messages;

namespace ShitpostBot.Backprocessor;

public record ImageBackfillResult(int InsertedImages, bool Skipped);

public class ImageBackfillService(IDbContext dbContext, IUnitOfWork unitOfWork, IBus bus)
{
    public virtual async Task<ImageBackfillResult> ProcessMessageAsync(
        HistoricalMessage message,
        CancellationToken cancellationToken = default
    )
    {
        if (message.IsBot)
        {
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
                !incomingAttachment.IsImageOrVideo()
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
}
