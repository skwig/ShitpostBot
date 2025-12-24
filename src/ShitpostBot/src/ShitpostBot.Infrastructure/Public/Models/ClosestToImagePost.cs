using ShitpostBot.Domain;

namespace ShitpostBot.Infrastructure;

public record ClosestToImagePost(
    long ImagePostId,
    DateTimeOffset PostedOn,
    ChatMessageIdentifier ChatMessageIdentifier,
    PosterIdentifier PosterIdentifier,
    double L2Distance,
    double CosineDistance,
    Uri ImageUri)
{
    public double CosineSimilarity => Math.Round(1 - CosineDistance, 8);
}
