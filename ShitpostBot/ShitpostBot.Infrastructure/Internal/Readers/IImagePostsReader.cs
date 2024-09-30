using System;
using System.Linq;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using ShitpostBot.Domain;

namespace ShitpostBot.Infrastructure;

public interface IImagePostsReader : IReader<ImagePost>
{
    public IQueryable<ClosestToImagePost> ClosestWhitelistedToImagePostWithFeatureVector(DateTimeOffset postedOnBefore, Vector imagePostFeatureVector,
        OrderBy orderBy = OrderBy.CosineDistance);
    
    public IQueryable<ClosestToImagePost> ClosestToImagePostWithFeatureVector(DateTimeOffset postedOnBefore, Vector imagePostFeatureVector,
        OrderBy orderBy = OrderBy.CosineDistance);
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
    public double CosineSimilarity => Math.Round(1 - CosineDistance, 8);
}

public enum OrderBy
{
    L2Distance,
    CosineDistance
}

internal class ImagePostsReader(IDbContextFactory<ShitpostBotDbContext> contextFactory) : Reader<ImagePost>(contextFactory), IImagePostsReader
{
    public IQueryable<ClosestToImagePost> ClosestWhitelistedToImagePostWithFeatureVector(DateTimeOffset postedOnBefore, Vector imagePostFeatureVector,
        OrderBy orderBy = OrderBy.CosineDistance)
    {
        var context = ContextFactory.CreateDbContext();   
        return context.WhitelistedPost
            .Where(x => x.WhitelistedOn < postedOnBefore) // x.WhitelistedOn is implicitly larger (after) x.Post.PostedOn
            .OrderBy(x => orderBy == OrderBy.CosineDistance
                ? x.Post.Image.ImageFeatures!.FeatureVector.CosineDistance(imagePostFeatureVector)
                : x.Post.Image.ImageFeatures!.FeatureVector.L2Distance(imagePostFeatureVector)
            )
            .ThenBy(x => x.Post.PostedOn)
            .Select(x => new ClosestToImagePost(
                x.Id,
                x.Post.PostedOn,
                new ChatMessageIdentifier(
                    x.Post.ChatGuildId,
                    x.Post.ChatChannelId,
                    x.Post.ChatMessageId
                ),
                new PosterIdentifier(
                    x.Post.PosterId
                ),
                x.Post.Image.ImageFeatures!.FeatureVector.L2Distance(imagePostFeatureVector),
                x.Post.Image.ImageFeatures!.FeatureVector.CosineDistance(imagePostFeatureVector)
            ));
    }

    public IQueryable<ClosestToImagePost> ClosestToImagePostWithFeatureVector(DateTimeOffset postedOnBefore, Vector imagePostFeatureVector,
        OrderBy orderBy = OrderBy.CosineDistance)
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