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
        // Arrange
        var msg = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            """
            #travle #1299 +0 (Perfect)
            ✅✅✅✅
            https://travle.earth/
            """,
            [],
            [new Embed(new Uri("https://travle.earth/"))],
            DateTimeOffset.UtcNow
        );
        var detector = new TravleDetector();

        // Act
        var result = detector.Matches(msg);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void TravleDetector_LinkOnly_ReturnsFalse()
    {
        // Arrange
        var msg = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            null,
            [],
            [new Embed(new Uri("https://travle.earth/"))],
            DateTimeOffset.UtcNow
        );
        var detector = new TravleDetector();

        // Act
        var result = detector.Matches(msg);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GlobleDetector_Matches_ReturnsTrueForValidMessage()
    {
        // Arrange
        var msg = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            """
            🌎 Jul 5, 2026 🌍
            🔥 1 | Avg. Guesses: 2
            🟥🟩 = 2

            https://globle-game.com/
            #globle
            """,
            [],
            [new Embed(new Uri("https://globle-game.com/"))],
            DateTimeOffset.UtcNow
        );
        var detector = new GlobleDetector();

        // Act
        var result = detector.Matches(msg);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GlobleDetector_LinkOnly_ReturnsFalse()
    {
        // Arrange
        var msg = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            null,
            [],
            [new Embed(new Uri("https://globle-game.com/"))],
            DateTimeOffset.UtcNow
        );
        var detector = new GlobleDetector();

        // Act
        var result = detector.Matches(msg);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void MaptapDetector_Matches_ReturnsTrueForValidMessage()
    {
        // Arrange
        var msg = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            """
            www.maptap.gg July 5
            97🔥 93🏆 91👑 83😁 48😟
            Final score: 765
            """,
            [],
            [new Embed(new Uri("https://maptap.gg/"))],
            DateTimeOffset.UtcNow
        );
        var detector = new MaptapDetector();

        // Act
        var result = detector.Matches(msg);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void MaptapDetector_LinkOnly_ReturnsFalse()
    {
        // Arrange
        var msg = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            null,
            [],
            [new Embed(new Uri("https://maptap.gg/"))],
            DateTimeOffset.UtcNow
        );
        var detector = new MaptapDetector();

        // Act
        var result = detector.Matches(msg);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CutleDetector_Matches_ReturnsTrueForValidMessage()
    {
        // Arrange
        var msg = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            "Cutle #224: ⬜ 46:54 ⬜ (2026-07-05) - https://pfiffel.com/cutle",
            [],
            [new Embed(new Uri("https://pfiffel.com/cutle"))],
            DateTimeOffset.UtcNow
        );
        var detector = new CutleDetector();

        // Act
        var result = detector.Matches(msg);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CutleDetector_LinkOnly_ReturnsFalse()
    {
        // Arrange
        var msg = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            null,
            [],
            [new Embed(new Uri("https://pfiffel.com/cutle"))],
            DateTimeOffset.UtcNow
        );
        var detector = new CutleDetector();

        // Act
        var result = detector.Matches(msg);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void FoodguessrDetector_Matches_ReturnsTrueForValidMessage()
    {
        // Arrange
        var msg = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            """
            I got 6,020 on the FoodGuessr Daily!

            🌕🌕🌕🌕🌑 4,000 (Round 1)
            🌕🌕🌘🌑🌑 2,020 (Round 2)
            🌑🌑🌑🌑🌑 0 (Round 3)

            Thursday, Jul 2, 2026
            Play here: https://www.foodguessr.com/
            """,
            [],
            [new Embed(new Uri("https://www.foodguessr.com/"))],
            DateTimeOffset.UtcNow
        );
        var detector = new FoodguessrDetector();

        // Act
        var result = detector.Matches(msg);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void FoodguessrDetector_Matches_ReturnsFalseForPlateOff()
    {
        // Arrange
        var msg = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            """
            got 9/10 on today's FoodGuessr Plate-Off!

            ✅✅✅✅✅❌✅✅✅✅

            Thursday, Jul 2, 2026
            Play here: https://www.foodguessr.com/game/plate-off/daily
            """,
            [],
            [new Embed(new Uri("https://www.foodguessr.com/game/plate-off/daily"))],
            DateTimeOffset.UtcNow
        );
        var detector = new FoodguessrDetector();

        // Act
        var result = detector.Matches(msg);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void FoodguessrDetector_LinkOnly_ReturnsFalse()
    {
        // Arrange
        var msg = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            null,
            [],
            [new Embed(new Uri("https://www.foodguessr.com/"))],
            DateTimeOffset.UtcNow
        );
        var detector = new FoodguessrDetector();

        // Act
        var result = detector.Matches(msg);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void PlateOffDetector_Matches_ReturnsTrueForValidMessage()
    {
        // Arrange
        var msg = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            """
            got 9/10 on today's FoodGuessr Plate-Off!

            ✅✅✅✅✅❌✅✅✅✅

            Thursday, Jul 2, 2026
            Play here: https://www.foodguessr.com/game/plate-off/daily
            """,
            [],
            [new Embed(new Uri("https://www.foodguessr.com/game/plate-off/daily"))],
            DateTimeOffset.UtcNow
        );
        var detector = new PlateOffDetector();

        // Act
        var result = detector.Matches(msg);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void PlateOffDetector_ContentUrlOnly_ReturnsTrue()
    {
        // Arrange
        var msg = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            """
            got 9/10 on today's FoodGuessr Plate-Off!

            ✅✅✅✅✅❌✅✅✅✅

            Thursday, Jul 2, 2026
            Play here: https://www.foodguessr.com/game/plate-off/daily
            """,
            [],
            [],
            DateTimeOffset.UtcNow
        );
        var detector = new PlateOffDetector();

        // Act
        var result = detector.Matches(msg);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void KindahardGolfDetector_Matches_ReturnsTrueForValidMessage()
    {
        // Arrange
        var msg = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            """
            kindahard.golf 07/04

            📝 17

            4.⛳ -
            3.⛳ 1
            2.⛳ 4
            1.⛳ 4
            0.🏌️ 8

            https://kindahard.golf/
            """,
            [],
            [new Embed(new Uri("https://kindahard.golf/"))],
            DateTimeOffset.UtcNow
        );
        var detector = new KindahardGolfDetector();

        // Act
        var result = detector.Matches(msg);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void KindahardGolfDetector_LinkOnly_ReturnsFalse()
    {
        // Arrange
        var msg = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            null,
            [],
            [new Embed(new Uri("https://kindahard.golf/"))],
            DateTimeOffset.UtcNow
        );
        var detector = new KindahardGolfDetector();

        // Act
        var result = detector.Matches(msg);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ScrandleDetector_Matches_ReturnsTrueForValidMessage()
    {
        // Arrange
        var msg = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            "🟩🟩🟩🟩🟩🟩🟩🟩🟥🟩 9/10 | 2026-07-02 | https://scrandle.com/",
            [],
            [new Embed(new Uri("https://scrandle.com/"))],
            DateTimeOffset.UtcNow
        );
        var detector = new ScrandleDetector();

        // Act
        var result = detector.Matches(msg);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ScrandleDetector_LinkOnly_ReturnsFalse()
    {
        // Arrange
        var msg = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            null,
            [],
            [new Embed(new Uri("https://scrandle.com/"))],
            DateTimeOffset.UtcNow
        );
        var detector = new ScrandleDetector();

        // Act
        var result = detector.Matches(msg);

        // Assert
        result.Should().BeFalse();
    }
}
