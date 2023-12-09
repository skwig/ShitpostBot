using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CSharpFunctionalExtensions;

namespace ShitpostBot.Domain;

public class ImageFeatures : ValueObject
{
    public Vector FeatureVector { get; private set; }

    private ImageFeatures()
    {
        // For EF
    }

    public ImageFeatures(Vector featureVector)
    {
        FeatureVector = featureVector;
    }

    protected override IEnumerable<IComparable> GetEqualityComponents()
    {
        foreach (var feature in FeatureVector.ToArray())
        {
            yield return feature;
        }
    }
}

public class Vector : IEquatable<Vector>
{
    public ReadOnlyMemory<float> Memory { get; }

    public Vector(ReadOnlyMemory<float> v)
        => Memory = v;

    public Vector(string s)
        => Memory = Array.ConvertAll(s.Substring(1, s.Length - 2).Split(','), v => float.Parse(v, CultureInfo.InvariantCulture));

    public override string ToString()
        => string.Concat("[", string.Join(",", Memory.ToArray().Select(v => v.ToString(CultureInfo.InvariantCulture))), "]");

    public float[] ToArray()
        => Memory.ToArray();

    public bool Equals(Vector? other)
        => other is not null && Memory.Span.SequenceEqual(other.Memory.Span);

    public override bool Equals(object? obj)
        => obj is Vector vector && Equals(vector);

    public static bool operator ==(Vector? x, Vector? y)
        => (x is null && y is null) || (x is not null && x.Equals(y));

    public static bool operator !=(Vector? x, Vector? y) => !(x == y);

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        var span = Memory.Span;

        for (var i = 0; i < span.Length; i++)
            hashCode.Add(span[i]);

        return hashCode.ToHashCode();
    }
}