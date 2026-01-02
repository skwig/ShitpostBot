# Stats Bot Command Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a `stats` bot command that displays the count of ImagePosts and LinkPosts available for repost matching.

**Architecture:** Follow the existing `IBotCommandHandler` pattern used by other bot commands (About, Help, Search, etc.). The handler queries the database directly via `IDbContext` to count available posts, then sends a formatted message back to Discord via `IChatClient`.

**Tech Stack:** C# .NET 10.0, Entity Framework Core, DSharpPlus (Discord library), MediatR pattern

---

## Task 1: Create StatsBotCommandHandler with TDD

**Files:**
- Create: `src/ShitpostBot/test/ShitpostBot.Tests.Unit/StatsBotCommandHandlerTests.cs`
- Create: `src/ShitpostBot/src/ShitpostBot.Application/Features/BotCommands/Stats/StatsBotCommandHandler.cs`

### Step 1: Write the failing test for command matching

**Purpose:** Verify the handler only processes "stats" commands

**Create:** `src/ShitpostBot/test/ShitpostBot.Tests.Unit/StatsBotCommandHandlerTests.cs`

```csharp
using NSubstitute;
using ShitpostBot.Application.Features.BotCommands.Stats;
using ShitpostBot.Domain.Services;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Tests.Unit;

[TestFixture]
public class StatsBotCommandHandlerTests
{
    [Test]
    public async Task TryHandle_WhenCommandIsNotStats_ReturnsFalse()
    {
        // Arrange
        var dbContext = Substitute.For<IDbContext>();
        var chatClient = Substitute.For<IChatClient>();
        var handler = new StatsBotCommandHandler(dbContext, chatClient);
        
        var commandMessageId = new MessageIdentification(
            GuildId: 123ul,
            ChannelId: 456ul,
            MessageId: 789ul,
            PosterId: 111ul
        );
        
        var command = new BotCommand("about", []);
        
        // Act
        var result = await handler.TryHandle(
            commandMessageId,
            null,
            command,
            null
        );
        
        // Assert
        Assert.That(result, Is.False);
    }
    
    [Test]
    public async Task TryHandle_WhenCommandIsStats_ReturnsTrue()
    {
        // Arrange
        var dbContext = Substitute.For<IDbContext>();
        var chatClient = Substitute.For<IChatClient>();
        var handler = new StatsBotCommandHandler(dbContext, chatClient);
        
        var commandMessageId = new MessageIdentification(
            GuildId: 123ul,
            ChannelId: 456ul,
            MessageId: 789ul,
            PosterId: 111ul
        );
        
        var command = new BotCommand("stats", []);
        
        // Act
        var result = await handler.TryHandle(
            commandMessageId,
            null,
            command,
            null
        );
        
        // Assert
        Assert.That(result, Is.True);
    }
}
```

### Step 2: Run test to verify it fails

**Run:**
```bash
cd src/ShitpostBot
dotnet test --filter "FullyQualifiedName~StatsBotCommandHandlerTests" --logger "console;verbosity=detailed"
```

**Expected Output:**
```
Compilation error: StatsBotCommandHandler does not exist
```

### Step 3: Write minimal implementation to make tests compile

**Create:** `src/ShitpostBot/src/ShitpostBot.Application/Features/BotCommands/Stats/StatsBotCommandHandler.cs`

```csharp
using ShitpostBot.Domain.Services;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.BotCommands.Stats;

public class StatsBotCommandHandler(
    IDbContext dbContext,
    IChatClient chatClient)
    : IBotCommandHandler
{
    public string? GetHelpMessage() => "`stats` - displays count of posts available for repost detection";

    public async Task<bool> TryHandle(
        MessageIdentification commandMessageIdentification,
        MessageIdentification? referencedMessageIdentification,
        BotCommand command,
        BotCommandEdit? edit)
    {
        if (command.Command != "stats")
        {
            return false;
        }

        var messageDestination = new MessageDestination(
            commandMessageIdentification.GuildId,
            commandMessageIdentification.ChannelId,
            commandMessageIdentification.MessageId
        );

        // TODO: Query database and send stats
        await chatClient.SendMessage(messageDestination, "Stats coming soon!");
        
        return true;
    }
}
```

