using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShitpostBot.Domain;

namespace ShitpostBot.Infrastructure;

public class ImagePostConfiguration : IEntityTypeConfiguration<ImagePost>
{
    public void Configure(EntityTypeBuilder<ImagePost> builder)
    {
        builder.OwnsOne(imagePost => imagePost.Image, navigationBuilder =>
        {
            navigationBuilder.OwnsOne(image => image.ImageFeatures, ownedNavigationBuilder =>
            {
                ownedNavigationBuilder.Property(imageFeatures => imageFeatures.ModelName)
                    .IsRequired();
                
                ownedNavigationBuilder.Property(imageFeatures => imageFeatures.FeatureVector)
                    .HasColumnType("vector");
            });
        });
    }
}