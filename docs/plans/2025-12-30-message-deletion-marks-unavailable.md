# Message Deletion Marks ImagePost Unavailable Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** When a Discord message containing an image is deleted, mark the corresponding `ImagePost.IsPostAvailable = false` so deleted images won't be flagged as reposts if reposted in a different channel.

**Architecture:** Create a new handler for the existing `MessageDeleted` notification that queries for an `ImagePost` by the message's ChatMessageId and marks it unavailable. This prevents repost detection from flagging images that were deleted and reposted elsewhere.

**Tech Stack:** C# .NET 10.0, EF Core, MediatR, xUnit, FluentAssertions

---

## Task 1: Add GetByChatMessageId Query Extension

**Files:**
- Modify: `src/ShitpostBot/src/ShitpostBot.Infrastructure/Public/Extensions/ImagePostQueryExtensions.cs:10-62`
- Test: `src/ShitpostBot/test/ShitpostBot.Tests.Unit/ImagePostQueryExtensionsTests.cs` (create)

**Step 1: Write the failing test**

Create new test file: `src/ShitpostBot/test/ShitpostBot.Tests.Unit/ImagePostQueryExtensionsTests.cs`

```csharp
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

// Test DbContext for in-memory testing
public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
    
    public DbSet<ImagePost> ImagePost { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Minimal configuration for testing
        modelBuilder.Entity<ImagePost>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.OwnsOne(e => e.Image, img =>
            {
                img.OwnsOne(i => i.ImageFeatures);
            });
        });
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test src/ShitpostBot/test/ShitpostBot.Tests.Unit --filter "FullyQualifiedName~ImagePostQueryExtensionsTests.GetByChatMessageId"`

Expected: FAIL - `GetByChatMessageId` method does not exist

**Step 3: Implement GetByChatMessageId extension method**

In `src/ShitpostBot/src/ShitpostBot.Infrastructure/Public/Extensions/ImagePostQueryExtensions.cs`, add new method after `GetById` (around line 19):

```csharp
public Task<ImagePost?> GetByChatMessageId(ulong chatMessageId,
    CancellationToken cancellationToken = default)
{
    return query
        .Where(ip => ip.ChatMessageId == chatMessageId)
        .SingleOrDefaultAsync(cancellationToken);
}
```

**Important:** Do NOT apply `.Where(x => x.IsPostAvailable)` filter here - we need to find the post even if it's already unavailable so we can mark it unavailable when the message is deleted.

**Step 4: Run tests to verify they pass**

Run: `dotnet test src/ShitpostBot/test/ShitpostBot.Tests.Unit --filter "FullyQualifiedName~ImagePostQueryExtensionsTests.GetByChatMessageId"`

Expected: PASS (all 3 tests)

**Step 5: Commit**

```bash
git add src/ShitpostBot/src/ShitpostBot.Infrastructure/Public/Extensions/ImagePostQueryExtensions.cs \
        src/ShitpostBot/test/ShitpostBot.Tests.Unit/ImagePostQueryExtensionsTests.cs
git commit -m "feat: add GetByChatMessageId query extension for ImagePost"
```

---

## Task 2: Create MarkImagePostUnavailable Handler

**Files:**
- Create: `src/ShitpostBot/src/ShitpostBot.Application/Features/PostTracking/MarkImagePostUnavailableHandler.cs`
- Test: `src/ShitpostBot/test/ShitpostBot.Tests.Unit/MarkImagePostUnavailableHandlerTests.cs` (create)

**Step 1: Write the failing test**

Create test file: `src/ShitpostBot/test/ShitpostBot.Tests.Unit/MarkImagePostUnavailableHandlerTests.cs`

```csharp
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
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test src/ShitpostBot/test/ShitpostBot.Tests.Unit --filter "FullyQualifiedName~MarkImagePostUnavailableHandlerTests"`

Expected: FAIL - `MarkImagePostUnavailableHandler` class does not exist

