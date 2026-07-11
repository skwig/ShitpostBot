using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CSharpFunctionalExtensions;
using Pgvector;

namespace ShitpostBot.Domain;

public class ImageFeatures : ComparableValueObject
{
    public string ModelName { get; private set; }
    public Vector FeatureVector { get; private set; }

    private ImageFeatures()
    {
        // For EF
        ModelName = null!;
        FeatureVector = null!;
    }

    public ImageFeatures(string modelName, Vector featureVector)
    {
        ModelName = modelName;
        FeatureVector = featureVector;
    }

    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return ModelName;
        foreach (var feature in FeatureVector.ToArray())
        {
            yield return feature;
        }
    }
}