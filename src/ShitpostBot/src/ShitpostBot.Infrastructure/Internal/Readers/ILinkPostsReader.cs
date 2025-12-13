using System;
using System.Linq;
using System.Threading;
using ShitpostBot.Domain;

namespace ShitpostBot.Infrastructure;

public interface ILinkPostsReader : IReader<LinkPost>
{
    public IQueryable<ClosestToLinkPost> ClosestToLinkPostWithUri(DateTimeOffset postedOnBefore, LinkProvider linkProvider, Uri linkUri);
}

public record ClosestToLinkPost(
    long LinkPostId,
    DateTimeOffset PostedOn,
    ChatMessageIdentifier ChatMessageIdentifier,
    PosterIdentifier PosterIdentifier,
    double Distance
)
{
    public double Similarity => 1 - Distance;
}

internal class LinkPostsReader(IDbContextFactory<ShitpostBotDbContext> contextFactory) : Reader<LinkPost>(contextFactory), ILinkPostsReader
{
    public IQueryable<ClosestToLinkPost> ClosestToLinkPostWithUri(DateTimeOffset postedOnBefore, LinkProvider linkProvider, Uri linkUri)
    {
        return All()
            .Where(x => x.PostedOn < postedOnBefore)
            .Where(x => x.Link.LinkProvider == linkProvider)
            .Where(x => x.Link.LinkUri == linkUri)
            .OrderBy(x => x.PostedOn)
            .Select(x => new ClosestToLinkPost(
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
                0
            ));
    }
}