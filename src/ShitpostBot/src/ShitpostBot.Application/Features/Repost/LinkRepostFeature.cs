using System.Text.RegularExpressions;
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
    private static readonly Regex UrlRegex = new(
        @"(?:http(s)?:\/\/)?[\w.-]+(?:\.[\w\.-]+)+[\w\-\._~:/?#[\]@!\$&'\(\)\*\+,;=.]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public async Task<bool> TryHandleCreate(IncomingMessage created, CancellationToken ct)
    {
        var utcNow = dateTimeProvider.UtcNow;
        var trackedAny = false;

        foreach (var embed in created.Embeds)
        {
            var link = Link.CreateOrDefault(embed.Url);
            if (link == null)
            {
                logger.LogDebug("Link '{Uri}' is not interesting. Not tracking.", embed.Url);
                continue;
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
            trackedAny = true;
        }

        if (!trackedAny && created.Content != null)
        {
            var regexMatches = UrlRegex.Matches(created.Content);
            foreach (Match regexMatch in regexMatches)
            {
                var link = Link.CreateOrDefault(new Uri(regexMatch.Value));
                if (link != null)
                {
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
                    trackedAny = true;
                }
            }
        }

        return trackedAny;
    }
}