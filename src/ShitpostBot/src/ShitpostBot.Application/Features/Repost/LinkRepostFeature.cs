using MediatR;
using ShitpostBot.Application.Features.PostTracking;
using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.Features.Repost;

public class LinkRepostFeature(IMediator mediator) : IMessageFeature
{
    public async Task<bool> TryHandleCreate(IncomingMessage created, CancellationToken ct)
    {
        var linkEmbed = created.Embeds.FirstOrDefault();

        if (linkEmbed == null)
        {
            return false;
        }

        var linkMessage = new LinkMessage(
            created.Id,
            new LinkMessageEmbed(linkEmbed.Url),
            created.PostedOn
        );
        await mediator.Publish(new LinkMessageCreated(linkMessage), ct);
        return true;
    }

    public async Task<bool> TryHandleDelete(MessageIdentification deleted, CancellationToken ct)
    {
        await mediator.Publish(new MessageDeleted(deleted), ct);
        return true;
    }
}