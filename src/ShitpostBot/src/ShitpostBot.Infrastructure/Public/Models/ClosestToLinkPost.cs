using ShitpostBot.Domain;

namespace ShitpostBot.Infrastructure;

public record ClosestToLinkPost(
    long LinkPostId,
    DateTimeOffset PostedOn,
    ChatMessageIdentifier ChatMessageIdentifier,
    PosterIdentifier PosterIdentifier,
    double Distance)
{
    public double Similarity => 1 - Distance;
}
