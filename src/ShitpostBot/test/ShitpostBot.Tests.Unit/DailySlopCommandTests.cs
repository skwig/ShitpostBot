using System.Text.Json;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ShitpostBot.Application.Features.DailySlop;
using ShitpostBot.Application.Features.DailySlop.Detectors;
using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;
using Xunit;

namespace ShitpostBot.Tests.Unit;

public class DailySlopCommandTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-05T12:00:00Z");

    [Theory]
    [InlineData("dailyslop")]
    [InlineData("daily")]
    public async Task TargetedCommand_NoGameName_LeavesMessageForSummaryCommand(string text)
    {
        // Arrange
        using var db = new DailySlopTestDbContext();
        var chat = new DailySlopTestChatClient();

        // Act
        var handled = await CreateCommand(db, chat).TryHandleCreate(Message(text), default);

        // Assert
        handled.Should().BeFalse();
        chat.Messages.Should().BeEmpty();

        var summary = new DailySlopCommand(db, chat, new FixedClock(), [new RngdleDetector()]);
        var summaryHandler =
            text == "daily"
                ? (BotCommandFeature)new CommandAlias(chat, summary, "daily", "dailyslop")
                : summary;
        (await summaryHandler.TryHandleCreate(Message(text), default)).Should().BeTrue();
        chat.Messages.Should().ContainSingle().Which.Should().Be("No daily slop today.");
    }

    [Fact]
    public async Task Track_RngdleResult_StoresConcreteDomainScore()
    {
        // Arrange
        using var db = new DailySlopTestDbContext();
        var feature = new DailySlopFeature(
            [new RngdleDetector()],
            db,
            db,
            new FixedClock(),
            [new RngdleDetector()]
        );

        // Act
        await Track(feature, 1, 101, "11,887 EP", Now);
        db.ChangeTracker.Clear();
        var entry = await db.DailySlopEntry.SingleAsync();

        // Assert
        entry.Score.Should().BeOfType<RngdleScore>().Which.Ep.Should().Be(11887);
    }

    [Fact]
    public async Task TargetedCommand_ManyUsers_SendsEveryResultWithinDiscordMessageLimit()
    {
        // Arrange
        using var db = new DailySlopTestDbContext();
        for (ulong user = 1; user <= 60; user++)
        {
            db.DailySlopEntry.Add(Entry(user, 1000 + user, "travle", Now));
        }
        await db.SaveChangesAsync();
        var chat = new DailySlopTestChatClient();

        // Act
        await new CommandAlias(
            chat,
            CreateCommand(db, chat),
            "daily",
            "dailyslop",
            forwardArguments: true
        ).TryHandleCreate(Message("daily travle"), default);

        // Assert
        chat.Messages.Should()
            .AllSatisfy(message => message.Length.Should().BeLessThanOrEqualTo(2000));
        var output = string.Join('\n', chat.Messages);
        for (ulong user = 1; user <= 60; user++)
        {
            output.Should().Contain($"User {user} https://discord.com/channels/1/1/{1000 + user}");
        }
        output.Should().Contain("🔟 User 10 https://discord.com/channels/1/1/1010");
        output.Should().Contain("6️⃣0️⃣ User 60 https://discord.com/channels/1/1/1060");
    }

    [Fact]
    public async Task TargetedCommand_UnscoredDuplicates_SelectsNewestThenSortsOldestFirst()
    {
        // Arrange: user 1's older submission must not improve their position.
        using var db = new DailySlopTestDbContext();
        db.DailySlopEntry.AddRange(
            Entry(1, 101, "travle", Now.AddHours(-3)),
            Entry(1, 102, "travle", Now.AddHours(-1)),
            Entry(2, 201, "travle", Now.AddHours(-2)),
            Entry(3, 301, "globle", Now),
            Entry(4, 401, "travle", Now.AddDays(-1))
        );
        await db.SaveChangesAsync();
        var chat = new DailySlopTestChatClient();
        var command = CreateCommand(db, chat);

        // Act
        var handled = await command.TryHandleCreate(Message("dailyslop travle"), default);

        // Assert
        handled.Should().BeTrue();
        var response = chat.Messages.Should().ContainSingle().Subject;
        response.Should().Contain("User 2").And.Contain("User 1");
        response
            .IndexOf("User 2", StringComparison.Ordinal)
            .Should()
            .BeLessThan(response.IndexOf("User 1", StringComparison.Ordinal));
        response
            .Should()
            .Contain("/102")
            .And.NotContain("/101")
            .And.NotContain("User 3")
            .And.NotContain("User 4");
    }

    [Theory]
    [InlineData("dailyslop missing", "does not exist", "rngdle")]
    [InlineData("dailyslop rngdle", "No entries today", "rngdle")]
    public async Task TargetedCommand_UnknownOrEmptyGame_ExplainsResult(
        string text,
        string expected,
        string game
    )
    {
        // Arrange
        using var db = new DailySlopTestDbContext();
        var chat = new DailySlopTestChatClient();
        var command = CreateCommand(db, chat);

        // Act
        var handled = await command.TryHandleCreate(Message(text), default);

        // Assert
        handled.Should().BeTrue();
        chat.Messages.Should().ContainSingle().Which.Should().Contain(expected).And.Contain(game);
    }

    [Fact]
    public async Task TargetedCommand_ScoredDuplicates_SelectsWorstWithNewerTiesAndMissingLast()
    {
        // Arrange: higher EP is better; missing EP defeats even a scored submission.
        using var db = new DailySlopTestDbContext();
        var feature = new DailySlopFeature(
            [new RngdleDetector()],
            db,
            db,
            new FixedClock(),
            [new RngdleDetector()]
        );
        await Track(feature, 1, 101, "100 EP", Now.AddHours(-3));
        await Track(feature, 1, 102, "20,000 EP", Now.AddHours(-1));
        await Track(feature, 2, 201, "11,887 EP", Now.AddHours(-2));
        await Track(feature, 3, 301, "12 EP", Now.AddHours(-3));
        await Track(feature, 3, 302, "12 EP", Now.AddHours(-2));
        await Track(feature, 4, 401, "20,000 EP", Now.AddHours(-1));
        await Track(feature, 4, 402, "", Now.AddHours(-4));
        var chat = new DailySlopTestChatClient();

        // Act
        await new CommandAlias(
            chat,
            CreateCommand(db, chat),
            "daily",
            "dailyslop",
            forwardArguments: true
        ).TryHandleCreate(Message("daily RNGdle"), default);

        // Assert
        var response = chat.Messages.Should().ContainSingle().Subject;
        response
            .Split('\n')
            .Skip(1)
            .Should()
            .Equal(
                "1️⃣ 11,887 EP - User 2 https://discord.com/channels/1/1/201",
                "2️⃣ 100 EP - User 1 https://discord.com/channels/1/1/101",
                "3️⃣ 12 EP - User 3 https://discord.com/channels/1/1/302",
                "4️⃣ score unavailable - User 4 https://discord.com/channels/1/1/402"
            );
        response
            .Should()
            .Contain("11,887 EP")
            .And.Contain("100 EP")
            .And.Contain("12 EP")
            .And.Contain("score unavailable");
        response.Should().Contain("/101").And.Contain("/302").And.Contain("/402");
        response.Should().NotContain("/102").And.NotContain("/301").And.NotContain("/401");
    }

    [Theory]
    [InlineData("11,887 EP", "11,887 EP")]
    [InlineData("11887 EP", "11,887 EP")]
    [InlineData("6 481 EP", "6,481 EP")]
    [InlineData("6\u00A0481 EP", "6,481 EP")]
    [InlineData("6\u202F481 EP", "6,481 EP")]
    [InlineData("1 234 567 EP", "1,234,567 EP")]
    [InlineData("0 EP", "0 EP")]
    [InlineData("", "score unavailable")]
    [InlineData("11,88 EP", "1,188 EP")]
    [InlineData("6 48 EP", "648 EP")]
    [InlineData("-100 EP", "score unavailable")]
    [InlineData("1.5 EP", "score unavailable")]
    [InlineData("999999999999999999999999999 EP", "score unavailable")]
    public async Task TargetedCommand_RngdleScore_ParsesEpOrReportsUnavailable(
        string ep,
        string expected
    )
    {
        // Arrange
        using var db = new DailySlopTestDbContext();
        var feature = new DailySlopFeature(
            [new RngdleDetector()],
            db,
            db,
            new FixedClock(),
            [new RngdleDetector()]
        );
        await Track(feature, 1, 101, ep, Now);
        db.ChangeTracker.Clear();
        var chat = new DailySlopTestChatClient();

        // Act
        await CreateCommand(db, chat).TryHandleCreate(Message("dailyslop rngdle"), default);

        // Assert
        chat.Messages.Should().ContainSingle().Which.Should().Contain(expected);
    }

    private static Task<bool> Track(
        DailySlopFeature feature,
        ulong user,
        ulong message,
        string ep,
        DateTimeOffset posted
    ) =>
        feature.TryHandleCreate(
            new IncomingMessage(
                new MessageIdentification(1, 1, user, message),
                null,
                $"RNGdle 🎲 32264\n\n🟦 RARE • Top 22%\n\n{ep}\nhttps://rngdle.com/",
                [],
                [],
                posted
            ),
            default
        );

    [Theory]
    [InlineData("2026-03-29T12:00:00Z", "2026-03-28T23:00:00Z", "2026-03-29T22:00:00Z")]
    [InlineData("2026-10-25T12:00:00Z", "2026-10-24T22:00:00Z", "2026-10-25T23:00:00Z")]
    public async Task TargetedCommand_DaylightSavingDay_UsesLocalMidnightBoundaries(
        string now,
        string start,
        string end
    )
    {
        // Arrange: DST days are 23 or 25 hours, not always 24.
        using var db = new DailySlopTestDbContext();
        var dayStart = DateTimeOffset.Parse(start);
        var dayEnd = DateTimeOffset.Parse(end);
        db.DailySlopEntry.AddRange(
            Entry(1, 101, "travle", dayStart.AddTicks(-1)),
            Entry(2, 201, "travle", dayStart),
            Entry(3, 301, "travle", dayEnd.AddTicks(-1)),
            Entry(4, 401, "travle", dayEnd)
        );
        await db.SaveChangesAsync();
        var chat = new DailySlopTestChatClient();
        var command = new DailySlopLeaderboardCommand(
            db,
            chat,
            new FixedClock(DateTimeOffset.Parse(now)),
            [new TravleDetector()],
            []
        );

        // Act
        await new CommandAlias(
            chat,
            command,
            "daily",
            "dailyslop",
            forwardArguments: true
        ).TryHandleCreate(Message("daily travle"), default);

        // Assert
        chat.Messages.Should()
            .ContainSingle()
            .Which.Should()
            .Contain("User 2")
            .And.Contain("User 3")
            .And.NotContain("User 1")
            .And.NotContain("User 4");
    }

    [Fact]
    public async Task TargetedCommand_LegacyEntryWithoutScore_SelectsItOverScoredEntry()
    {
        // Arrange
        using var db = new DailySlopTestDbContext();
        db.DailySlopEntry.Add(Entry(1, 101, "rngdle", Now.AddHours(-2)));
        await db.SaveChangesAsync();
        var feature = new DailySlopFeature(
            [new RngdleDetector()],
            db,
            db,
            new FixedClock(),
            [new RngdleDetector()]
        );
        await Track(feature, 1, 102, "100 EP", Now);
        var chat = new DailySlopTestChatClient();

        // Act
        await CreateCommand(db, chat).TryHandleCreate(Message("dailyslop rngdle"), default);

        // Assert
        chat.Messages.Should()
            .ContainSingle()
            .Which.Should()
            .Contain("score unavailable")
            .And.Contain("/101")
            .And.NotContain("/102");
    }

    [Fact]
    public async Task TargetedCommand_CustomCompositeStrategy_UsesGameSpecificComparison()
    {
        // Arrange: fewer attempts wins; time breaks attempt ties.
        using var db = new CompositeScoreTestDbContext();
        db.DailySlopEntry.AddRange(
            new DailySlopEntry(1, "travle", Now, Now, 1, 1, 101, new CompositeScore(2, 10)),
            new DailySlopEntry(
                1,
                "travle",
                Now.AddHours(-1),
                Now,
                1,
                1,
                102,
                new CompositeScore(3, 5)
            ),
            new DailySlopEntry(2, "travle", Now, Now, 1, 1, 201, new CompositeScore(2, 20)),
            new DailySlopEntry(3, "travle", Now, Now, 1, 1, 301, new CompositeScore(2, 10))
        );
        await db.SaveChangesAsync();
        var chat = new DailySlopTestChatClient();
        var command = new DailySlopLeaderboardCommand(
            db,
            chat,
            new FixedClock(),
            [new TravleDetector()],
            [new CompositeScoringStrategy()]
        );

        // Act
        await command.TryHandleCreate(Message("dailyslop travle"), default);

        // Assert
        var response = chat.Messages.Should().ContainSingle().Subject;
        response
            .Split('\n')
            .Skip(1)
            .Should()
            .Equal(
                "1️⃣ 2 attempts, 10s - User 3 https://discord.com/channels/1/1/301",
                "2️⃣ 2 attempts, 20s - User 2 https://discord.com/channels/1/1/201",
                "3️⃣ 3 attempts, 5s - User 1 https://discord.com/channels/1/1/102"
            );
        response.Should().Contain("3 attempts, 5s").And.Contain("/102").And.NotContain("/101");
    }

    private sealed record CompositeScore(int Attempts, int Seconds) : DailySlopScore;

    private sealed class CompositeScoreTestDbContext : DailySlopTestDbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder
                .Entity<DailySlopEntry>()
                .Property(e => e.Score)
                .HasConversion(
                    score =>
                        JsonSerializer.Serialize(
                            (CompositeScore)score!,
                            (JsonSerializerOptions?)null
                        ),
                    json =>
                        JsonSerializer.Deserialize<CompositeScore>(
                            json,
                            (JsonSerializerOptions?)null
                        )
                );
        }
    }

    private sealed class CompositeScoringStrategy : IDailySlopScoringStrategy
    {
        public string GameId => "travle";

        public DailySlopScore? ExtractScore(IncomingMessage message) =>
            throw new NotSupportedException();

        public int Compare(DailySlopScore left, DailySlopScore right)
        {
            var leftScore = (CompositeScore)left;
            var rightScore = (CompositeScore)right;
            var attempts = leftScore.Attempts.CompareTo(rightScore.Attempts);
            return attempts != 0 ? attempts : leftScore.Seconds.CompareTo(rightScore.Seconds);
        }

        public string FormatScore(DailySlopScore score) =>
            $"{((CompositeScore)score).Attempts} attempts, {((CompositeScore)score).Seconds}s";
    }

    private static DailySlopEntry Entry(
        ulong user,
        ulong message,
        string game,
        DateTimeOffset posted
    ) => new(user, game, posted, Now, 1, 1, message, null);

    private static DailySlopLeaderboardCommand CreateCommand(
        DailySlopTestDbContext db,
        DailySlopTestChatClient chat
    ) =>
        new(
            db,
            chat,
            new FixedClock(),
            [new TravleDetector(), new GlobleDetector(), new RngdleDetector()],
            [new RngdleDetector()]
        );

    private static IncomingMessage Message(string command) =>
        new(new MessageIdentification(1, 1, 999, 99), null, $"<@42> {command}", [], [], Now);

    private sealed class FixedClock(DateTimeOffset? utcNow = null) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => utcNow ?? DailySlopCommandTests.Now;
        public DateTimeOffset Now => UtcNow;
    }
}

