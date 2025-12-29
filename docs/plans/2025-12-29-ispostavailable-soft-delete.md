# IsPostAvailable Soft Delete Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Implement soft delete functionality across all ImagePost queries by consistently applying `IsPostAvailable` filter, treating it as a soft delete flag where unavailable posts are hidden from all queries.

**Architecture:** Add `.Where(x => x.IsPostAvailable != false)` filtering to all query extension methods in `ImagePostQueryExtensions` and `WhitelistedPostQueryExtensions`. This treats `null` values as "available" for backward compatibility with existing data. Update unit tests to verify the filtering behavior.

**Tech Stack:** C# .NET 10.0, EF Core, xUnit, FluentAssertions

---

## Task 1: Add IsPostAvailable Filter to ImagePostsWithClosestFeatureVector (Base)

**Files:**
- Modify: `src/ShitpostBot/src/ShitpostBot.Infrastructure/Public/Extensions/ImagePostQueryExtensions.cs:37-55`
- Test: `src/ShitpostBot/test/ShitpostBot.Tests.Unit/ImagePostQueryExtensionsTests.cs` (create)

**Step 1: Write the failing test**

Create new test file: `src/ShitpostBot/test/ShitpostBot.Tests.Unit/ImagePostQueryExtensionsTests.cs`

```csharp
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
        
        // Create available post
        var availableImage = Image.CreateOrDefault(1, new Uri("https://example.com/available.jpg"), "image/jpeg", now)!;
        availableImage.SetImageFeatures(new ImageFeatures("test-model", featureVector), now);
        var availablePost = ImagePost.Create(now, new ChatMessageIdentifier(1, 2, 3), new PosterIdentifier(100), now, availableImage);
        
        // Create unavailable post
        var unavailableImage = Image.CreateOrDefault(2, new Uri("https://example.com/unavailable.jpg"), "image/jpeg", now)!;
        unavailableImage.SetImageFeatures(new ImageFeatures("test-model", featureVector), now);
        var unavailablePost = ImagePost.Create(now.AddMinutes(1), new ChatMessageIdentifier(1, 2, 4), new PosterIdentifier(101), now, unavailableImage);
        unavailablePost.MarkPostAsUnavailable();
        
        // Create post with null IsPostAvailable (should be treated as available)
        var nullAvailableImage = Image.CreateOrDefault(3, new Uri("https://example.com/null.jpg"), "image/jpeg", now)!;
        nullAvailableImage.SetImageFeatures(new ImageFeatures("test-model", featureVector), now);
        var nullAvailablePost = ImagePost.Create(now.AddMinutes(2), new ChatMessageIdentifier(1, 2, 5), new PosterIdentifier(102), now, nullAvailableImage);
        // Use reflection to set IsPostAvailable to null (simulating legacy data)
        typeof(ImagePost).GetProperty("IsPostAvailable")!.SetValue(nullAvailablePost, null);
        
        context.ImagePost.AddRange(availablePost, unavailablePost, nullAvailablePost);
        context.SaveChanges();

        // Act
        var results = context.ImagePost
            .ImagePostsWithClosestFeatureVector(featureVector)
            .ToList();

        // Assert
        results.Should().HaveCount(2); // Only available and null posts
        results.Should().Contain(x => x.Id == availablePost.Id);
        results.Should().Contain(x => x.Id == nullAvailablePost.Id);
        results.Should().NotContain(x => x.Id == unavailablePost.Id);
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
                img.OwnsOne(i => i.ImageFeatures);
            });
        });
        
        modelBuilder.Entity<WhitelistedPost>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Post);
        });
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test src/ShitpostBot/test/ShitpostBot.Tests.Unit --filter "FullyQualifiedName~ImagePostQueryExtensionsTests.ImagePostsWithClosestFeatureVector_FiltersOutUnavailablePosts"`

Expected: FAIL - test expects 2 results but gets 3 (unavailable post is not filtered)

**Step 3: Implement IsPostAvailable filter**

In `src/ShitpostBot/src/ShitpostBot.Infrastructure/Public/Extensions/ImagePostQueryExtensions.cs`, update the base `ImagePostsWithClosestFeatureVector` method (lines 37-55):

