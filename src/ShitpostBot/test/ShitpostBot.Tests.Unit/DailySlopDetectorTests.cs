using FluentAssertions;
using ShitpostBot.Application.Features.DailySlop;
using ShitpostBot.Application.Features.DailySlop.Detectors;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using Xunit;

namespace ShitpostBot.Tests.Unit;

public class DailySlopDetectorTests
{
    [Fact]
    public void RngdleDetector_NewShareFormat_ExtractsSpaceSeparatedEp()
    {
        // Arrange
        var detector = new RngdleDetector();
        var message = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            """
            RNGdle 🎲 517759

            🟩 UNCOMMON • Top 43%

            🟩 🎨 Flush
            🟩 🕚 Eleven
            ⬜ 👯‍♀️ Two Pair
            +11 more

            6 481 EP
            https://rngdle.com/
            """,
            [],
            [],
            DateTimeOffset.UtcNow
        );

        // Act
        var matches = detector.Matches(message);
        var score = detector.ExtractScore(message);

        // Assert
        matches.Should().BeTrue();
        score.Should().BeOfType<RngdleScore>().Which.Ep.Should().Be(6481);
    }

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
    public void MaptapDetector_Matches_ReturnsTrueWithoutEmbeds()
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
            [],
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
        var detector = new FoodguessrDetector("foodguessr", false);

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
        var detector = new FoodguessrDetector("foodguessr", false);

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
        var detector = new FoodguessrDetector("foodguessr", false);

        // Act
        var result = detector.Matches(msg);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void FoodguessrDetector_PlateOff_MatchesValidMessage()
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
        var detector = new FoodguessrDetector("foodguessr-plateoff", true);

        // Act
        var result = detector.Matches(msg);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void FoodguessrDetector_PlateOff_ContentUrlOnly_ReturnsTrue()
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
        var detector = new FoodguessrDetector("foodguessr-plateoff", true);

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

    [Theory]
    [InlineData("size-it-up", "Size It Up", "https://magnitudle.com/size-it-up", 223)]
    [InlineData(
        "size-it-up-geography",
        "Size It Up: Geography",
        "https://magnitudle.com/size-it-up/geography",
        247
    )]
    [InlineData(
        "size-it-up-pop-culture",
        "Size It Up: Pop Culture",
        "https://magnitudle.com/size-it-up/pop-culture",
        134
    )]
    public void SizeItUpDetector_MatchesShare_TracksCorrectGame(
        string gameId,
        string title,
        string url,
        int score
    )
    {
        var msg = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            $"""
            {title}
            Overall Score {score}

            🟥⬜⬜⬜⬜ 24
            🟥⬜⬜⬜⬜ 28
            🟥🟥🟥⬜⬜ 59
            🟥⬜⬜⬜⬜ 22
            🟥🟥🟥🟥🟥 90
            {url}
            """,
            [],
            [new Embed(new Uri(url))],
            DateTimeOffset.UtcNow
        );

        var matches = new[]
        {
            new SizeItUpDetector("size-it-up", "/size-it-up"),
            new SizeItUpDetector("size-it-up-geography", "/size-it-up/geography"),
            new SizeItUpDetector("size-it-up-pop-culture", "/size-it-up/pop-culture"),
        }
            .Where(d => d.Matches(msg))
            .Select(d => d.GameId);

        matches.Should().ContainSingle().Which.Should().Be(gameId);
    }

    [Fact]
    public void SizeItUpDetector_EmbedOnly_TracksGame()
    {
        var detector = new SizeItUpDetector("size-it-up", "/size-it-up");
        var msg = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            null,
            [],
            [new Embed(new Uri("https://magnitudle.com/size-it-up"))],
            DateTimeOffset.UtcNow
        );

        detector.Matches(msg).Should().BeTrue();
    }

    [Theory]
    [InlineData("size-it-up", "/size-it-up", "https://magnitudle.com/size-it-up")]
    [InlineData(
        "size-it-up-geography",
        "/size-it-up/geography",
        "https://magnitudle.com/size-it-up/geography"
    )]
    [InlineData(
        "size-it-up-pop-culture",
        "/size-it-up/pop-culture",
        "https://magnitudle.com/size-it-up/pop-culture"
    )]
    public void SizeItUpDetector_ContentLinkOnly_TracksGame(string gameId, string path, string url)
    {
        var detector = new SizeItUpDetector(gameId, path);
        var msg = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            url,
            [],
            [],
            DateTimeOffset.UtcNow
        );

        detector.Matches(msg).Should().BeTrue();
    }

    [Theory]
    [InlineData("Size It Up\nOverall Score 223")]
    [InlineData("Size It Up\nOverall Score 223\nhttps://magnitudle.com/size-it-up/fake")]
    [InlineData("Size It Up\nOverall Score 223\nhttps://fake-magnitudle.com/size-it-up")]
    public void SizeItUpDetector_InvalidShare_DoesNotTrack(string content)
    {
        var detector = new SizeItUpDetector("size-it-up", "/size-it-up");
        var msg = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            content,
            [],
            [],
            DateTimeOffset.UtcNow
        );

        detector.Matches(msg).Should().BeFalse();
    }

    [Fact]
    public void RngdleDetector_SharedResult_TracksGame()
    {
        var msg = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            """
            RNGdle 🎲 32264

            🟦 RARE • Top 22%

            🟩 🟰 Equation
            🟩 🖐️ Five Digits
            ⬜ ↕️ Gap One
            +11 more

            11,887 EP
            https://rngdle.com/
            """,
            [],
            [],
            DateTimeOffset.UtcNow
        );

        var detector = new RngdleDetector();
        detector.Matches(msg).Should().BeTrue();
        detector.GameId.Should().Be("rngdle");
    }

    [Fact]
    public void RngdleDetector_EmbedLinkOnly_TracksGame()
    {
        var msg = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            null,
            [],
            [new Embed(new Uri("https://rngdle.com/"))],
            DateTimeOffset.UtcNow
        );

        new RngdleDetector().Matches(msg).Should().BeTrue();
    }

    [Theory]
    [InlineData("https://notrngdle.com/")]
    [InlineData("https://rngdle.com.evil.example/")]
    [InlineData("RNGdle 🎲 32264")]
    public void RngdleDetector_UnrelatedText_DoesNotTrack(string content)
    {
        var msg = new IncomingMessage(
            new MessageIdentification(1, 1, 1, 1),
            null,
            content,
            [],
            [],
            DateTimeOffset.UtcNow
        );

        new RngdleDetector().Matches(msg).Should().BeFalse();
    }
}
