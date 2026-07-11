using System.ComponentModel.DataAnnotations;

namespace ShitpostBot.Infrastructure.Services;

public class ImageFeatureExtractorApiOptions
{
    [Required]
    public required string Uri { get; init; }
}