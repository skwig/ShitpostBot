using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure.Extensions;
using Xunit;

namespace ShitpostBot.Tests.Unit;

public class ImagePostQueryExtensionsTests
{
    [Fact]
    public async Task GetByChatMessageId_ReturnsPost_WhenMessageIdMatches()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_GetByChatMessageId_Found")
            .Options;

        using var context = new TestDbContext(options);
        
        var now = DateTimeOffset.UtcNow;
        var image = Image.CreateOrDefault(1, new Uri("https://example.com/image.jpg"), "image/jpeg", now)!;
        var messageId = 123456789UL;
        var post = ImagePost.Create(
            now, 
            new ChatMessageIdentifier(1, 2, messageId), 
            new PosterIdentifier(100), 
            now, 
            image);
        
        context.ImagePost.Add(post);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Act
        var result = await context.ImagePost.GetByChatMessageId(messageId);

        // Assert
        result.Should().NotBeNull();
        result!.ChatMessageId.Should().Be(messageId);
    }

    [Fact]
    public async Task GetByChatMessageId_ReturnsNull_WhenMessageIdNotFound()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_GetByChatMessageId_NotFound")
            .Options;

        using var context = new TestDbContext(options);

        // Act
        var result = await context.ImagePost.GetByChatMessageId(999999UL);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByChatMessageId_ReturnsPost_EvenWhenUnavailable()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_GetByChatMessageId_Unavailable")
            .Options;

        using var context = new TestDbContext(options);
        
        var now = DateTimeOffset.UtcNow;
        var image = Image.CreateOrDefault(1, new Uri("https://example.com/image.jpg"), "image/jpeg", now)!;
        var messageId = 123456789UL;
        var post = ImagePost.Create(
            now, 
            new ChatMessageIdentifier(1, 2, messageId), 
            new PosterIdentifier(100), 
            now, 
            image);
        post.MarkPostAsUnavailable();
        
        context.ImagePost.Add(post);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Act
        var result = await context.ImagePost.GetByChatMessageId(messageId);

        // Assert - Should still find it even when unavailable (we need to mark it unavailable!)
        result.Should().NotBeNull();
        result!.IsPostAvailable.Should().BeFalse();
    }
}
