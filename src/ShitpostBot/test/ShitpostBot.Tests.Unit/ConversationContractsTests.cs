using System.Text.Json;
using FluentAssertions;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Messages;
using ShitpostBot.Infrastructure.Services;
using Xunit;

namespace ShitpostBot.Tests.Unit;

public class ConversationContractsTests
{
    [Fact]
    public void ConversationTextEmbedRequest_WithQueryMode_SerializesModeAsQuery()
    {
        // Arrange
        var request = new ConversationTextEmbedRequest
        {
            Text = "gta5 discussion",
            Mode = ConversationTextEmbedMode.Query,
        };

        // Act
        var json = JsonSerializer.Serialize(request);

        // Assert
        json.Should().Contain("\"text\":\"gta5 discussion\"");
        json.Should().Contain("\"mode\":\"query\"");
    }

    [Fact]
    public void ConversationSearchOptions_Constants_MatchMvpDefaults()
    {
        // Assert
        ConversationSearchOptions.FragmentGapMinutes.Should().Be(20);
        ConversationSearchOptions.ResultCount.Should().Be(5);
    }

    [Fact]
    public void ConversationFragmentFinalized_CarriesImmutableMessages()
    {
        // Arrange
        var message = new ConversationFragmentMessage
        {
            MessageId = 100,
            AuthorId = 200,
            Timestamp = new DateTimeOffset(2026, 7, 14, 20, 15, 0, TimeSpan.Zero),
            Content = "dame gta?",
        };

        // Act
        var finalized = new ConversationFragmentFinalized
        {
            GuildId = 1,
            ChannelId = 2,
            Messages = [message],
        };

        // Assert
        finalized.Messages.Should().ContainSingle().Which.MessageId.Should().Be(100);
    }
}
