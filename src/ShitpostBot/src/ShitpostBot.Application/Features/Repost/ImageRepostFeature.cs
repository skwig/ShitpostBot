using MassTransit;
using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Extensions;
using ShitpostBot.Infrastructure.Messages;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.Repost;

public class ImageRepostFeature(
    ILogger<ImageRepostFeature> logger,
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    IBus bus)
    : IMessageFeature
{
    public async Task<bool> TryHandleCreate(IncomingMessage created, CancellationToken ct)
    {
        var imageAttachment = created.Attachments.FirstOrDefault(a =>
            a.MediaType != null
            && a.MediaType.StartsWith("image/")
            && (a.Width == null || a.Width >= 299)
            && (a.Height == null || a.Height >= 299));

        if (imageAttachment == null)
        {
            return false;
        }

        var utcNow = dateTimeProvider.UtcNow;

        var image = Image.CreateOrDefault(
            imageAttachment.Id,
            imageAttachment.Url,
            imageAttachment.MediaType,
            utcNow
        );

        if (image == null)
        {
            logger.LogDebug("Image '{Uri}' is not interesting. Not tracking.", imageAttachment.Url);
            return false;
        }

        var newPost = ImagePost.Create(
            created.PostedOn,
            new ChatMessageIdentifier(
                created.Id.GuildId,
                created.Id.ChannelId,
                created.Id.MessageId
            ),
            new PosterIdentifier(created.Id.PosterId),
            utcNow,
            image
        );

        dbContext.ImagePost.Add(newPost);
        await unitOfWork.SaveChangesAsync(ct);
        await bus.Publish(new ImagePostTracked { ImagePostId = newPost.Id }, cancellationToken: ct);

        logger.LogDebug("Tracked ImagePost {NewPost}", newPost);

        return true;
    }

    public async Task<bool> TryHandleDelete(MessageIdentification deleted, CancellationToken ct)
    {
        var imagePosts = await dbContext.ImagePost.GetByChatMessageId(deleted.MessageId, ct);

        if (imagePosts.Count == 0)
        {
            logger.LogDebug("No ImagePosts found for deleted message {MessageId}. Ignoring.", deleted.MessageId);
            return false;
        }

        foreach (var imagePost in imagePosts)
        {
            imagePost.MarkPostAsUnavailable();
        }

        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Marked {Count} ImagePost(s) as unavailable due to message {MessageId} deletion",
            imagePosts.Count, deleted.MessageId);

        return true;
    }
}