```csharp
public IQueryable<ClosestToImagePost> ImagePostsWithClosestFeatureVector(
    Vector imageFeatureVector,
    OrderBy orderBy = OrderBy.CosineDistance)
{
    return query
        .Where(x => x.Image.ImageFeatures != null)
        .Where(x => x.IsPostAvailable != false)  // ADD THIS LINE - soft delete filter
        .OrderBy(x => orderBy == OrderBy.CosineDistance
            ? x.Image.ImageFeatures!.FeatureVector.CosineDistance(imageFeatureVector)
            : x.Image.ImageFeatures!.FeatureVector.L2Distance(imageFeatureVector))
        .ThenBy(x => x.PostedOn)
        .Select(x => new ClosestToImagePost(
            x.Id,
            x.PostedOn,
            new ChatMessageIdentifier(x.ChatGuildId, x.ChatChannelId, x.ChatMessageId),
            new PosterIdentifier(x.PosterId),
            x.Image.ImageFeatures!.FeatureVector.L2Distance(imageFeatureVector),
            x.Image.ImageFeatures!.FeatureVector.CosineDistance(imageFeatureVector),
            x.Image.ImageUri));
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test src/ShitpostBot/test/ShitpostBot.Tests.Unit --filter "FullyQualifiedName~ImagePostQueryExtensionsTests.ImagePostsWithClosestFeatureVector_FiltersOutUnavailablePosts"`

Expected: PASS

**Step 5: Commit**

```bash
git add src/ShitpostBot/src/ShitpostBot.Infrastructure/Public/Extensions/ImagePostQueryExtensions.cs \
        src/ShitpostBot/test/ShitpostBot.Tests.Unit/ImagePostQueryExtensionsTests.cs
git commit -m "feat: add IsPostAvailable soft delete filter to ImagePostsWithClosestFeatureVector"
```

---

## Task 2: Add IsPostAvailable Filter to GetById

**Files:**
- Modify: `src/ShitpostBot/src/ShitpostBot.Infrastructure/Public/Extensions/ImagePostQueryExtensions.cs:12-16`
- Test: `src/ShitpostBot/test/ShitpostBot.Tests.Unit/ImagePostQueryExtensionsTests.cs`

**Step 1: Write the failing test**

Add to `src/ShitpostBot/test/ShitpostBot.Tests.Unit/ImagePostQueryExtensionsTests.cs`:

```csharp
[Fact]
public async Task GetById_ReturnsNull_WhenPostIsUnavailable()
{
    // Arrange
    var options = new DbContextOptionsBuilder<TestDbContext>()
        .UseInMemoryDatabase(databaseName: "TestDb_GetById_Unavailable")
        .Options;

    using var context = new TestDbContext(options);
    
    var now = DateTimeOffset.UtcNow;
    var image = Image.CreateOrDefault(1, new Uri("https://example.com/image.jpg"), "image/jpeg", now)!;
    var post = ImagePost.Create(now, new ChatMessageIdentifier(1, 2, 3), new PosterIdentifier(100), now, image);
    post.MarkPostAsUnavailable();
    
    context.ImagePost.Add(post);
    context.SaveChanges();
    context.ChangeTracker.Clear();

    // Act
    var result = await context.ImagePost.GetById(post.Id);

    // Assert
    result.Should().BeNull(); // Soft deleted posts should not be retrieved
}

[Fact]
public async Task GetById_ReturnsPost_WhenPostIsAvailable()
{
    // Arrange
    var options = new DbContextOptionsBuilder<TestDbContext>()
        .UseInMemoryDatabase(databaseName: "TestDb_GetById_Available")
        .Options;

    using var context = new TestDbContext(options);
    
    var now = DateTimeOffset.UtcNow;
    var image = Image.CreateOrDefault(1, new Uri("https://example.com/image.jpg"), "image/jpeg", now)!;
    var post = ImagePost.Create(now, new ChatMessageIdentifier(1, 2, 3), new PosterIdentifier(100), now, image);
    
    context.ImagePost.Add(post);
    context.SaveChanges();
    context.ChangeTracker.Clear();

    // Act
    var result = await context.ImagePost.GetById(post.Id);

    // Assert
    result.Should().NotBeNull();
    result!.Id.Should().Be(post.Id);
}

[Fact]
public async Task GetById_ReturnsPost_WhenIsPostAvailableIsNull()
{
    // Arrange
    var options = new DbContextOptionsBuilder<TestDbContext>()
        .UseInMemoryDatabase(databaseName: "TestDb_GetById_Null")
        .Options;

    using var context = new TestDbContext(options);
    
    var now = DateTimeOffset.UtcNow;
    var image = Image.CreateOrDefault(1, new Uri("https://example.com/image.jpg"), "image/jpeg", now)!;
    var post = ImagePost.Create(now, new ChatMessageIdentifier(1, 2, 3), new PosterIdentifier(100), now, image);
    
    // Simulate legacy data with null IsPostAvailable
    typeof(ImagePost).GetProperty("IsPostAvailable")!.SetValue(post, null);
    
    context.ImagePost.Add(post);
    context.SaveChanges();
    context.ChangeTracker.Clear();

    // Act
    var result = await context.ImagePost.GetById(post.Id);

    // Assert
    result.Should().NotBeNull(); // Null is treated as available
    result!.Id.Should().Be(post.Id);
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test src/ShitpostBot/test/ShitpostBot.Tests.Unit --filter "FullyQualifiedName~ImagePostQueryExtensionsTests.GetById"`

