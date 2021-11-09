using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;
using ShitpostBot.Domain;

namespace ShitpostBot.Infrastructure
{
    public class ImagePostConfiguration : IEntityTypeConfiguration<ImagePost>
    {
        public void Configure(EntityTypeBuilder<ImagePost> builder)
        {
            var converter = new ValueConverter<PostContent, string>(
                modelValue => JsonConvert.SerializeObject(modelValue, Config.DatabaseJsonSerializerSettings),
                columnValue => JsonConvert.DeserializeObject<ImagePostContent>(columnValue, Config.DatabaseJsonSerializerSettings)
            );

            var comparer = new ValueComparer<PostContent>(
                (l, r) => converter.ConvertToProvider(l) == converter.ConvertToProvider(r),
                v => converter.ConvertToProvider(v).GetHashCode()
            );

            builder.Property(b => b.Content).HasConversion(converter);

            builder.Property(b => b.Content).Metadata.SetValueComparer(comparer);
            builder.Property(b => b.Content).Metadata.SetValueConverter(converter);
            // builder.Property(b => b.Content).HasColumnType("jsonb");


            builder.Ignore(b => b.ImagePostContent);
        }
    }
}