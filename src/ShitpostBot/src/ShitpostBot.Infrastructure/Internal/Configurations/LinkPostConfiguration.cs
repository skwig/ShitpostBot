using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShitpostBot.Domain;

namespace ShitpostBot.Infrastructure;

public class LinkPostConfiguration : IEntityTypeConfiguration<LinkPost>
{
    public void Configure(EntityTypeBuilder<LinkPost> builder)
    {
        builder.OwnsOne(linkPost => linkPost.Link);
    }
}