Expected: FAIL - GetById_ReturnsNull_WhenPostIsUnavailable expects null but gets the post

**Step 3: Implement IsPostAvailable filter**

In `src/ShitpostBot/src/ShitpostBot.Infrastructure/Public/Extensions/ImagePostQueryExtensions.cs`, update `GetById` (lines 12-16):

```csharp
public Task<ImagePost?> GetById(long id,
    CancellationToken cancellationToken = default)
{
    return query
        .Where(ip => ip.Id == id)
        .Where(ip => ip.IsPostAvailable != false)  // ADD THIS LINE - soft delete filter
        .SingleOrDefaultAsync(cancellationToken);
}
```

**Step 4: Run tests to verify they pass**

Run: `dotnet test src/ShitpostBot/test/ShitpostBot.Tests.Unit --filter "FullyQualifiedName~ImagePostQueryExtensionsTests.GetById"`

Expected: PASS (all 3 tests)

**Step 5: Commit**

```bash
git add src/ShitpostBot/src/ShitpostBot.Infrastructure/Public/Extensions/ImagePostQueryExtensions.cs \
        src/ShitpostBot/test/ShitpostBot.Tests.Unit/ImagePostQueryExtensionsTests.cs
git commit -m "feat: add IsPostAvailable soft delete filter to GetById"
```

---

## Task 3: Add IsPostAvailable Filter to GetHistory

**Files:**
- Modify: `src/ShitpostBot/src/ShitpostBot.Infrastructure/Public/Extensions/ImagePostQueryExtensions.cs:18-25`
- Test: `src/ShitpostBot/test/ShitpostBot.Tests.Unit/ImagePostQueryExtensionsTests.cs`

**Step 1: Write the failing test**

Add to `src/ShitpostBot/test/ShitpostBot.Tests.Unit/ImagePostQueryExtensionsTests.cs`:

```csharp
[Fact]
public async Task GetHistory_ExcludesUnavailablePosts()
{
    // Arrange
    var options = new DbContextOptionsBuilder<TestDbContext>()
        .UseInMemoryDatabase(databaseName: "TestDb_GetHistory")
        .Options;

    using var context = new TestDbContext(options);
    
    var baseTime = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
    
    // Create 3 posts within the time range
    var availableImage1 = Image.CreateOrDefault(1, new Uri("https://example.com/1.jpg"), "image/jpeg", baseTime)!;
    var availablePost1 = ImagePost.Create(baseTime.AddHours(1), new ChatMessageIdentifier(1, 2, 3), new PosterIdentifier(100), baseTime, availableImage1);
    
    var unavailableImage = Image.CreateOrDefault(2, new Uri("https://example.com/2.jpg"), "image/jpeg", baseTime)!;
    var unavailablePost = ImagePost.Create(baseTime.AddHours(2), new ChatMessageIdentifier(1, 2, 4), new PosterIdentifier(101), baseTime, unavailableImage);
    unavailablePost.MarkPostAsUnavailable();
    
    var availableImage2 = Image.CreateOrDefault(3, new Uri("https://example.com/3.jpg"), "image/jpeg", baseTime)!;
    var availablePost2 = ImagePost.Create(baseTime.AddHours(3), new ChatMessageIdentifier(1, 2, 5), new PosterIdentifier(102), baseTime, availableImage2);
    
    // Create post outside time range
    var outsideImage = Image.CreateOrDefault(4, new Uri("https://example.com/4.jpg"), "image/jpeg", baseTime)!;
    var outsidePost = ImagePost.Create(baseTime.AddHours(10), new ChatMessageIdentifier(1, 2, 6), new PosterIdentifier(103), baseTime, outsideImage);
    
    context.ImagePost.AddRange(availablePost1, unavailablePost, availablePost2, outsidePost);
    context.SaveChanges();

    // Act
    var results = await context.ImagePost.GetHistory(
        baseTime,
        baseTime.AddHours(5));

    // Assert
    results.Should().HaveCount(2); // Only the 2 available posts within range
    results.Should().Contain(x => x.Id == availablePost1.Id);
    results.Should().Contain(x => x.Id == availablePost2.Id);
    results.Should().NotContain(x => x.Id == unavailablePost.Id);
    results.Should().NotContain(x => x.Id == outsidePost.Id);
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test src/ShitpostBot/test/ShitpostBot.Tests.Unit --filter "FullyQualifiedName~ImagePostQueryExtensionsTests.GetHistory_ExcludesUnavailablePosts"`

