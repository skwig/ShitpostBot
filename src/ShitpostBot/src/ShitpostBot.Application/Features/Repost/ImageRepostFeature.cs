using MediatR;
using ShitpostBot.Application.Features.PostTracking;
using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.Features.Repost;

public class ImageRepostFeature(IMediator mediator) : IMessageFeature
{
    public async Task<bool> TryHandleCreate(IncomingMessage created, CancellationToken ct)
    {
        var imageAttachment = created.Attachments.FirstOrDefault(a =>
            a.MediaType != null && a.MediaType.StartsWith("image/"));

        if (imageAttachment == null)
        {
            return false;
        }

        var imageMessage = new ImageMessage(
            created.Id,
            new ImageMessageAttachment(imageAttachment.Id, imageAttachment.Url.Segments.Last(), imageAttachment.Url, imageAttachment.MediaType),
            created.PostedOn
        );
        await mediator.Publish(new ImageMessageCreated(imageMessage), ct);
        return true;
    }

    public async Task<bool> TryHandleDelete(MessageIdentification deleted, CancellationToken ct)
    {
        await mediator.Publish(new MessageDeleted(deleted), ct);
        return true;
    }
}