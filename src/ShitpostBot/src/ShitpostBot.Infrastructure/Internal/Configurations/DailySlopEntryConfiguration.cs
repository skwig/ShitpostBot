using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShitpostBot.Domain;

namespace ShitpostBot.Infrastructure;

public class DailySlopEntryConfiguration : IEntityTypeConfiguration<DailySlopEntry>
{
    public void Configure(EntityTypeBuilder<DailySlopEntry> builder)
    {
        builder.ToTable("DailySlopEntry");
        builder.HasKey(b => b.Id);
        builder.HasIndex(b => b.PosterId);
        builder.HasIndex(b => b.PostedOn);
        builder
            .HasIndex(b => new
            {
                b.ChatGuildId,
                b.ChatChannelId,
                b.ChatMessageId,
            })
            .IsUnique();
    }
}