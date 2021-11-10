using System;
using System.Collections.Generic;

namespace ShitpostBot.Domain
{
    public class PostStatistics : ValueObject<PostStatistics>
    {
        /// <summary>
        /// null if not similar to anything
        /// </summary>
        public PostStatisticsMostSimilarTo? MostSimilarTo { get; private set; }

        /// <summary>
        /// Placeholder to allow EF to work. Has no use.
        /// </summary>
        public bool Placeholder { get; private set; }

        private PostStatistics()
        {
        }

        public PostStatistics(PostStatisticsMostSimilarTo? mostSimilarTo)
        {
            MostSimilarTo = mostSimilarTo;
        }
    }

    public class PostStatisticsMostSimilarTo : ValueObject<PostStatisticsMostSimilarTo>
    {
        public long SimilarToPostId { get; private set; }
        public decimal Similarity { get; private set; }

        private PostStatisticsMostSimilarTo()
        {
        }

        public PostStatisticsMostSimilarTo(long similarToPostId, decimal similarity)
        {
            SimilarToPostId = similarToPostId;
            Similarity = similarity;
        }
    }
}