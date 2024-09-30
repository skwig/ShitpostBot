using System;
using System.Collections.Generic;
using CSharpFunctionalExtensions;
using Pgvector;

namespace ShitpostBot.Domain;

public class ImageFeatures : ComparableValueObject
{
    public Vector FeatureVector { get; private set; }

    private ImageFeatures()
    {
        // For EF
        FeatureVector = null!;
    }

    public ImageFeatures(Vector featureVector)
    {
        FeatureVector = featureVector;
    }

    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        foreach (var feature in FeatureVector.ToArray())
        {
            yield return feature;
        }
    }
}