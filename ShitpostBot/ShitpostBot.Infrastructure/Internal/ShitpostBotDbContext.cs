using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ShitpostBot.Domain;

namespace ShitpostBot.Infrastructure;

internal class ShitpostBotContextFactory : IDesignTimeDbContextFactory<ShitpostBotDbContext>
{
    public ShitpostBotDbContext CreateDbContext(string[] args)
    {
        const string connString = "Server=localhost,5432;User ID=postgres;Password=mysecretpassword;";
            
        var optionsBuilder = new DbContextOptionsBuilder<ShitpostBotDbContext>();
        optionsBuilder.UseNpgsql(connString, sqlOpts => sqlOpts
            .CommandTimeout((int)TimeSpan.FromMinutes(100).TotalSeconds)
            .UseVector()
        );

        return new ShitpostBotDbContext(optionsBuilder.Options);
    }
}
    
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