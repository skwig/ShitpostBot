using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using ShitpostBot.Domain;

namespace ShitpostBot.Infrastructure;

public static class ImagePostQueryExtensions
{
    extension(IQueryable<ImagePost> query)
    {
        public Task<ImagePost?> GetById(long id,
            CancellationToken cancellationToken = default)
        {
            return query.SingleOrDefaultAsync(ip => ip.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<ImagePost>> GetHistory(DateTimeOffset postedAtFromInclusive,
            DateTimeOffset postedAtToExclusive,
            CancellationToken cancellationToken = default)
        {
            return await query
                .Where(x => postedAtFromInclusive <= x.PostedOn && x.PostedOn < postedAtToExclusive)
                .ToListAsync(cancellationToken);
        }

        public IQueryable<ClosestToImagePost> ClosestToImagePostWithFeatureVector(DateTimeOffset postedOnBefore,
            Vector imagePostFeatureVector,
            OrderBy orderBy = OrderBy.CosineDistance)
        {
            return query
                .Where(x => x.PostedOn < postedOnBefore)
                .OrderBy(x => orderBy == OrderBy.CosineDistance
                    ? x.Image.ImageFeatures!.FeatureVector.CosineDistance(imagePostFeatureVector)
                    : x.Image.ImageFeatures!.FeatureVector.L2Distance(imagePostFeatureVector))
                .ThenBy(x => x.PostedOn)
                .Select(x => new ClosestToImagePost(
                    x.Id,
                    x.PostedOn,
                    new ChatMessageIdentifier(x.ChatGuildId, x.ChatChannelId, x.ChatMessageId),
                    new PosterIdentifier(x.PosterId),
                    x.Image.ImageFeatures!.FeatureVector.L2Distance(imagePostFeatureVector),
                    x.Image.ImageFeatures!.FeatureVector.CosineDistance(imagePostFeatureVector)));
        }
    }
}
