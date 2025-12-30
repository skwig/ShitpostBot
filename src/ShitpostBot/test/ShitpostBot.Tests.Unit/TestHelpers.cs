using Microsoft.EntityFrameworkCore;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Tests.Unit;

// Test DbContext for in-memory testing
public class TestDbContext : DbContext, IDbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
    
    public DbSet<Post> Post { get; set; } = null!;
    public DbSet<ImagePost> ImagePost { get; set; } = null!;
    public DbSet<LinkPost> LinkPost { get; set; } = null!;
    public DbSet<WhitelistedPost> WhitelistedPost { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Minimal configuration for testing
        // Configure base Post entity (ImagePost inherits from Post)
        modelBuilder.Entity<Post>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.UseTpcMappingStrategy(); // Table-per-concrete-type
        });
        
        modelBuilder.Entity<ImagePost>(entity =>
        {
            entity.OwnsOne(e => e.Image, img =>
            {
                img.Property(i => i.ImageId);
                img.Property(i => i.ImageUri);
                img.Property(i => i.MediaType);
                img.Property(i => i.ImageUriFetchedAt);
                // Ignore ImageFeatures to avoid pgvector type issues in InMemory database
                img.Ignore(i => i.ImageFeatures);
            });
        });
    }
}

// Test UnitOfWork for in-memory testing
public class TestUnitOfWork : IUnitOfWork
{
    private readonly DbContext _context;

    public TestUnitOfWork(DbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
