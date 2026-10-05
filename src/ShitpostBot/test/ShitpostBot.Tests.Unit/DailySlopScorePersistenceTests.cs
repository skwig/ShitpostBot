using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ShitpostBot.Domain;
using Xunit;

namespace ShitpostBot.Tests.Unit;

public class DailySlopScorePersistenceTests
{
    [Theory]
    [InlineData("""{"type":"rngdle","ep":11887}""")]
    [InlineData("""{"ep":11887,"type":"rngdle"}""")]
    public void ScoreConverter_ReadsStoredRngdleJson_AsConcreteDomainScore(string json)
    {
        // Arrange
        using var db = new DailySlopTestDbContext();
        var converter = GetScoreConverter(db);

        // Act
        var score = converter.ConvertFromProvider(json);

        // Assert
        score.Should().BeOfType<RngdleScore>().Which.Ep.Should().Be(11887);
    }

    [Fact]
    public void ScoreConverter_WritesConcreteDomainScore_WithTypeAndScoreFields()
    {
        // Arrange
        using var db = new DailySlopTestDbContext();
        var converter = GetScoreConverter(db);

        // Act
        var json = (string)converter.ConvertToProvider(new RngdleScore(11887))!;

        // Assert
        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("type").GetString().Should().Be("rngdle");
        document.RootElement.GetProperty("ep").GetInt64().Should().Be(11887);
    }

    [Theory]
    [InlineData("""{"type":"rngdle"}""")]
    [InlineData("""{"type":"unknown","ep":11887}""")]
    public void ScoreConverter_MissingFieldsOrUnknownType_RejectsInvalidScore(string json)
    {
        // Arrange
        using var db = new DailySlopTestDbContext();
        var converter = GetScoreConverter(db);

        // Act
        var read = () => converter.ConvertFromProvider(json);

        // Assert
        read.Should().Throw<JsonException>();
    }

    private static ValueConverter GetScoreConverter(DailySlopTestDbContext db) =>
        db
            .Model.FindEntityType(typeof(DailySlopEntry))!
            .FindProperty(nameof(DailySlopEntry.Score))!
            .GetValueConverter()!;
}
