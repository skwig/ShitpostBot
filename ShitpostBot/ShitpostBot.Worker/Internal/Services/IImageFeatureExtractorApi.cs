using System.Threading.Tasks;
using Newtonsoft.Json;
using Refit;

namespace ShitpostBot.Worker
{
    internal interface IImageFeatureExtractorApi
    {
        [Get("/images/features")]
        Task<ExtractImageFeaturesResponse> ExtractImageFeaturesAsync([AliasAs("image_url")] string imageUrl);
    }

    internal class ExtractImageFeaturesResponse
    {
        [JsonProperty("image_url")] public string ImageUrl { get; set; }
        [JsonProperty("image_features")] public double[] ImageFeatures { get; set; }
    }

    internal class ImageFeatureExtractorApiOptions
    {
        public string Uri { get; set; }
    }
}