internal class DailySlopTestDbContext()
    : DbContext(
        new DbContextOptionsBuilder().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options
    ),
        IDbContext,
        IUnitOfWork
{
    public DbSet<DailySlopEntry> DailySlopEntry => Set<DailySlopEntry>();
    DbSet<Post> IDbContext.Post => throw new NotSupportedException();
    DbSet<ImagePost> IDbContext.ImagePost => throw new NotSupportedException();
    DbSet<LinkPost> IDbContext.LinkPost => throw new NotSupportedException();
    DbSet<WhitelistedPost> IDbContext.WhitelistedPost => throw new NotSupportedException();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<Post>();
        modelBuilder.Ignore<ImagePost>();
        modelBuilder.Ignore<LinkPost>();
        modelBuilder.Ignore<WhitelistedPost>();
        modelBuilder.ApplyConfiguration(new DailySlopEntryConfiguration());
    }
}

internal sealed class DailySlopTestChatClient : IChatClient, IChatClientUtils
{
    public List<string> Messages { get; } = [];
    public ulong? ReplyMessageId { get; init; }
    public IChatClientUtils Utils => this;

    public ulong ShitpostBotId() => 42;

    public string Mention(ulong posterId, bool useDesktop = false) => $"<@{posterId}>";

    public Task<string?> GetMemberDisplayNameAsync(ulong guildId, ulong posterId) =>
        Task.FromResult<string?>($"User {posterId}");

