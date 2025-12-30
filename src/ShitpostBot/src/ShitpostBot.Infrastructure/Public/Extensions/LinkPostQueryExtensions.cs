using Microsoft.EntityFrameworkCore;
using ShitpostBot.Domain;

namespace ShitpostBot.Infrastructure.Extensions;

public static class LinkPostQueryExtensions
{
    extension(IQueryable<LinkPost> query)
    {
        public Task<LinkPost?> GetById(long id,
            CancellationToken cancellationToken = default)
        {
            return query.SingleOrDefaultAsync(lp => lp.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<LinkPost>> GetHistory(DateTimeOffset postedAtFromInclusive,
            DateTimeOffset postedAtToExclusive,
            CancellationToken cancellationToken = default)
        {
            return await query
                .Where(x => postedAtFromInclusive <= x.PostedOn && x.PostedOn < postedAtToExclusive)
                .ToListAsync(cancellationToken);
        }

        public IQueryable<ClosestToLinkPost> ClosestToLinkPostWithUri(DateTimeOffset postedOnBefore,
            LinkProvider linkProvider,
            Uri linkUri)
        {
            return query
                .Where(x => x.PostedOn < postedOnBefore)
                .Where(x => x.Link.LinkProvider == linkProvider)
                .Where(x => x.Link.LinkUri == linkUri)
                .OrderBy(x => x.PostedOn)
                .Select(x => new ClosestToLinkPost(
                    x.Id,
                    x.PostedOn,
                    new ChatMessageIdentifier(x.ChatGuildId, x.ChatChannelId, x.ChatMessageId),
                    new PosterIdentifier(x.PosterId),
                    0));
        }
    }
}