**Step 3: Implement MarkImagePostUnavailableHandler**

Create file: `src/ShitpostBot/src/ShitpostBot.Application/Features/PostTracking/MarkImagePostUnavailableHandler.cs`

```csharp
using MediatR;
using ShitpostBot.Application.Features.BotCommands.Redacted;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Extensions;

namespace ShitpostBot.Application.Features.PostTracking;

internal class MarkImagePostUnavailableHandler(
    ILogger<MarkImagePostUnavailableHandler> logger,
    IDbContext dbContext,
    IUnitOfWork unitOfWork)
    : INotificationHandler<MessageDeleted>
{
    public async Task Handle(MessageDeleted notification, CancellationToken cancellationToken)
    {
        var imagePost = await dbContext.ImagePost.GetByChatMessageId(
            notification.Identification.MessageId, 
            cancellationToken);

        if (imagePost == null)
        {
            logger.LogDebug(
                "No ImagePost found for deleted message {MessageId}. Ignoring.", 
                notification.Identification.MessageId);
            return;
        }

        if (!imagePost.IsPostAvailable)
        {
            logger.LogDebug(
                "ImagePost {ImagePostId} for message {MessageId} is already unavailable. Ignoring.",
                imagePost.Id,
                notification.Identification.MessageId);
            return;
        }

        imagePost.MarkPostAsUnavailable();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Marked ImagePost {ImagePostId} as unavailable due to message {MessageId} deletion",
            imagePost.Id,
            notification.Identification.MessageId);
    }
}
```

**Step 4: Run tests to verify they pass**

Run: `dotnet test src/ShitpostBot/test/ShitpostBot.Tests.Unit --filter "FullyQualifiedName~MarkImagePostUnavailableHandlerTests"`

Expected: PASS (all 3 tests)

**Step 5: Commit**

```bash
git add src/ShitpostBot/src/ShitpostBot.Application/Features/PostTracking/MarkImagePostUnavailableHandler.cs \
        src/ShitpostBot/test/ShitpostBot.Tests.Unit/MarkImagePostUnavailableHandlerTests.cs
git commit -m "feat: add handler to mark ImagePost unavailable when Discord message is deleted"
```

---

## Task 3: Run All Tests to Verify No Regressions

**Files:**
- None (verification step)

**Step 1: Run all unit tests**

Run: `dotnet test src/ShitpostBot/test/ShitpostBot.Tests.Unit`

Expected: All tests PASS

**Step 2: Run all integration tests**

Run: `dotnet test src/ShitpostBot/test/ShitpostBot.Tests.Integration`

Expected: All tests PASS

**Step 3: Run full test suite**

Run: `dotnet test src/ShitpostBot`

Expected: All tests PASS

**Step 4: If any tests fail, investigate and fix**

- Check test output for specific failures
- Verify handler is properly registered by MediatR (automatic via assembly scanning)
- Fix any issues found

**Step 5: Commit any test fixes (if needed)**

```bash
# Only if tests were fixed
git add src/ShitpostBot/test/
git commit -m "test: fix tests for message deletion handler"
```

---

## Task 4: Build and Verify Handler Registration

**Files:**
- None (verification step)

**Step 1: Build the solution**

Run: `dotnet build src/ShitpostBot`

Expected: Build succeeds with no errors

**Step 2: Verify handler is auto-registered by MediatR**

The handler implements `INotificationHandler<MessageDeleted>` and is marked `internal`, so it will be automatically discovered and registered by MediatR when the Application layer is registered.

Verify in `src/ShitpostBot/src/ShitpostBot.Application/DependencyInjection.cs` that MediatR is configured to scan the assembly:

Expected to find: `services.AddMediatR(...)` with assembly scanning

**Step 3: Verify MessageDeleted notification is published**

Confirm that `ChatMessageDeletedListener` publishes `MessageDeleted` notification:

