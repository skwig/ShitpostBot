using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ShitpostBot.Application.Features.BotCommands.Redacted;
using ShitpostBot.Application.Features.PostTracking;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using Xunit;

namespace ShitpostBot.Tests.Unit;

public class MarkImagePostUnavailableHandlerTests
{
    [Fact]
    public async Task Handle_MarksPostUnavailable_WhenImagePostExists()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_MarkUnavailable_Success")
            .Options;

        var context = new TestDbContext(options);
        var unitOfWork = new TestUnitOfWork(context);
        var logger = Substitute.For<ILogger<MarkImagePostUnavailableHandler>>();
        
        var now = DateTimeOffset.UtcNow;
        var messageId = 123456789UL;
        var image = Image.CreateOrDefault(1, new Uri("https://example.com/image.jpg"), "image/jpeg", now)!;
        var post = ImagePost.Create(
            now, 
            new ChatMessageIdentifier(1, 2, messageId), 
            new PosterIdentifier(100), 
            now, 
            image);
        
        context.ImagePost.Add(post);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var handler = new MarkImagePostUnavailableHandler(logger, context, unitOfWork);
        var notification = new MessageDeleted(new MessageIdentification(1, 2, 100, messageId));

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        var updatedPost = await context.ImagePost.FindAsync(post.Id);
        updatedPost.Should().NotBeNull();
        updatedPost!.IsPostAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_DoesNothing_WhenImagePostDoesNotExist()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_MarkUnavailable_NotFound")
            .Options;

        var context = new TestDbContext(options);
        var unitOfWork = new TestUnitOfWork(context);
        var logger = Substitute.For<ILogger<MarkImagePostUnavailableHandler>>();

        var handler = new MarkImagePostUnavailableHandler(logger, context, unitOfWork);
        var notification = new MessageDeleted(new MessageIdentification(1, 2, 100, 999999UL));

        // Act & Assert - should not throw
        await handler.Handle(notification, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_IsIdempotent_WhenPostAlreadyUnavailable()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_MarkUnavailable_Idempotent")
            .Options;

        var context = new TestDbContext(options);
        var unitOfWork = new TestUnitOfWork(context);
        var logger = Substitute.For<ILogger<MarkImagePostUnavailableHandler>>();
        
        var now = DateTimeOffset.UtcNow;
        var messageId = 123456789UL;
        var image = Image.CreateOrDefault(1, new Uri("https://example.com/image.jpg"), "image/jpeg", now)!;
        var post = ImagePost.Create(
            now, 
            new ChatMessageIdentifier(1, 2, messageId), 
            new PosterIdentifier(100), 
            now, 
            image);
        post.MarkPostAsUnavailable(); // Already unavailable
        
        context.ImagePost.Add(post);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var handler = new MarkImagePostUnavailableHandler(logger, context, unitOfWork);
        var notification = new MessageDeleted(new MessageIdentification(1, 2, 100, messageId));

        // Act - should not throw
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        var updatedPost = await context.ImagePost.FindAsync(post.Id);
        updatedPost.Should().NotBeNull();
        updatedPost!.IsPostAvailable.Should().BeFalse();
    }
}
