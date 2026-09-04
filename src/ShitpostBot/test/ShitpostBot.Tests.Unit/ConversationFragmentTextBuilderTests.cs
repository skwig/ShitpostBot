using FluentAssertions;
using ShitpostBot.Application.Features.ConversationSearch;
using Xunit;

namespace ShitpostBot.Tests.Unit;

public class ConversationFragmentTextBuilderTests
{
    [Fact]
    public void Build_JoinsNonEmptyTrimmedMessages()
    {
        // Arrange
        var messages = new[]
        {
            new StagedMessage(1, 2, 10, 100, DateTimeOffset.UtcNow, "dame dnes gta?"),
            new StagedMessage(1, 2, 11, 101, DateTimeOffset.UtcNow, " "),
            new StagedMessage(1, 2, 12, 102, DateTimeOffset.UtcNow, "cayo again?"),
        };

        // Act
        var text = ConversationFragmentTextBuilder.Build(messages);

        // Assert
        text.Should().Be("dame dnes gta?\ncayo again?");
    }
}