File: `src/ShitpostBot/src/ShitpostBot.Worker/Internal/Core/ChatMessageDeletedListener.cs:45`

Expected: `await mediator.Publish(new MessageDeleted(messageIdentification), cancellationToken);` ✅

**Step 4: No code changes needed**

This is verification only - no commit needed.

---

## Task 5: Manual E2E Testing Guidance

**Files:**
- None (manual testing guidance)

**Step 1: Start local development environment**

Run: `docker compose -f docker-compose.yml -f docker-compose.Development.Linux.yml up --build`

Expected: All services start successfully

**Step 2: Manual test scenario**

1. **Post an image in Discord channel A**
   - Bot should track the image as an `ImagePost`
   - `IsPostAvailable` should be `true`

2. **Delete the message from channel A**
   - Handler should mark the `ImagePost` as unavailable
   - Check logs for: `"Marked ImagePost {ImagePostId} as unavailable due to message {MessageId} deletion"`

3. **Post the same image in Discord channel B**
   - Bot should NOT flag it as a repost (because original is unavailable)
   - Image gets tracked as a new `ImagePost`

4. **Verify database state (optional)**
   - Connect to PostgreSQL: `docker exec -it shitpostbot-database-1 psql -U postgres -d ShitpostBot`
   - Query: `SELECT "Id", "ChatMessageId", "IsPostAvailable" FROM "ImagePost" ORDER BY "Id" DESC LIMIT 5;`
   - Expected: First post has `IsPostAvailable = false`, second post has `IsPostAvailable = true`

**Step 3: Test edge cases**

1. **Delete a text message (no image)**: Should log "No ImagePost found" and do nothing
2. **Delete bot's own message**: Currently filtered out in `ChatMessageDeletedListener` - no handler invoked
3. **Delete already unavailable post**: Should be idempotent (log "already unavailable")

**Step 4: No code changes needed**

This is manual testing only - no commit needed.

---

## Summary

**What Changed:**
- Added `GetByChatMessageId()` query extension to find `ImagePost` by Discord message ID
- Created `MarkImagePostUnavailableHandler` to handle `MessageDeleted` notification
- Handler silently marks posts unavailable when Discord messages are deleted
- Handler is idempotent and handles edge cases (not found, already unavailable)

**Impact:**
- When users delete Discord messages containing images, the image posts are marked unavailable
- Repost detection will no longer flag deleted images as reposts if reposted elsewhere
- Applies to all message deletions (user, admin, mod) - Discord SDK doesn't differentiate
- Bot's own message deletions are filtered out before handler is invoked

**Testing:**
- Unit tests verify query extension finds posts by message ID (including unavailable posts)
- Unit tests verify handler marks posts unavailable correctly
- Unit tests verify idempotent behavior and edge cases
- Manual E2E testing can verify end-to-end flow

**Files Created:**
1. `src/ShitpostBot/src/ShitpostBot.Application/Features/PostTracking/MarkImagePostUnavailableHandler.cs`
2. `src/ShitpostBot/test/ShitpostBot.Tests.Unit/ImagePostQueryExtensionsTests.cs`
3. `src/ShitpostBot/test/ShitpostBot.Tests.Unit/MarkImagePostUnavailableHandlerTests.cs`

**Files Modified:**
1. `src/ShitpostBot/src/ShitpostBot.Infrastructure/Public/Extensions/ImagePostQueryExtensions.cs`

**Commits:**
1. feat: add GetByChatMessageId query extension for ImagePost
2. feat: add handler to mark ImagePost unavailable when Discord message is deleted
3. (optional) test: fix tests for message deletion handler

**Related:**
- Existing soft delete infrastructure from `docs/plans/2025-12-29-ispostavailable-soft-delete.md` already implemented
- `MessageDeleted` notification already published by `ChatMessageDeletedListener`
- Repost detection queries already filter by `IsPostAvailable`
