using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShitpostBot.Domain;

namespace ShitpostBot.Infrastructure;

public class ConversationFragmentConfiguration : IEntityTypeConfiguration<ConversationFragment>
{
    public void Configure(EntityTypeBuilder<ConversationFragment> builder)
    {
        builder.Property(fragment => fragment.Embedding).HasColumnType("vector(384)");

        builder
            .HasIndex(fragment => new
            {
                fragment.GuildId,
                fragment.ChannelId,
                fragment.FirstMessageId,
            })
            .IsUnique();
    }
}
