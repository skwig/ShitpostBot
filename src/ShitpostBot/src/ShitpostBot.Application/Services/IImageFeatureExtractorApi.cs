using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Refit;

namespace ShitpostBot.Application.Services;

public interface IImageFeatureExtractorApi
{
    [Post("/process/image")]
    Task<ProcessImageResponse> ProcessImageAsync([Body] ProcessImageRequest request);
}

public record ProcessImageRequest
{
    [JsonPropertyName("image_url")] 
    public required string ImageUrl { get; init; }
    
    [JsonPropertyName("embedding")] 
    public bool Embedding { get; init; } = true;
    
    [JsonPropertyName("caption")] 
    public bool Caption { get; init; } = false;
    
    [JsonPropertyName("ocr")] 
    public bool Ocr { get; init; } = false;
    
    [JsonPropertyName("use_tesseract")] 
    public bool UseTesseract { get; init; } = false;
}

public record ProcessImageResponse
{
    [JsonPropertyName("size")] 
    public int[]? Size { get; init; }
    
    [JsonPropertyName("embedding")] 
    public float[]? Embedding { get; init; }
    
    [JsonPropertyName("caption")] 
    public string? Caption { get; init; }
    
    [JsonPropertyName("ocr")] 
    public string? Ocr { get; init; }
    
    [JsonPropertyName("ocr_confidence")] 
    public float? OcrConfidence { get; init; }
    
    [JsonPropertyName("ocr_engine")] 
    public string? OcrEngine { get; init; }
}

public class ImageFeatureExtractorApiOptions
{
    [Required] public required string Uri { get; init; }
}
