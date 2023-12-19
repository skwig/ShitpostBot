using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;
using ShitpostBot.Domain;

namespace ShitpostBot.Infrastructure
{
    public class PostConfiguration : IEntityTypeConfiguration<Post>
    {
        public void Configure(EntityTypeBuilder<Post> builder)
        {
            builder.HasKey(b => b.Id);

            builder.HasDiscriminator(b => b.Type)
                .HasValue<ImagePost>(PostType.Image)
                .HasValue<LinkPost>(PostType.Link);
            
            builder.HasIndex(b => b.PostedOn);
            builder.HasIndex(b => b.ChatMessageId);
            builder.HasIndex(b => b.PosterId);
        }
    }
}