using System;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ShitpostBot.Domain;

namespace ShitpostBot.Infrastructure
{
    internal class ShitpostBotContextFactory : IDesignTimeDbContextFactory<ShitpostBotDbContext>
    {
        public ShitpostBotDbContext CreateDbContext(string[] args)
        {
            const string connString = "Server=localhost,1433;Initial Catalog=shitpostbot-db;Persist Security Info=False;User ID=sa;Password=P@ssword123;MultipleActiveResultSets=False;Connection Timeout=30;";
            
            var optionsBuilder = new DbContextOptionsBuilder<ShitpostBotDbContext>();
            optionsBuilder.UseSqlServer(connString, opts => opts.CommandTimeout((int) TimeSpan.FromMinutes(100).TotalSeconds));

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
            
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ShitpostBotDbContext).Assembly);
        }

        public virtual DbSet<ImagePost> ImagePost { get; set; } = null!;
        public virtual DbSet<LinkPost> VideoPosts { get; set; } = null!;
    }
}