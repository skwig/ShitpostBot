using MassTransit;
using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Messages;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.Repost;

public class LinkRepostFeature(
    ILogger<LinkRepostFeature> logger,
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    IBus bus)
    : IMessageFeature
{
    public async Task<bool> TryHandleCreate(IncomingMessage created, CancellationToken ct)
    {
        var linkEmbed = created.Embeds.FirstOrDefault();

        if (linkEmbed == null)
        {
            return false;
        }

        var utcNow = dateTimeProvider.UtcNow;

        var link = Link.CreateOrDefault(linkEmbed.Url);
        if (link == null)
        {
            logger.LogDebug("Link '{Uri}' is not interesting. Not tracking.", linkEmbed.Url);
            return false;
        }

        var newPost = LinkPost.Create(
            created.PostedOn,
            new ChatMessageIdentifier(
                created.Id.GuildId,
                created.Id.ChannelId,
                created.Id.MessageId
            ),
            new PosterIdentifier(created.Id.PosterId),
            utcNow,
            link
        );

        dbContext.LinkPost.Add(newPost);
        await unitOfWork.SaveChangesAsync(ct);
        await bus.Publish(new LinkPostTracked { LinkPostId = newPost.Id }, cancellationToken: ct);

        logger.LogDebug("Tracked LinkPost {NewPost}", newPost);

        return true;
    }
}