    public Task SendMessage(MessageDestination destination, string? messageContent)
    {
        Messages.Add(messageContent!);
        return Task.CompletedTask;
    }

    public string Emoji(string name) => throw new NotSupportedException();

    public string RelativeTimestamp(DateTimeOffset timestamp) => throw new NotSupportedException();

    public Task ConnectAsync() => throw new NotSupportedException();

    public event AsyncEventHandler<MessageCreateEventArgs> MessageCreated
    {
        add { }
        remove { }
    }
    public event AsyncEventHandler<MessageDeleteEventArgs> MessageDeleted
    {
        add { }
        remove { }
    }
    public event AsyncEventHandler<MessageUpdateEventArgs> MessageUpdated
    {
        add { }
        remove { }
    }

    public Task SendMessage(MessageDestination destination, DiscordMessageBuilder messageBuilder) =>
        throw new NotSupportedException();

    public Task SendEmbeddedMessage(MessageDestination destination, DiscordEmbed embed) =>
        throw new NotSupportedException();

    public Task React(MessageIdentification messageIdentification, string emoji) =>
        throw new NotSupportedException();

    public Task<FetchedMessage?> GetMessageWithAttachmentsAsync(
        MessageIdentification messageIdentification
    ) => throw new NotSupportedException();

    public Task<ulong?> FindReplyToMessage(MessageIdentification replyToMessage) =>
        Task.FromResult(ReplyMessageId);

    public Task<bool> UpdateMessage(
        MessageIdentification messageToUpdate,
        DiscordMessageBuilder newContent
    ) => throw new NotSupportedException();
}
