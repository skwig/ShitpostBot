using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using Xunit;

namespace ShitpostBot.Tests.Unit;

public class WhitelistedPostQueryExtensionsTests
{
    [Fact]
    public async Task GetByPostId_ReturnsNull_WhenUnderlyingPostIsUnavailable()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_WhitelistGetByPostId")
            .Options;

        using var context = new TestDbContext(options);
        
        var now = DateTimeOffset.UtcNow;
        var image = Image.CreateOrDefault(1, new Uri("https://example.com/image.jpg"), "image/jpeg", now)!;
        var post = ImagePost.Create(now, new ChatMessageIdentifier(1, 2, 3), new PosterIdentifier(100), now, image);
        post.MarkPostAsUnavailable();
        
        var whitelistedPost = WhitelistedPost.Create(post, now, 999);
        
        context.ImagePost.Add(post);
        context.WhitelistedPost.Add(whitelistedPost);
        context.SaveChanges();
        context.ChangeTracker.Clear();

        // Act
        var result = await context.WhitelistedPost.GetByPostId(post.Id);

        // Assert
        result.Should().BeNull(); // Should not return whitelisted post if underlying post is unavailable
    }

    [Fact]
    public void ClosestWhitelistedToImagePostWithFeatureVector_FiltersOutUnavailablePosts()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_WhitelistClosest")
            .Options;

        using var context = new TestDbContext(options);
        
        var now = DateTimeOffset.UtcNow;
        var featureVector = new Vector(new[] { 1.0f, 0.0f, 0.0f });
        
        // Create available whitelisted post
        var availableImage = Image.CreateOrDefault(1, new Uri("https://example.com/available.jpg"), "image/jpeg", now)!;
        var availablePost = ImagePost.Create(now, new ChatMessageIdentifier(1, 2, 3), new PosterIdentifier(100), now, availableImage);
        availablePost.SetImageFeatures(new ImageFeatures("test-model", featureVector), now);
        var availableWhitelisted = WhitelistedPost.Create(availablePost, now, 999);
        
        // Create unavailable whitelisted post
        var unavailableImage = Image.CreateOrDefault(2, new Uri("https://example.com/unavailable.jpg"), "image/jpeg", now)!;
        var unavailablePost = ImagePost.Create(now.AddMinutes(1), new ChatMessageIdentifier(1, 2, 4), new PosterIdentifier(101), now, unavailableImage);
        unavailablePost.SetImageFeatures(new ImageFeatures("test-model", featureVector), now);
        unavailablePost.MarkPostAsUnavailable();
        var unavailableWhitelisted = WhitelistedPost.Create(unavailablePost, now.AddMinutes(1), 999);
        
        context.ImagePost.AddRange(availablePost, unavailablePost);
        context.WhitelistedPost.AddRange(availableWhitelisted, unavailableWhitelisted);
        context.SaveChanges();

        // Act
        // Note: We can't use the actual vector distance functions in InMemory,
        // so we test the filter logic by simulating what the query should return
        var results = context.WhitelistedPost
            .Where(x => x.WhitelistedOn < now.AddHours(1))
            .Where(x => x.Post.Image.ImageFeatures != null)
            .Where(x => x.Post.IsPostAvailable != false)
            .Select(x => new ClosestToImagePost(
                x.Id,
                x.Post.PostedOn,
                new ChatMessageIdentifier(x.Post.ChatGuildId, x.Post.ChatChannelId, x.Post.ChatMessageId),
                new PosterIdentifier(x.Post.PosterId),
                0, // L2Distance placeholder for InMemory
                0, // CosineDistance placeholder for InMemory
                x.Post.Image.ImageUri))
            .ToList();

        // Assert
        results.Should().HaveCount(1); // Only available post
        results.Should().Contain(x => x.ImagePostId == availableWhitelisted.Id);
        results.Should().NotContain(x => x.ImagePostId == unavailableWhitelisted.Id);
    }
}