### Step 4: Run tests to verify they pass

**Run:**
```bash
cd src/ShitpostBot
dotnet test --filter "FullyQualifiedName~StatsBotCommandHandlerTests" --logger "console;verbosity=detailed"
```

**Expected Output:**
```
Test Run Successful.
Total tests: 2
     Passed: 2
```

### Step 5: Commit

**Run:**
```bash
git add src/ShitpostBot/test/ShitpostBot.Tests.Unit/StatsBotCommandHandlerTests.cs
git add src/ShitpostBot/src/ShitpostBot.Application/Features/BotCommands/Stats/StatsBotCommandHandler.cs
git commit -m "feat: add stats bot command handler skeleton with basic tests"
```

---

## Task 2: Implement Database Query Logic with TDD

**Files:**
- Modify: `src/ShitpostBot/test/ShitpostBot.Tests.Unit/StatsBotCommandHandlerTests.cs`
- Modify: `src/ShitpostBot/src/ShitpostBot.Application/Features/BotCommands/Stats/StatsBotCommandHandler.cs`

### Step 1: Write failing test for stats query logic

**Add to:** `src/ShitpostBot/test/ShitpostBot.Tests.Unit/StatsBotCommandHandlerTests.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using ShitpostBot.Domain.Posts;

// Add to StatsBotCommandHandlerTests class:

[Test]
public async Task TryHandle_WhenCommandIsStats_QueriesAvailableImagePostsWithFeatures()
{
    // Arrange
    var dbContext = Substitute.For<IDbContext>();
    var chatClient = Substitute.For<IChatClient>();
    
    // Mock ImagePost DbSet with test data
    var imagePosts = new List<ImagePost>
    {
        // Available with features - SHOULD BE COUNTED
        CreateImagePost(1, isAvailable: true, hasFeatures: true),
        CreateImagePost(2, isAvailable: true, hasFeatures: true),
        
        // Available without features - SHOULD NOT BE COUNTED
        CreateImagePost(3, isAvailable: true, hasFeatures: false),
        
        // Unavailable with features - SHOULD NOT BE COUNTED
        CreateImagePost(4, isAvailable: false, hasFeatures: true),
    }.AsQueryable();
    
    var mockImagePostSet = CreateMockDbSet(imagePosts);
    dbContext.ImagePost.Returns(mockImagePostSet);
    
    // Mock LinkPost DbSet with test data
    var linkPosts = new List<LinkPost>
    {
        CreateLinkPost(1),
        CreateLinkPost(2),
        CreateLinkPost(3),
    }.AsQueryable();
    
    var mockLinkPostSet = CreateMockDbSet(linkPosts);
    dbContext.LinkPost.Returns(mockLinkPostSet);
    
    var handler = new StatsBotCommandHandler(dbContext, chatClient);
    
    var commandMessageId = new MessageIdentification(
        GuildId: 123ul,
        ChannelId: 456ul,
        MessageId: 789ul,
        PosterId: 111ul
    );
    
    var command = new BotCommand("stats", []);
    
    // Act
    await handler.TryHandle(commandMessageId, null, command, null);
    
    // Assert
    await chatClient.Received(1).SendMessage(
        Arg.Is<MessageDestination>(md => 
            md.GuildId == 123ul && 
            md.ChannelId == 456ul && 
            md.ReplyToMessageId == 789ul
        ),
        Arg.Is<string>(msg => 
            msg.Contains("2") &&  // 2 available ImagePosts with features
            msg.Contains("3")     // 3 LinkPosts
        )
    );
}

private static ImagePost CreateImagePost(long id, bool isAvailable, bool hasFeatures)
{
    var postedOn = DateTimeOffset.UtcNow.AddDays(-id);
    var imageUrl = $"https://example.com/image{id}.jpg";
    
    var imagePost = ImagePost.Track(
        postedOn,
        guildId: 123ul,
        channelId: 456ul,
        messageId: (ulong)(1000 + id),
        posterId: 999ul,
        imageUrl: imageUrl,
        imageHash: $"hash{id}"
    );
    
    if (hasFeatures)
    {
        var featureVector = new float[512];
        imagePost.SetImageFeatures(featureVector, postedOn);
    }
    
    if (!isAvailable)
    {
        imagePost.MarkPostAsUnavailable();
    }
    
    return imagePost;
}

private static LinkPost CreateLinkPost(long id)
{
    var postedOn = DateTimeOffset.UtcNow.AddDays(-id);
    
    return LinkPost.Track(
        postedOn,
        guildId: 123ul,
        channelId: 456ul,
        messageId: (ulong)(2000 + id),
        posterId: 999ul,
        linkProvider: "Reddit",
        linkUri: new Uri($"https://reddit.com/post{id}")
    );
}

private static DbSet<T> CreateMockDbSet<T>(IQueryable<T> data) where T : class
{
    var mockSet = Substitute.For<DbSet<T>, IQueryable<T>>();
    ((IQueryable<T>)mockSet).Provider.Returns(data.Provider);
    ((IQueryable<T>)mockSet).Expression.Returns(data.Expression);
    ((IQueryable<T>)mockSet).ElementType.Returns(data.ElementType);
    ((IQueryable<T>)mockSet).GetEnumerator().Returns(data.GetEnumerator());
    return mockSet;
}
```

