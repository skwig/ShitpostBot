using FluentAssertions;
using ShitpostBot.Application.Features.ConversationSearch;
using ShitpostBot.Infrastructure;
using Xunit;

namespace ShitpostBot.Tests.Unit;

public class ConversationFragmentStageTests
{
    [Fact]
    public void Stage_FirstMessage_CreatesActiveFragmentWithoutFinalizedFragment()
    {
        // Arrange
        var stage = new ConversationFragmentStage();
        var first = CreateMessage(1, channelId: 10, minutes: 0);

        // Act
        var result = stage.Stage(first, TimeSpan.FromMinutes(20));

        // Assert
        result.FinalizedFragment.Should().BeNull();
    }

    [Fact]
    public void Stage_MessageWithinGap_AppendsWithoutFinalizing()
    {
        // Arrange
        var stage = new ConversationFragmentStage();
        stage.Stage(CreateMessage(1, channelId: 10, minutes: 0), TimeSpan.FromMinutes(20));

        // Act
        var result = stage.Stage(
            CreateMessage(2, channelId: 10, minutes: 20),
            TimeSpan.FromMinutes(20)
        );

        // Assert
        result.FinalizedFragment.Should().BeNull();
    }

    [Fact]
    public void Stage_MessageAfterGap_DetachesPreviousFragmentAndStartsNewFragment()
    {
        // Arrange
        var stage = new ConversationFragmentStage();
        stage.Stage(CreateMessage(1, channelId: 10, minutes: 0), TimeSpan.FromMinutes(20));
        stage.Stage(CreateMessage(2, channelId: 10, minutes: 5), TimeSpan.FromMinutes(20));

        // Act
        var result = stage.Stage(
            CreateMessage(3, channelId: 10, minutes: 26),
            TimeSpan.FromMinutes(20)
        );

        // Assert
        result.FinalizedFragment.Should().NotBeNull();
        result.FinalizedFragment!.Messages.Select(m => m.MessageId).Should().Equal(1UL, 2UL);
        result.FinalizedFragment.LastMessageAt.Should().Be(CreateTimestamp(5));
    }

    [Fact]
    public void Stage_DifferentChannels_DoNotFinalizeEachOther()
    {
        // Arrange
        var stage = new ConversationFragmentStage();
        stage.Stage(CreateMessage(1, channelId: 10, minutes: 0), TimeSpan.FromMinutes(20));

        // Act
        var result = stage.Stage(
            CreateMessage(2, channelId: 20, minutes: 60),
            TimeSpan.FromMinutes(20)
        );

        // Assert
        result.FinalizedFragment.Should().BeNull();
    }

    [Fact]
    public void Stage_WhenActiveFragmentHasMaxMessages_DetachesPreviousFragmentAndStartsNewFragment()
    {
        // Arrange
        var stage = new ConversationFragmentStage();
        for (var i = 1; i <= ConversationSearchOptions.MaxFragmentMessageCount; i++)
        {
            stage.Stage(
                CreateMessage((ulong)i, channelId: 10, minutes: i),
                TimeSpan.FromMinutes(20)
            );
        }

        // Act
        var result = stage.Stage(
            CreateMessage(21, channelId: 10, minutes: 21),
            TimeSpan.FromMinutes(20)
        );

        // Assert
        result.FinalizedFragment.Should().NotBeNull();
        result.FinalizedFragment!.Messages.Should().HaveCount(20);
        result.FinalizedFragment.Messages.Last().MessageId.Should().Be(20);
    }

    [Fact]
    public async Task Stage_ConcurrentBoundaryMessages_FinalizesPreviousFragmentOnce()
    {
        // Arrange
        var stage = new ConversationFragmentStage();
        stage.Stage(CreateMessage(1, channelId: 10, minutes: 0), TimeSpan.FromMinutes(20));

        // Act
        var results = await Task.WhenAll(
            Enumerable
                .Range(2, 20)
                .Select(id =>
                    Task.Run(() =>
                        stage.Stage(
                            CreateMessage((ulong)id, channelId: 10, minutes: 30),
                            TimeSpan.FromMinutes(20)
                        )
                    )
                )
        );

        // Assert
        results.Count(r => r.FinalizedFragment is not null).Should().Be(1);
        results
            .Single(r => r.FinalizedFragment is not null)
            .FinalizedFragment!.Messages.Should()
            .ContainSingle(m => m.MessageId == 1);
    }

    private static StagedMessage CreateMessage(ulong messageId, ulong channelId, int minutes)
    {
        return new StagedMessage(
            GuildId: 1,
            ChannelId: channelId,
            MessageId: messageId,
            AuthorId: 100 + messageId,
            Timestamp: CreateTimestamp(minutes),
            Content: $"message {messageId}"
        );
    }

    private static DateTimeOffset CreateTimestamp(int minutes)
    {
        return new DateTimeOffset(2026, 7, 14, 20, 0, 0, TimeSpan.Zero).AddMinutes(minutes);
    }
}
