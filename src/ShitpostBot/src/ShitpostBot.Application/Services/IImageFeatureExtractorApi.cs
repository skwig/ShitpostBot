using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Refit;

namespace ShitpostBot.Application.Services;

public interface IImageFeatureExtractorApi
{
    [Get("/images/features")]
    Task<ExtractImageFeaturesResponse> ExtractImageFeaturesAsync([AliasAs("image_url")] string imageUrl);
}

public record ExtractImageFeaturesResponse
{
    [JsonPropertyName("image_url")] public required string ImageUrl { get; init; }
    [JsonPropertyName("image_features")] public required float[] ImageFeatures { get; init; }
}

public class ImageFeatureExtractorApiOptions
{
    [Required] public required string Uri { get; init; }
}
