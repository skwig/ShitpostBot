using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShitpostBot.Domain;

namespace ShitpostBot.Infrastructure;

public class WhitelistedPostConfiguration : IEntityTypeConfiguration<WhitelistedPost>
{
    public void Configure(EntityTypeBuilder<WhitelistedPost> builder)
    {
        builder
            .HasOne(whitelistedPost => whitelistedPost.Post)
            .WithOne()
            .HasForeignKey<WhitelistedPost>(whitelistedPost => whitelistedPost.PostId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}