### Step 2: Run test to verify it fails

**Run:**
```bash
cd src/ShitpostBot
dotnet test --filter "FullyQualifiedName~StatsBotCommandHandlerTests.TryHandle_WhenCommandIsStats_QueriesAvailableImagePostsWithFeatures" --logger "console;verbosity=detailed"
```

**Expected Output:**
```
Test Failed: Expected message to contain "2" and "3", but got "Stats coming soon!"
```

### Step 3: Implement the database query logic

**Modify:** `src/ShitpostBot/src/ShitpostBot.Application/Features/BotCommands/Stats/StatsBotCommandHandler.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using ShitpostBot.Domain.Services;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.BotCommands.Stats;

public class StatsBotCommandHandler(
    IDbContext dbContext,
    IChatClient chatClient)
    : IBotCommandHandler
{
    public string? GetHelpMessage() => "`stats` - displays count of posts available for repost detection";

    public async Task<bool> TryHandle(
        MessageIdentification commandMessageIdentification,
        MessageIdentification? referencedMessageIdentification,
        BotCommand command,
        BotCommandEdit? edit)
    {
        if (command.Command != "stats")
        {
            return false;
        }

        var messageDestination = new MessageDestination(
            commandMessageIdentification.GuildId,
            commandMessageIdentification.ChannelId,
            commandMessageIdentification.MessageId
        );

        // Count ImagePosts that are available AND have features extracted
        var availableImagePostCount = await dbContext.ImagePost
            .Where(p => p.IsPostAvailable)
            .Where(p => p.Image.ImageFeatures != null)
            .CountAsync();

        // Count all LinkPosts (they're always available)
        var availableLinkPostCount = await dbContext.LinkPost
            .CountAsync();

        var message = $"**ShitpostBot Stats**\n\n" +
                      $"Available ImagePosts: {availableImagePostCount}\n" +
                      $"Available LinkPosts: {availableLinkPostCount}\n" +
                      $"Total: {availableImagePostCount + availableLinkPostCount}";

        await chatClient.SendMessage(messageDestination, message);
        
        return true;
    }
}
```

### Step 4: Run tests to verify they pass

**Run:**
```bash
cd src/ShitpostBot
dotnet test --filter "FullyQualifiedName~StatsBotCommandHandlerTests" --logger "console;verbosity=detailed"
```

**Expected Output:**
```
Test Run Successful.
Total tests: 3
     Passed: 3
```

### Step 5: Commit

**Run:**
```bash
git add src/ShitpostBot/test/ShitpostBot.Tests.Unit/StatsBotCommandHandlerTests.cs
git add src/ShitpostBot/src/ShitpostBot.Application/Features/BotCommands/Stats/StatsBotCommandHandler.cs
git commit -m "feat: implement stats query logic to count available posts"
```

