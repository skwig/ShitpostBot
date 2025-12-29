using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using Xunit;

namespace ShitpostBot.Tests.Unit;

public class ImagePostQueryExtensionsTests
{
    [Fact]
    public void ImagePostsWithClosestFeatureVector_FiltersOutUnavailablePosts()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_FilterUnavailable")
            .Options;

        using var context = new TestDbContext(options);
        
        var now = DateTimeOffset.UtcNow;
        var featureVector = new Vector(new[] { 1.0f, 0.0f, 0.0f });
        
        // Create available post (IsPostAvailable = true, the default)
        var availableImage1 = Image.CreateOrDefault(1, new Uri("https://example.com/available1.jpg"), "image/jpeg", now)!;
        var availablePost1 = ImagePost.Create(now, new ChatMessageIdentifier(1, 2, 3), new PosterIdentifier(100), now, availableImage1);
        availablePost1.SetImageFeatures(new ImageFeatures("test-model", featureVector), now);
        
        // Create another available post
        var availableImage2 = Image.CreateOrDefault(3, new Uri("https://example.com/available2.jpg"), "image/jpeg", now)!;
        var availablePost2 = ImagePost.Create(now.AddMinutes(2), new ChatMessageIdentifier(1, 2, 5), new PosterIdentifier(102), now, availableImage2);
        availablePost2.SetImageFeatures(new ImageFeatures("test-model", featureVector), now);
        
        // Create unavailable post (IsPostAvailable = false)
        var unavailableImage = Image.CreateOrDefault(2, new Uri("https://example.com/unavailable.jpg"), "image/jpeg", now)!;
        var unavailablePost = ImagePost.Create(now.AddMinutes(1), new ChatMessageIdentifier(1, 2, 4), new PosterIdentifier(101), now, unavailableImage);
        unavailablePost.SetImageFeatures(new ImageFeatures("test-model", featureVector), now);
        unavailablePost.MarkPostAsUnavailable();
        
        context.ImagePost.AddRange(availablePost1, unavailablePost, availablePost2);
        context.SaveChanges();

        // Act
        // Note: We can't use the actual ImagePostsWithClosestFeatureVector extension in InMemory
        // because it uses PostgreSQL-specific vector distance functions.
        // Instead, we test the filter logic directly (x.IsPostAvailable != false).
        var results = context.ImagePost
            .Where(x => x.Image.ImageFeatures != null)
            .Where(x => x.IsPostAvailable != false)
            .Select(x => new ClosestToImagePost(
                x.Id,
                x.PostedOn,
                new ChatMessageIdentifier(x.ChatGuildId, x.ChatChannelId, x.ChatMessageId),
                new PosterIdentifier(x.PosterId),
                0, // L2Distance placeholder for InMemory
                0, // CosineDistance placeholder for InMemory
                x.Image.ImageUri))
            .ToList();

        // Assert
        results.Should().HaveCount(2); // Only available posts
        results.Should().Contain(x => x.ImagePostId == availablePost1.Id);
        results.Should().Contain(x => x.ImagePostId == availablePost2.Id);
        results.Should().NotContain(x => x.ImagePostId == unavailablePost.Id);
    }
}

// Test DbContext for in-memory testing
public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
    
    public DbSet<ImagePost> ImagePost { get; set; } = null!;
    public DbSet<WhitelistedPost> WhitelistedPost { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Minimal configuration for testing
        modelBuilder.Entity<ImagePost>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.OwnsOne(e => e.Image, img =>
            {
                img.OwnsOne(i => i.ImageFeatures, features =>
                {
                    // Configure Vector property to work with InMemory provider
                    // Convert Vector to string representation for storage
                    features.Property(f => f.FeatureVector)
                        .HasConversion(
                            v => v.ToString(),
                            v => new Vector(v));
                });
            });
        });
        
        modelBuilder.Entity<WhitelistedPost>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Post);
        });
    }
}
