using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using ShitpostBot.Domain;

namespace ShitpostBot.Infrastructure.Extensions;

public static class ImagePostQueryExtensions
{
    extension(IQueryable<ImagePost> query)
    {
        public Task<ImagePost?> GetById(long id,
            CancellationToken cancellationToken = default)
        {
            return query
                .Where(ip => ip.IsPostAvailable)
                .Where(ip => ip.Id == id)
                .SingleOrDefaultAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<ImagePost>> GetByChatMessageId(ulong chatMessageId,
            CancellationToken cancellationToken = default)
        {
            return await query
                .Where(ip => ip.ChatMessageId == chatMessageId)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<ImagePost>> GetHistory(DateTimeOffset postedAtFromInclusive,
            DateTimeOffset postedAtToExclusive,
            CancellationToken cancellationToken = default)
        {
            return await query
                .Where(x => x.IsPostAvailable)
                .Where(x => postedAtFromInclusive <= x.PostedOn && x.PostedOn < postedAtToExclusive)
                .ToListAsync(cancellationToken);
        }

        public IQueryable<ClosestToImagePost> ImagePostsWithClosestFeatureVector(
            DateTimeOffset postedOnBefore,
            Vector imageFeatureVector,
            OrderBy orderBy = OrderBy.CosineDistance)
        {
            return query
                .Where(x => x.PostedOn < postedOnBefore)
                .ImagePostsWithClosestFeatureVector(imageFeatureVector, orderBy);
        }

        public IQueryable<ClosestToImagePost> ImagePostsWithClosestFeatureVector(
            Vector imageFeatureVector,
            OrderBy orderBy = OrderBy.CosineDistance)
        {
            return query
                .Where(x => x.IsPostAvailable)
                .Where(x => x.Image.ImageFeatures != null)
                .OrderBy(x => orderBy == OrderBy.CosineDistance
                    ? x.Image.ImageFeatures!.FeatureVector.CosineDistance(imageFeatureVector)
                    : x.Image.ImageFeatures!.FeatureVector.L2Distance(imageFeatureVector))
                .ThenBy(x => x.PostedOn)
                .Select(x => new ClosestToImagePost(
                    x.Id,
                    x.PostedOn,
                    new ChatMessageIdentifier(x.ChatGuildId, x.ChatChannelId, x.ChatMessageId),
                    new PosterIdentifier(x.PosterId),
                    x.Image.ImageFeatures!.FeatureVector.L2Distance(imageFeatureVector),
                    x.Image.ImageFeatures!.FeatureVector.CosineDistance(imageFeatureVector),
                    x.Image.ImageUri));
        }
    }
}