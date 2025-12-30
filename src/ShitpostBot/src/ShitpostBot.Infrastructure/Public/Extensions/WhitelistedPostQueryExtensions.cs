using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using ShitpostBot.Domain;

namespace ShitpostBot.Infrastructure.Extensions;

public static class WhitelistedPostQueryExtensions
{
    extension(IQueryable<WhitelistedPost> query)
    {
        public Task<WhitelistedPost?> GetByPostId(
            long postId,
            CancellationToken cancellationToken = default)
        {
            return query
                .Where(wp => wp.Post.IsPostAvailable)
                .Where(wp => wp.Post.Id == postId)
                .SingleOrDefaultAsync(cancellationToken);
        }

        public IQueryable<ClosestToImagePost> ClosestWhitelistedToImagePostWithFeatureVector(
            DateTimeOffset postedOnBefore,
            Vector imagePostFeatureVector,
            OrderBy orderBy = OrderBy.CosineDistance)
        {
            return query
                .Where(x => x.Post.IsPostAvailable)
                .Where(x => x.WhitelistedOn < postedOnBefore)
                .OrderBy(x => orderBy == OrderBy.CosineDistance
                    ? x.Post.Image.ImageFeatures!.FeatureVector.CosineDistance(imagePostFeatureVector)
                    : x.Post.Image.ImageFeatures!.FeatureVector.L2Distance(imagePostFeatureVector))
                .ThenBy(x => x.Post.PostedOn)
                .Select(x => new ClosestToImagePost(
                    x.Id,
                    x.Post.PostedOn,
                    new ChatMessageIdentifier(x.Post.ChatGuildId, x.Post.ChatChannelId, x.Post.ChatMessageId),
                    new PosterIdentifier(x.Post.PosterId),
                    x.Post.Image.ImageFeatures!.FeatureVector.L2Distance(imagePostFeatureVector),
                    x.Post.Image.ImageFeatures!.FeatureVector.CosineDistance(imagePostFeatureVector),
                    x.Post.Image.ImageUri));
        }
    }
}