using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShitpostBot.Application;
using ShitpostBot.Application.Features.DailySlop;
using ShitpostBot.Infrastructure;
using Xunit;

namespace ShitpostBot.Tests.Unit;

public class OriginleDetectorTests
{
    [Theory]
    [InlineData("10", "6,250", "6", "tugi", "6,250")]
    [InlineData("9", "3,000", "8", "bela", "3,000")]
    [InlineData("11", "0", "8", "test", "0")]
    public void RegisteredDetector_SharedResult_ExtractsAndPersistsOnlyPoints(
        string day,
        string points,
        string clues,
        string word,
        string formatted
    )
    {
        // Arrange
        using var provider = CreateProvider();
        var detector = GetDetector(provider);
        var scoring = provider
            .GetServices<IDailySlopScoringStrategy>()
            .Single(s => s.GameId == "originle");
        var message = Message(
            $"Originle #{day} · {points}\n🟩 · {clues}/8 clues\nhttps://originle.io/{word}/?day={day} "
        );

        // Act
        var score = scoring.ExtractScore(message);
        using var db = new DailySlopTestDbContext();
        var converter = db
            .Model.FindEntityType(typeof(ShitpostBot.Domain.DailySlopEntry))!
            .FindProperty("Score")!
            .GetValueConverter()!;
        var json = (string)converter.ConvertToProvider(score)!;
        var restored = (ShitpostBot.Domain.DailySlopScore)converter.ConvertFromProvider(json)!;

        // Assert
        detector.Matches(message).Should().BeTrue();
        scoring.FormatScore(restored).Should().Be(formatted);
        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("type").GetString().Should().Be("originle");
        document.RootElement.EnumerateObject().Should().HaveCount(2);
    }

    [Fact]
    public void RegisteredScoring_ComparesOnlyPoints_HigherFirstAndIgnoresClues()
    {
        // Arrange
        using var provider = CreateProvider();
        GetDetector(provider);
        var scoring = provider
            .GetServices<IDailySlopScoringStrategy>()
            .Single(s => s.GameId == "originle");
        var higher = scoring.ExtractScore(Message("Originle #10 · 6,250\n🟩 · 8/8 clues"))!;
        var lower = scoring.ExtractScore(Message("Originle #10 · 3,000\n🟩 · 1/8 clues"))!;
        var tied = scoring.ExtractScore(Message("Originle #10 · 6,250\n🟩 · 1/8 clues"))!;

        // Act / Assert
        scoring.Compare(higher, lower).Should().BeNegative();
        scoring.Compare(lower, higher).Should().BePositive();
        scoring.Compare(higher, tied).Should().Be(0);
    }

    [Theory]
    [InlineData("https://originle.io/tugi/?day=10", true)]
    [InlineData("https://originle.io/bela/?day=9", true)]
    [InlineData("https://originle.io.evil.example/tugi/", false)]
    [InlineData("https://fake-originle.io/tugi/", false)]
    [InlineData("Originle #10 · 6,250", false)]
    public void RegisteredDetector_ContentUrl_ValidatesHost(string content, bool expected)
    {
        using var provider = CreateProvider();
        GetDetector(provider).Matches(Message(content)).Should().Be(expected);
    }

    [Fact]
    public void RegisteredDetector_EmbedOnly_TracksWithoutScore()
    {
        using var provider = CreateProvider();
        var detector = GetDetector(provider);
        var message = Message(null) with
        {
            Embeds = [new Embed(new Uri("https://originle.io/tugi/?day=10"))],
        };
        detector.Matches(message).Should().BeTrue();
        provider
            .GetServices<IDailySlopScoringStrategy>()
            .Single(s => s.GameId == "originle")
            .ExtractScore(message)
            .Should()
            .BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Originle #10\n🟩 · 6/8 clues")]
    [InlineData("Originle #10 · invalid\n🟩 · 6/8 clues")]
    [InlineData("Originle #10 · 999999999999999999999999999")]
    public void RegisteredScoring_MissingOrInvalidPoints_ReturnsNull(string? content)
    {
        using var provider = CreateProvider();
        GetDetector(provider);
        provider
            .GetServices<IDailySlopScoringStrategy>()
            .Single(s => s.GameId == "originle")
            .ExtractScore(Message(content))
            .Should()
            .BeNull();
    }

    private static ServiceProvider CreateProvider() =>
        new ServiceCollection()
            .AddShitpostBotApplication(new ConfigurationBuilder().Build())
            .BuildServiceProvider();

    private static IDailySlopDetector GetDetector(ServiceProvider provider)
    {
        var detectors = provider
            .GetServices<IDailySlopDetector>()
            .Where(d => d.GameId == "originle");
        return detectors
            .Should()
            .ContainSingle("Originle must be registered for daily slop")
            .Subject;
    }

    private static IncomingMessage Message(string? content) =>
        new(new MessageIdentification(1, 1, 1, 1), null, content, [], [], DateTimeOffset.UtcNow);
}
