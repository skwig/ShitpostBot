using System;
using System.Linq;
using System.Threading;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure.PgVector;

namespace ShitpostBot.Infrastructure;

public interface IImagePostsReader : IReader<ImagePost>
{
    public IQueryable<ClosestToImagePost> ClosestToImagePostWithFeatureVector(DateTimeOffset postedOnBefore, Vector imagePostFeatureVector,
        OrderBy orderBy = OrderBy.CosineDistance, CancellationToken cancellationToken = default);
}

public record ClosestToImagePost(
    long ImagePostId,
    DateTimeOffset PostedOn,
    ChatMessageIdentifier ChatMessageIdentifier,
    PosterIdentifier PosterIdentifier,
    double L2Distance,
    double CosineDistance
)
{
    public double CosineSimilarity => 1 - CosineDistance;
}

public enum OrderBy
{
    L2Distance,
    CosineDistance
}

internal class ImagePostsReader : Reader<ImagePost>, IImagePostsReader
{
    public ImagePostsReader(IDbContextFactory<ShitpostBotDbContext> contextFactory) : base(contextFactory)
    {
    }

    public IQueryable<ClosestToImagePost> ClosestToImagePostWithFeatureVector(DateTimeOffset postedOnBefore, Vector imagePostFeatureVector,
        OrderBy orderBy = OrderBy.CosineDistance,
        CancellationToken cancellationToken = default)
    {
        return All()
            .Where(x => x.PostedOn < postedOnBefore)
            .OrderBy(x => orderBy == OrderBy.CosineDistance
                ? x.Image.ImageFeatures!.FeatureVector.CosineDistance(imagePostFeatureVector)
                : x.Image.ImageFeatures!.FeatureVector.L2Distance(imagePostFeatureVector)
            )
            .ThenBy(x => x.PostedOn)
            .Select(x => new ClosestToImagePost(
                x.Id,
                x.PostedOn,
                new ChatMessageIdentifier(
                    x.ChatGuildId,
                    x.ChatChannelId,
                    x.ChatMessageId
                ),
                new PosterIdentifier(
                    x.PosterId
                ),
                x.Image.ImageFeatures!.FeatureVector.L2Distance(imagePostFeatureVector),
                x.Image.ImageFeatures!.FeatureVector.CosineDistance(imagePostFeatureVector)
            ));
    }
}