---

## Task 3: Manual Integration Testing

**Files:**
- Test manually using Discord bot

### Step 1: Build and run the application locally

**Run:**
```bash
# From repository root
docker compose -f docker-compose.yml -f docker-compose.Development.Linux.yml up --build
```

**Expected Output:**
```
All services started successfully
Worker container logs show: "Bot is ready"
```

### Step 2: Test the stats command in Discord

**Action:** In your Discord test server, send a message mentioning the bot with the `stats` command:
```
@ShitpostBot stats
```

**Expected Response:** Bot should reply with a message like:
```
**ShitpostBot Stats**

Available ImagePosts: 42
Available LinkPosts: 13
Total: 55
```

### Step 3: Verify help command includes stats

**Action:** Send help command:
```
@ShitpostBot help
```

**Expected Response:** Help message should include:
```
`stats` - displays count of posts available for repost detection
```

### Step 4: Test edge case - no posts in database

**Action:** If you have a fresh database with no posts, verify the command still works:
```
@ShitpostBot stats
```

**Expected Response:**
```
**ShitpostBot Stats**

Available ImagePosts: 0
Available LinkPosts: 0
Total: 0
```

### Step 5: Stop the services

**Run:**
```bash
docker compose down
```

---

## Task 4: Add Integration Test (Optional but Recommended)

**Files:**
- Modify: `src/ShitpostBot/test/ShitpostBot.Tests.Integration/WebApiIntegrationTests.cs`

### Step 1: Write integration test for stats command

**Add to:** `src/ShitpostBot/test/ShitpostBot.Tests.Integration/WebApiIntegrationTests.cs`

```csharp
// Add this test method to the WebApiIntegrationTests class:

[Test]
public async Task StatsCommand_ReturnsCorrectCounts()
{
    // Arrange
    var guildId = 123456789ul;
    var channelId = 987654321ul;
    var messageId = 111222333ul;
    var posterId = 444555666ul;
    
    // Create test data: 2 available ImagePosts with features, 1 LinkPost
    var imagePost1 = ImagePost.Track(
        DateTimeOffset.UtcNow.AddDays(-1),
        guildId,
        channelId,
        messageId + 1,
        posterId,
        "https://example.com/image1.jpg",
        "hash1"
    );
    imagePost1.SetImageFeatures(new float[512], DateTimeOffset.UtcNow);
    
    var imagePost2 = ImagePost.Track(
        DateTimeOffset.UtcNow.AddDays(-2),
        guildId,
        channelId,
        messageId + 2,
        posterId,
        "https://example.com/image2.jpg",
        "hash2"
    );
    imagePost2.SetImageFeatures(new float[512], DateTimeOffset.UtcNow);
    
    // ImagePost without features - should NOT be counted
    var imagePost3 = ImagePost.Track(
        DateTimeOffset.UtcNow.AddDays(-3),
        guildId,
        channelId,
        messageId + 3,
        posterId,
        "https://example.com/image3.jpg",
        "hash3"
    );
    
    var linkPost = LinkPost.Track(
        DateTimeOffset.UtcNow.AddDays(-4),
        guildId,
        channelId,
        messageId + 4,
        posterId,
        "Reddit",
        new Uri("https://reddit.com/post1")
    );
    
    await using var scope = _factory.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<IDbContext>();
    dbContext.ImagePost.Add(imagePost1);
    dbContext.ImagePost.Add(imagePost2);
    dbContext.ImagePost.Add(imagePost3);
    dbContext.LinkPost.Add(linkPost);
    await ((DbContext)dbContext).SaveChangesAsync();
    
    // Act
    var requestBody = new
    {
        guildId = guildId,
        channelId = channelId,
        messageId = messageId,
        posterId = posterId,
        command = "stats",
        arguments = Array.Empty<string>(),
        referencedMessage = (object?)null,
        edit = (object?)null
    };
    
    var response = await _client.PostAsJsonAsync("/bot-commands/execute", requestBody);
    
    // Assert
    response.EnsureSuccessStatusCode();
    
    var actions = await response.Content.ReadFromJsonAsync<BotActionDto[]>();
    Assert.That(actions, Is.Not.Null);
    Assert.That(actions!, Has.Length.EqualTo(1));
    
    var action = actions![0];
    Assert.That(action.Type, Is.EqualTo("SendMessage"));
    Assert.That(action.Message, Does.Contain("2")); // 2 ImagePosts with features
    Assert.That(action.Message, Does.Contain("1")); // 1 LinkPost
}
```

