using Microsoft.EntityFrameworkCore;
using ShitpostBot.Domain;

namespace ShitpostBot.Infrastructure;

internal class ShitpostBotDbContext : DbContext
{
    public ShitpostBotDbContext(DbContextOptions<ShitpostBotDbContext> options) : base(options)
    {
        Database.SetCommandTimeout(9000);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ShitpostBotDbContext).Assembly);
    }

    public virtual DbSet<ImagePost> ImagePost { get; set; } = null!;
    public virtual DbSet<LinkPost> LinkPost { get; set; } = null!;
    public virtual DbSet<WhitelistedPost> WhitelistedPost { get; set; } = null!;
}