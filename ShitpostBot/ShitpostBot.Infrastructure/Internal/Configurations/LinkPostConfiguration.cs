using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;
using ShitpostBot.Domain;

namespace ShitpostBot.Infrastructure
{
    public class LinkPostConfiguration : IEntityTypeConfiguration<LinkPost>
    {
        public void Configure(EntityTypeBuilder<LinkPost> builder)
        {
            builder.Property(b => b.Content).HasConversion(
                modelValue => JsonConvert.SerializeObject(modelValue, Config.DatabaseJsonSerializerSettings),
                columnValue => JsonConvert.DeserializeObject<LinkPostContent>(columnValue, Config.DatabaseJsonSerializerSettings)
            );

            builder.Ignore(b => b.LinkPostContent);
        }
    }
}