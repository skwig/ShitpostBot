using System;
using System.Collections.Generic;

namespace ShitpostBot.Domain
{
    public sealed record ImagePostStatistics : ValueObject<ImagePostStatistics>
    {
        public long LargestSimilaritySoFarToId { get; private set; }
        public decimal LargestSimilaritySoFar { get; private set; }

        private ImagePostStatistics()
        {
            
        }
        
        public ImagePostStatistics(long largestSimilaritySoFarToId, decimal largestSimilaritySoFar)
        {
            LargestSimilaritySoFarToId = largestSimilaritySoFarToId;
            LargestSimilaritySoFar = largestSimilaritySoFar;
        }
    }
}