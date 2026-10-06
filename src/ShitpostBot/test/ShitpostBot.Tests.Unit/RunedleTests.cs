using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShitpostBot.Application;
using ShitpostBot.Application.Features.DailySlop;
using ShitpostBot.Application.Features.DailySlop.Detectors;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;
using Xunit;

namespace ShitpostBot.Tests.Unit;

public class RunedleTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-05T12:00:00Z");

    [Theory]
    [InlineData("Runedle in 5 attempts", "https://runedle.com/", true, false, 5)]
    [InlineData("Runedle (expert) in 5 attempts", "https://runedle.com/", false, true, 5)]
    [InlineData("Runedle in 1 attempt", "https://runedle.com/", true, false, 1)]
    [InlineData("Runedle in 0 attempts", "https://runedle.com/", true, false, null)]
    [InlineData(
        "Runedle in 999999999999999999 attempts",
        "https://runedle.com/",
        true,
        false,
        null
    )]
    [InlineData("Runedle in 5 attempts", "https://runedle.com.fake/", false, false, 5)]
    [InlineData("Runedle in 5 attempts", "https://example.com/runedle.com/", false, false, 5)]
    [InlineData("Runedle in 5 attempts", "", false, false, 5)]
    public void Detect_ShareHeader_SeparatesModesAndRequiresGameDomain(
        string header,
        string url,
        bool normalMatch,
        bool expertMatch,
        int? attempts
    )
    {
        // Arrange
        var message = Message(
            $"{header}\n🟥🟥🟥🟥🟥🟥\n🟥🟥🟥🟥🟥🟥\n🟩🟥🟥🟥🟥🟥\n🟩🟩🟥🟥🟥🟥\n🟩🟩🟩🟩🟩🟩\n{url} "
        );
        var normal = new RunedleDetector("runedle", "Runedle");
        var expert = new RunedleDetector("runedle-expert", "Runedle (expert)");

        // Act / Assert
        normal.Matches(message).Should().Be(normalMatch);
        expert.Matches(message).Should().Be(expertMatch);
        var score = (header.Contains("(expert)") ? expert : normal).ExtractScore(message);
        if (attempts is { } expected)
        {
            score.Should().BeOfType<RunedleScore>().Which.Attempts.Should().Be(expected);
        }
        else
        {
            score.Should().BeNull();
        }
    }

    [Theory]
    [InlineData("", "runedle")]
    [InlineData(" (expert)", "runedle-expert")]
    public async Task TrackAndList_RunedleModes_PersistsAttemptsAndRanksLowerFirst(
        string mode,
        string game
    )
    {
        // Arrange
        using var services = new ServiceCollection()
            .AddShitpostBotApplication(new ConfigurationBuilder().Build())
            .BuildServiceProvider();
        var detectors = services.GetServices<IDailySlopDetector>().ToArray();
        var strategies = services.GetServices<IDailySlopScoringStrategy>().ToArray();
        using var db = new DailySlopTestDbContext();
        var feature = new DailySlopFeature(detectors, db, db, new Clock(), strategies);

        // Act
        foreach (var (user, attempts) in new[] { (1UL, 5), (2UL, 2), (3UL, 12) })
        {
            var handled = await feature.TryHandleCreate(
                Message(
                    $"Runedle{mode} in {attempts} attempts\n🟥🟥🟥🟥🟥🟥\n🟩🟩🟩🟩🟩🟩\nhttps://runedle.com/ ",
                    user
                ),
                default
            );
            handled.Should().BeTrue();
        }
        db.ChangeTracker.Clear();
        db.DailySlopEntry.Should()
            .HaveCount(3)
            .And.OnlyContain(e => e.GameId == game && e.Score != null);
        var chat = new DailySlopTestChatClient();
        var command = new DailySlopLeaderboardCommand(db, chat, new Clock(), detectors, strategies);
        await command.TryHandleCreate(Message($"<@42> dailyslop {game}"), default);

        // Assert
        var response = chat.Messages.Should().ContainSingle().Subject;
        response
            .Split('\n')
            .Skip(1)
            .Should()
            .Equal(
                "1️⃣ 2 attempts - User 2 https://discord.com/channels/1/1/2",
                "2️⃣ 5 attempts - User 1 https://discord.com/channels/1/1/1",
                "3️⃣ 12 attempts - User 3 https://discord.com/channels/1/1/3"
            );
        response
            .Should()
            .Contain("2 attempts")
            .And.Contain("5 attempts")
            .And.Contain("12 attempts");

        var summaryChat = new DailySlopTestChatClient();
        var summary = new DailySlopCommand(db, summaryChat, new Clock(), detectors);
        await summary.TryHandleCreate(Message("<@42> dailyslop"), default);
        summaryChat.Messages.Should().ContainSingle().Which.Should().Be("No daily slop today.");
    }

    [Theory]
    [InlineData("dailyslop")]
    [InlineData("daily")]
    public async Task Summary_ExcludedGames_DoNotAffectListingOrUserOrder(string alias)
    {
        // Arrange
        using var db = new DailySlopTestDbContext();
        db.DailySlopEntry.AddRange(
            Entry(1, "travle"),
            Entry(1, "optional"),
            Entry(1, "optional"),
            Entry(2, "travle"),
            Entry(2, "globle"),
            Entry(3, "optional")
        );
        await db.SaveChangesAsync();
        var chat = new DailySlopTestChatClient();
        var command = new DailySlopCommand(
            db,
            chat,
            new Clock(),
            [new TravleDetector(), new GlobleDetector(), new OptionalDetector()]
        );

        // Act
        await command.TryHandleCreate(Message($"<@42> {alias}"), default);

        // Assert
        var response = chat.Messages.Should().ContainSingle().Subject;
        response.Should().NotContain("optional").And.NotContain("User 3");
        response
            .IndexOf("User 2", StringComparison.Ordinal)
            .Should()
            .BeLessThan(response.IndexOf("User 1", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Summary_OnlyExcludedGames_ReportsNoDailySlop()
    {
        using var db = new DailySlopTestDbContext();
        db.DailySlopEntry.Add(Entry(1, "optional"));
        await db.SaveChangesAsync();
        var chat = new DailySlopTestChatClient();
        var command = new DailySlopCommand(db, chat, new Clock(), [new OptionalDetector()]);

        await command.TryHandleCreate(Message("<@42> dailyslop"), default);

        chat.Messages.Should().ContainSingle().Which.Should().Be("No daily slop today.");
    }

    private static DailySlopEntry Entry(ulong user, string game) =>
        new(user, game, Now, Now, 1, 1, user, null);

    private static IncomingMessage Message(string content, ulong user = 999) =>
        new(new MessageIdentification(1, 1, user, user), null, content, [], [], Now);

    private sealed class Clock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => RunedleTests.Now;
        public DateTimeOffset Now => UtcNow;
    }

    private sealed class OptionalDetector : IDailySlopDetector
    {
        public string GameId => "optional";
        public bool IsHidden => true;

        public bool Matches(IncomingMessage msg) => false;
    }
}