Expected: FAIL - expects 2 results but gets 3 (unavailable post included)

**Step 3: Implement IsPostAvailable filter**

In `src/ShitpostBot/src/ShitpostBot.Infrastructure/Public/Extensions/ImagePostQueryExtensions.cs`, update `GetHistory` (lines 18-25):

```csharp
public async Task<IReadOnlyList<ImagePost>> GetHistory(DateTimeOffset postedAtFromInclusive,
    DateTimeOffset postedAtToExclusive,
    CancellationToken cancellationToken = default)
{
    return await query
        .Where(x => postedAtFromInclusive <= x.PostedOn && x.PostedOn < postedAtToExclusive)
        .Where(x => x.IsPostAvailable != false)  // ADD THIS LINE - soft delete filter
        .ToListAsync(cancellationToken);
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test src/ShitpostBot/test/ShitpostBot.Tests.Unit --filter "FullyQualifiedName~ImagePostQueryExtensionsTests.GetHistory_ExcludesUnavailablePosts"`

Expected: PASS

**Step 5: Commit**

```bash
git add src/ShitpostBot/src/ShitpostBot.Infrastructure/Public/Extensions/ImagePostQueryExtensions.cs \
        src/ShitpostBot/test/ShitpostBot.Tests.Unit/ImagePostQueryExtensionsTests.cs
git commit -m "feat: add IsPostAvailable soft delete filter to GetHistory"
```

---

## Task 4: Add IsPostAvailable Filter to WhitelistedPost Queries

**Files:**
- Modify: `src/ShitpostBot/src/ShitpostBot.Infrastructure/Public/Extensions/WhitelistedPostQueryExtensions.cs:12-36`
- Test: `src/ShitpostBot/test/ShitpostBot.Tests.Unit/WhitelistedPostQueryExtensionsTests.cs` (create)

**Step 1: Write the failing test**

Create new test file: `src/ShitpostBot/test/ShitpostBot.Tests.Unit/WhitelistedPostQueryExtensionsTests.cs`

```csharp
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
        
        var whitelistedPost = WhitelistedPost.Create(post, now);
        
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
        availableImage.SetImageFeatures(new ImageFeatures("test-model", featureVector), now);
        var availablePost = ImagePost.Create(now, new ChatMessageIdentifier(1, 2, 3), new PosterIdentifier(100), now, availableImage);
        var availableWhitelisted = WhitelistedPost.Create(availablePost, now);
        
        // Create unavailable whitelisted post
        var unavailableImage = Image.CreateOrDefault(2, new Uri("https://example.com/unavailable.jpg"), "image/jpeg", now)!;
        unavailableImage.SetImageFeatures(new ImageFeatures("test-model", featureVector), now);
        var unavailablePost = ImagePost.Create(now.AddMinutes(1), new ChatMessageIdentifier(1, 2, 4), new PosterIdentifier(101), now, unavailableImage);
        unavailablePost.MarkPostAsUnavailable();
        var unavailableWhitelisted = WhitelistedPost.Create(unavailablePost, now.AddMinutes(1));
        
        context.ImagePost.AddRange(availablePost, unavailablePost);
        context.WhitelistedPost.AddRange(availableWhitelisted, unavailableWhitelisted);
        context.SaveChanges();

        // Act
        var results = context.WhitelistedPost
            .ClosestWhitelistedToImagePostWithFeatureVector(now.AddHours(1), featureVector)
            .ToList();

        // Assert
        results.Should().HaveCount(1); // Only available post
        results.Should().Contain(x => x.Id == availableWhitelisted.Id);
        results.Should().NotContain(x => x.Id == unavailableWhitelisted.Id);
    }
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test src/ShitpostBot/test/ShitpostBot.Tests.Unit --filter "FullyQualifiedName~WhitelistedPostQueryExtensionsTests"`

