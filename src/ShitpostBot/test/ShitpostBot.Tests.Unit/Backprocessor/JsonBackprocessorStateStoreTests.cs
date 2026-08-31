using FluentAssertions;
using Microsoft.Extensions.Options;
using ShitpostBot.Backprocessor;
using Xunit;

namespace ShitpostBot.Tests.Unit.Backprocessor;

public class JsonBackprocessorStateStoreTests
{
    [Fact]
    public async Task LoadAsync_MissingFile_ReturnsEmptyState()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        var store = CreateStore(path);

        var state = await store.LoadAsync();

        state.Channels.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsChannelState()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        var store = CreateStore(path);
        var saved = new BackprocessorState
        {
            Channels =
            [
                new BackprocessorChannelState
                {
                    GuildId = 1,
                    ChannelId = 2,
                    Name = "gladsheim",
                    LastCompletedMessageId = 123,
                    LastCompletedTimestamp = new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.Zero),
                    ProcessedMessages = 10,
                    InsertedImages = 3,
                    SkippedMessages = 7,
                    FailedMessages = 1,
                },
            ],
        };

        await store.SaveAsync(saved);

        var loaded = await store.LoadAsync();
        loaded.Channels.Should().ContainSingle();
        loaded.Channels[0].LastCompletedMessageId.Should().Be(123);
        loaded.Channels[0].InsertedImages.Should().Be(3);
    }

    private static JsonBackprocessorStateStore CreateStore(string path) =>
        new(Options.Create(new BackprocessorOptions { StateFilePath = path }));
}
