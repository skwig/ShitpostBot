using System.Collections.Generic;

namespace ShitpostBot.Domain
{
    public sealed record LinkPostStatistics : ValueObject<LinkPostStatistics>
    {
        public decimal LargestSimilaritySoFar { get; private set; }

        private LinkPostStatistics()
        {
        }
        
        public LinkPostStatistics(decimal largestSimilaritySoFar)
        {
            LargestSimilaritySoFar = largestSimilaritySoFar;
        }
    }
}