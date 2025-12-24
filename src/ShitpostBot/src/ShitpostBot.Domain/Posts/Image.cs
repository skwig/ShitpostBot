using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using CSharpFunctionalExtensions;

namespace ShitpostBot.Domain;

public class Image : ComparableValueObject
{
    public ulong ImageId { get; init; }
    public Uri ImageUri { get; init; }
    public string? MediaType { get; init; }
    public ImageFeatures? ImageFeatures { get; init; }

    private Image()
    {
        // For EF
        ImageUri = null!;
    }

    internal Image(ulong imageId, Uri imageUri, string? mediaType, ImageFeatures? imageFeatures)
    {
        ImageId = imageId;
        ImageUri = imageUri;
        MediaType = mediaType;
        ImageFeatures = imageFeatures;
    }

    [Pure]
    public Image WithImageFeatures(ImageFeatures? imageFeatures)
    {
        return new Image(ImageId, ImageUri, MediaType, imageFeatures);
    }

    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return ImageId;
        yield return ImageUri.ToString();
        if (ImageFeatures != null) yield return ImageFeatures;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="imageId"></param>
    /// <param name="imageUri"></param>
    /// <param name="mediaType"></param>
    /// <returns></returns>
    public static Image? CreateOrDefault(ulong imageId, Uri imageUri, string? mediaType)
    {
        return new Image(imageId, imageUri, mediaType, null);
    }
}