### Step 2: Run integration test

**Run:**
```bash
cd src/ShitpostBot
dotnet test --filter "FullyQualifiedName~WebApiIntegrationTests.StatsCommand_ReturnsCorrectCounts" --logger "console;verbosity=detailed"
```

**Expected Output:**
```
Test Run Successful.
Total tests: 1
     Passed: 1
```

**Note:** Integration tests use Testcontainers and may take longer to run (30-60 seconds).

### Step 3: Commit

**Run:**
```bash
git add src/ShitpostBot/test/ShitpostBot.Tests.Integration/WebApiIntegrationTests.cs
git commit -m "test: add integration test for stats command"
```

---

## Task 5: Update Documentation

**Files:**
- Modify: `README.md` (if bot commands are documented there)

### Step 1: Check if README documents bot commands

**Run:**
```bash
grep -n "bot command" README.md
```

**Expected Output:** Either finds references to commands, or no matches.

### Step 2: Update documentation if needed

**If commands are documented in README:**

Add stats command to the list of available commands:

```markdown
### Available Commands

- `@ShitpostBot about` - Display bot information and uptime
- `@ShitpostBot help` - Show all available commands
- `@ShitpostBot stats` - Display count of posts available for repost detection
- `@ShitpostBot search <query>` - Search for posts using semantic similarity
... (other commands)
```

**If no command documentation exists:** Skip this step.

### Step 3: Commit documentation changes

**Run (only if changes were made):**
```bash
git add README.md
git commit -m "docs: add stats command to README"
```

---

## Task 6: Final Verification and Cleanup

**Files:**
- Run full test suite

### Step 1: Run all unit tests

**Run:**
```bash
cd src/ShitpostBot
dotnet test --filter "Category=Unit|Category!=Integration" --logger "console;verbosity=normal"
```

**Expected Output:**
```
Test Run Successful.
Total tests: [X]
     Passed: [X]
```

### Step 2: Run all integration tests

**Run:**
```bash
cd src/ShitpostBot
dotnet test --filter "Category=Integration" --logger "console;verbosity=normal"
```

**Expected Output:**
```
Test Run Successful.
Total tests: [Y]
     Passed: [Y]
```

### Step 3: Build the solution to verify no compilation errors

**Run:**
```bash
cd src/ShitpostBot
dotnet build
```

**Expected Output:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### Step 4: Run E2E tests to ensure no regressions

**Run:**
```bash
# From repository root
./test/e2e/run-e2e-tests.sh
```

**Expected Output:**
```
All E2E tests passed
```

### Step 5: Final commit if any cleanup was needed

**Run (only if you made cleanup changes):**
```bash
git add .
git commit -m "chore: cleanup after stats command implementation"
```

---

## Summary

**Implementation Complete!** You have successfully:

1. ✅ Created `StatsBotCommandHandler` following the existing pattern
2. ✅ Implemented TDD approach with unit tests
3. ✅ Added database queries for available ImagePosts (with features) and LinkPosts
4. ✅ Manually tested the bot command in Discord
5. ✅ Added integration test coverage (optional)
6. ✅ Updated documentation
7. ✅ Verified no regressions with full test suite

**The stats command now:**
- Counts ImagePosts where `IsPostAvailable = true` AND `Image.ImageFeatures != null`
- Counts all LinkPosts (always available)
- Displays the results in a formatted Discord message
- Is automatically registered via dependency injection
- Appears in the help command output

**Next Steps:**
- Deploy the changes to your production environment
- Monitor the stats command usage in Discord
- Consider adding more detailed statistics (e.g., posts by date range, feature extraction success rate)
