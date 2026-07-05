using FluentAssertions;
using ShitpostBot.Application.Features.DailySlop;
using ShitpostBot.Application.Features.DailySlop.Detectors;
using ShitpostBot.Infrastructure;
using Xunit;

namespace ShitpostBot.Tests.Unit;

public class DailySlopDetectorTests
{
    [Fact]
    public void TravleDetector_Matches_ReturnsTrueForValidMessage()
    {
        var msg = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            "#travle 🗺️ Daily travle 123 - 4/5 guesses\nhttps://travle.earth/123",
            [],
            [new Embed(new Uri("https://travle.earth/123"))],
            DateTimeOffset.UtcNow
        );
        var detector = new TravleDetector();

        var result = detector.Matches(msg);

        result.Should().BeTrue();
    }

    [Fact]
    public void TravleDetector_LinkOnly_ReturnsFalse()
    {
        var msg = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            null,
            [],
            [new Embed(new Uri("https://travle.earth/123"))],
            DateTimeOffset.UtcNow
        );
        var detector = new TravleDetector();

        var result = detector.Matches(msg);

        result.Should().BeFalse();
    }

    [Fact]
    public void GlobleDetector_Matches_ReturnsTrueForValidMessage()
    {
        var msg = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            "#globle 🌎 I guessed the country in 4 tries!\nhttps://globle-game.com/game/123",
            [],
            [new Embed(new Uri("https://globle-game.com/game/123"))],
            DateTimeOffset.UtcNow
        );
        var detector = new GlobleDetector();

        var result = detector.Matches(msg);

        result.Should().BeTrue();
    }

    [Fact]
    public void GlobleDetector_LinkOnly_ReturnsFalse()
    {
        var msg = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            null,
            [],
            [new Embed(new Uri("https://globle-game.com/game/123"))],
            DateTimeOffset.UtcNow
        );
        var detector = new GlobleDetector();

        var result = detector.Matches(msg);

        result.Should().BeFalse();
    }

    [Fact]
    public void MaptapDetector_Matches_ReturnsTrueForValidMessage()
    {
        var msg = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            "www.maptap.gg - Final score: 3200 - Round 5/10 🗺️",
            [],
            [new Embed(new Uri("https://www.maptap.gg/game/123"))],
            DateTimeOffset.UtcNow
        );
        var detector = new MaptapDetector();

        var result = detector.Matches(msg);

        result.Should().BeTrue();
    }

    [Fact]
    public void MaptapDetector_LinkOnly_ReturnsFalse()
    {
        var msg = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            null,
            [],
            [new Embed(new Uri("https://www.maptap.gg/game/123"))],
            DateTimeOffset.UtcNow
        );
        var detector = new MaptapDetector();

        var result = detector.Matches(msg);

        result.Should().BeFalse();
    }

    [Fact]
    public void CutleDetector_Matches_ReturnsTrueForValidMessage()
    {
        var msg = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            "Cutle #42 - 3/6 guesses\n🔴🟡🟢\nhttps://pfiffel.com/cutle/42",
            [],
            [new Embed(new Uri("https://pfiffel.com/cutle/42"))],
            DateTimeOffset.UtcNow
        );
        var detector = new CutleDetector();

        var result = detector.Matches(msg);

        result.Should().BeTrue();
    }

    [Fact]
    public void CutleDetector_LinkOnly_ReturnsFalse()
    {
        var msg = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            null,
            [],
            [new Embed(new Uri("https://pfiffel.com/cutle/42"))],
            DateTimeOffset.UtcNow
        );
        var detector = new CutleDetector();

        var result = detector.Matches(msg);

        result.Should().BeFalse();
    }

    [Fact]
    public void FoodguessrDetector_Matches_ReturnsTrueForValidMessage()
    {
        var msg = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            "🍔 Foodguessr - 2025-01-15 - Score: 4500\nI identified all dishes correctly!",
            [],
            [new Embed(new Uri("https://www.foodguessr.com/game/daily"))],
            DateTimeOffset.UtcNow
        );
        var detector = new FoodguessrDetector();

        var result = detector.Matches(msg);

        result.Should().BeTrue();
    }

    [Fact]
    public void FoodguessrDetector_Matches_ReturnsFalseForPlateOff()
    {
        var msg = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            "🍔 Foodguessr Plate-Off - 2025-01-15 - Score: 3200",
            [],
            [new Embed(new Uri("https://www.foodguessr.com/game/plate-off/daily"))],
            DateTimeOffset.UtcNow
        );
        var detector = new FoodguessrDetector();

        var result = detector.Matches(msg);

        result.Should().BeFalse();
    }

    [Fact]
    public void FoodguessrDetector_LinkOnly_ReturnsFalse()
    {
        var msg = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            null,
            [],
            [new Embed(new Uri("https://www.foodguessr.com/game/daily"))],
            DateTimeOffset.UtcNow
        );
        var detector = new FoodguessrDetector();

        var result = detector.Matches(msg);

        result.Should().BeFalse();
    }

    [Fact]
    public void PlateOffDetector_Matches_ReturnsTrueForValidMessageWithEmbed()
    {
        var msg = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            null,
            [],
            [new Embed(new Uri("https://www.foodguessr.com/game/plate-off/daily"))],
            DateTimeOffset.UtcNow
        );
        var detector = new PlateOffDetector();

        var result = detector.Matches(msg);

        result.Should().BeTrue();
    }

    [Fact]
    public void PlateOffDetector_Matches_ReturnsTrueForContentWithPlateOffUrl()
    {
        var msg = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            "Check out my foodguessr.com/plate-off results! Score: 2800",
            [],
            [],
            DateTimeOffset.UtcNow
        );
        var detector = new PlateOffDetector();

        var result = detector.Matches(msg);

        result.Should().BeTrue();
    }

    [Fact]
    public void PlateOffDetector_LinkOnly_ReturnsFalse()
    {
        var msg = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            null,
            [],
            [],
            DateTimeOffset.UtcNow
        );
        var detector = new PlateOffDetector();

        var result = detector.Matches(msg);

        result.Should().BeFalse();
    }

    [Fact]
    public void KindahardGolfDetector_Matches_ReturnsTrueForValidMessage()
    {
        var msg = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            "kindahard.golf - Hole 7 - Par 4 - Score: 3 🏌️\nhttps://kindahard.golf/game/abc",
            [],
            [new Embed(new Uri("https://kindahard.golf/game/abc"))],
            DateTimeOffset.UtcNow
        );
        var detector = new KindahardGolfDetector();

        var result = detector.Matches(msg);

        result.Should().BeTrue();
    }

    [Fact]
    public void KindahardGolfDetector_LinkOnly_ReturnsFalse()
    {
        var msg = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            null,
            [],
            [new Embed(new Uri("https://kindahard.golf/game/abc"))],
            DateTimeOffset.UtcNow
        );
        var detector = new KindahardGolfDetector();

        var result = detector.Matches(msg);

        result.Should().BeFalse();
    }

    [Fact]
    public void ScrandleDetector_Matches_ReturnsTrueForValidMessage()
    {
        var msg = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            "scrandle.com - #42 - 4/6 - 🟩🟨⬜\nhttps://scrandle.com/game/42",
            [],
            [new Embed(new Uri("https://scrandle.com/game/42"))],
            DateTimeOffset.UtcNow
        );
        var detector = new ScrandleDetector();

        var result = detector.Matches(msg);

        result.Should().BeTrue();
    }

    [Fact]
    public void ScrandleDetector_LinkOnly_ReturnsFalse()
    {
        var msg = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            null,
            [],
            [new Embed(new Uri("https://scrandle.com/game/42"))],
            DateTimeOffset.UtcNow
        );
        var detector = new ScrandleDetector();

        var result = detector.Matches(msg);

        result.Should().BeFalse();
    }
}