Expected: FAIL - both tests fail because unavailable posts are not filtered

**Step 3: Implement IsPostAvailable filter**

In `src/ShitpostBot/src/ShitpostBot.Infrastructure/Public/Extensions/WhitelistedPostQueryExtensions.cs`:

Update `GetByPostId` (lines 12-16):

```csharp
public Task<WhitelistedPost?> GetByPostId(long postId,
    CancellationToken cancellationToken = default)
{
    return query
        .Where(wp => wp.Post.Id == postId)
        .Where(wp => wp.Post.IsPostAvailable != false)  // ADD THIS LINE - soft delete filter
        .SingleOrDefaultAsync(cancellationToken);
}
```

Update `ClosestWhitelistedToImagePostWithFeatureVector` (lines 18-36):

```csharp
public IQueryable<ClosestToImagePost> ClosestWhitelistedToImagePostWithFeatureVector(DateTimeOffset postedOnBefore,
    Vector imagePostFeatureVector,
    OrderBy orderBy = OrderBy.CosineDistance)
{
    return query
        .Where(x => x.WhitelistedOn < postedOnBefore)
        .Where(x => x.Post.IsPostAvailable != false)  // ADD THIS LINE - soft delete filter
        .OrderBy(x => orderBy == OrderBy.CosineDistance
            ? x.Post.Image.ImageFeatures!.FeatureVector.CosineDistance(imagePostFeatureVector)
            : x.Post.Image.ImageFeatures!.FeatureVector.L2Distance(imagePostFeatureVector))
        .ThenBy(x => x.Post.PostedOn)
        .Select(x => new ClosestToImagePost(
            x.Id,
            x.Post.PostedOn,
            new ChatMessageIdentifier(x.Post.ChatGuildId, x.Post.ChatChannelId, x.Post.ChatMessageId),
            new PosterIdentifier(x.Post.PosterId),
            x.Post.Image.ImageFeatures!.FeatureVector.L2Distance(imagePostFeatureVector),
            x.Post.Image.ImageFeatures!.FeatureVector.CosineDistance(imagePostFeatureVector),
            x.Post.Image.ImageUri));
}
```

**Step 4: Run tests to verify they pass**

Run: `dotnet test src/ShitpostBot/test/ShitpostBot.Tests.Unit --filter "FullyQualifiedName~WhitelistedPostQueryExtensionsTests"`

Expected: PASS (both tests)

**Step 5: Commit**

```bash
git add src/ShitpostBot/src/ShitpostBot.Infrastructure/Public/Extensions/WhitelistedPostQueryExtensions.cs \
        src/ShitpostBot/test/ShitpostBot.Tests.Unit/WhitelistedPostQueryExtensionsTests.cs
git commit -m "feat: add IsPostAvailable soft delete filter to WhitelistedPost queries"
```

---

## Task 5: Run All Tests to Verify No Regressions

**Files:**
- None (verification step)

**Step 1: Run all unit tests**

Run: `dotnet test src/ShitpostBot/test/ShitpostBot.Tests.Unit`

Expected: All tests PASS

**Step 2: Run all integration tests**

Run: `dotnet test src/ShitpostBot/test/ShitpostBot.Tests.Integration`

Expected: All tests PASS

**Step 3: If any tests fail, investigate and fix**

- Check test output for specific failures
- Verify that existing tests don't rely on unavailable posts being returned
- Update tests if needed to account for soft delete behavior

**Step 4: Run full test suite**

Run: `dotnet test src/ShitpostBot`

Expected: All tests PASS

**Step 5: Commit any test fixes (if needed)**

