using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ShitpostBot.Domain
{
    public class ImageFeatures
    {
        public IReadOnlyList<double> FeatureVector { get; private set; }

        private ImageFeatures()
        {
        }

        public ImageFeatures(IReadOnlyList<double> featureVector)
        {
            if (featureVector == null || featureVector.Count == 0)
            {
                throw new ArgumentException("Foo");
            }

            this.FeatureVector = featureVector;
        }

        public double GetSimilarityTo(ImageFeatures other) => CosineSimilarity(FeatureVector, other.FeatureVector);

        private static double CosineSimilarity(IReadOnlyList<double> x, IReadOnlyList<double> y)
        {
            if (x.Count != y.Count)
            {
                throw new ArgumentException($"Lengths of '{nameof(x)}' and '{nameof(y)}' do not match");
            }

            var num1 = 0.0;
            var d1 = 0.0;
            var d2 = 0.0;
            for (var index = 0; index < x.Count; ++index)
            {
                num1 += x[index] * y[index];
                d1 += x[index] * x[index];
                d2 += y[index] * y[index];
            }

            var num2 = Math.Sqrt(d1) * Math.Sqrt(d2);
            return num1 / num2;
        }
    }
}