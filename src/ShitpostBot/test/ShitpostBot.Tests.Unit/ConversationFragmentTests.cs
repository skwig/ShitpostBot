using FluentAssertions;
using Pgvector;
using ShitpostBot.Domain;
using Xunit;

namespace ShitpostBot.Tests.Unit;

public class ConversationFragmentTests
{
    [Fact]
    public void Create_WithMetadataAndEmbedding_SetsProperties()
    {
        // Arrange
        var embedding = new Vector(new float[384]);
        var startedAt = new DateTimeOffset(2026, 7, 14, 20, 15, 0, TimeSpan.Zero);
        var endedAt = startedAt.AddMinutes(16);

        // Act
        var fragment = ConversationFragment.Create(
            guildId: 1,
            channelId: 2,
            firstMessageId: 10,
            lastMessageId: 20,
            startedAt: startedAt,
            endedAt: endedAt,
            messageCount: 3,
            embedding: embedding
        );

        // Assert
        fragment.GuildId.Should().Be(1);
        fragment.ChannelId.Should().Be(2);
        fragment.FirstMessageId.Should().Be(10);
        fragment.LastMessageId.Should().Be(20);
        fragment.StartedAt.Should().Be(startedAt);
        fragment.EndedAt.Should().Be(endedAt);
        fragment.MessageCount.Should().Be(3);
        fragment.Embedding.Should().BeSameAs(embedding);
    }
}