```bash
# Only if tests were fixed
git add src/ShitpostBot/test/
git commit -m "test: update existing tests for IsPostAvailable soft delete behavior"
```

---

## Task 6: Manual Verification of Query Behavior

**Files:**
- None (manual verification)

**Step 1: Build the solution**

Run: `dotnet build src/ShitpostBot`

Expected: Build succeeds with no errors

**Step 2: Review all usage locations**

Verify these files now use the filtered queries:

1. `src/ShitpostBot/src/ShitpostBot.Application/Features/BotCommands/EvaluateRepost/EvaluateRepost_ImagePostTrackedHandler.cs:95-100`
   - Uses `ImagePostsWithClosestFeatureVector()` ✅

2. `src/ShitpostBot/src/ShitpostBot.Application/Features/BotCommands/Search/SearchBotCommandHandler.cs:62-66`
   - Uses `ImagePostsWithClosestFeatureVector()` ✅

3. `src/ShitpostBot/src/ShitpostBot.Application/Features/BotCommands/RepostMatch/RepostMatchAllBotCommandHandler.cs:97-101`
   - Uses `ImagePostsWithClosestFeatureVector()` ✅

4. `src/ShitpostBot/src/ShitpostBot.Application/Features/BotCommands/RepostMatch/RepostMatchAndRepostWhereBotCommandHandler.cs:97-100`
   - Uses `ImagePostsWithClosestFeatureVector()` ✅

5. `src/ShitpostBot/src/ShitpostBot.Application/Features/BotCommands/EvaluateRepost/EvaluateRepost_ImagePostTrackedHandler.cs:79`
   - Uses `ClosestWhitelistedToImagePostWithFeatureVector()` ✅

6. `src/ShitpostBot/src/ShitpostBot.Application/Features/BotCommands/RepostMatch/RepostMatchAllBotCommandHandler.cs:108`
   - Uses `ClosestWhitelistedToImagePostWithFeatureVector()` ✅

7. `src/ShitpostBot/src/ShitpostBot.Application/Features/BotCommands/RepostMatch/RepostMatchAndRepostWhereBotCommandHandler.cs:83`
   - Uses `ClosestWhitelistedToImagePostWithFeatureVector()` ✅

**Step 3: Verify no direct DbContext queries bypass filters**

Search for any raw queries that might bypass extension methods:

Run: `grep -r "dbContext.ImagePost" src/ShitpostBot/src/ShitpostBot.Application --include="*.cs"`

Expected: All queries use the extension methods (not raw LINQ on DbContext)

**Step 4: Document the change**

No commit needed - verification only.

---

## Summary

**What Changed:**
- All `ImagePost` query extension methods now filter by `IsPostAvailable != false`
- All `WhitelistedPost` query extension methods now filter by `Post.IsPostAvailable != false`
- `null` values for `IsPostAvailable` are treated as "available" (backward compatible)
- Comprehensive unit tests added for all query methods

**Impact:**
- Repost detection will only compare against available posts
- Search results will only show available posts
- Manual repost match commands will only show available posts
- URL refresher and post re-evaluator already filtered correctly (no change needed)

**Testing:**
- Unit tests verify filtering behavior with available, unavailable, and null states
- Integration tests verify no regressions in existing functionality
- E2E tests can be run with: `./test/e2e/run-e2e-tests.sh`

**Files Modified:**
1. `src/ShitpostBot/src/ShitpostBot.Infrastructure/Public/Extensions/ImagePostQueryExtensions.cs`
2. `src/ShitpostBot/src/ShitpostBot.Infrastructure/Public/Extensions/WhitelistedPostQueryExtensions.cs`

**Files Created:**
1. `src/ShitpostBot/test/ShitpostBot.Tests.Unit/ImagePostQueryExtensionsTests.cs`
2. `src/ShitpostBot/test/ShitpostBot.Tests.Unit/WhitelistedPostQueryExtensionsTests.cs`

**Commits:**
1. feat: add IsPostAvailable soft delete filter to ImagePostsWithClosestFeatureVector
2. feat: add IsPostAvailable soft delete filter to GetById
3. feat: add IsPostAvailable soft delete filter to GetHistory
4. feat: add IsPostAvailable soft delete filter to WhitelistedPost queries
5. (optional) test: update existing tests for IsPostAvailable soft delete behavior
