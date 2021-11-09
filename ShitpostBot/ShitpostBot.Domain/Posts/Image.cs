using System;
using System.Collections.Generic;

namespace ShitpostBot.Domain
{
    public record Image(ulong ImageId, Uri ImageUri, ImageFeatures? ImageFeatures)
    {
        private Image() : this(default, default!, default!)
        {
        }
        
        public double GetSimilarityTo(Image otherImage)
        {
            if (ImageFeatures == null || otherImage.ImageFeatures == null)
            {
                return 0;
            }
            
            return ImageFeatures.GetSimilarityTo(otherImage.ImageFeatures);
        }
    }
}