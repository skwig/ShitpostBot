using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Refit;

namespace ShitpostBot.Worker;

internal interface IImageFeatureExtractorApi
{
    [Get("/images/features")]
    Task<ExtractImageFeaturesResponse> ExtractImageFeaturesAsync([AliasAs("image_url")] string imageUrl);
}

internal record ExtractImageFeaturesResponse
{
    [JsonProperty("image_url")] public required string ImageUrl { get; init; }
    [JsonProperty("image_features")] public required float[] ImageFeatures { get; init; }
}

internal class ImageFeatureExtractorApiOptions
{
    [Required] public required string Uri { get; init; }
}