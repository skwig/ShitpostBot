using System.Reflection;
using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ShitpostBot.Backprocessor;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using Xunit;

namespace ShitpostBot.Tests.Unit.Backprocessor;

public class BackprocessorRunnerTests
{
    [Fact]
    public async Task RunAsync_UsesSavedCursorBeforeConfiguredCursor()
    {
        var stateStore = new FakeStateStore(
            new BackprocessorState
            {
                Channels =
                [
                    new BackprocessorChannelState
                    {
                        GuildId = 1,
                        ChannelId = 2,
                        Name = "general",
                        LastCompletedMessageId = 500,
                    },
                ],
            }
        );
        var history = new FakeDiscordHistoryClient([]);
        var runner = CreateRunner(stateStore, history);

        await runner.RunAsync();

        history.Requests.Should().ContainSingle().Which.BeforeMessageId.Should().Be(500);
    }

    [Fact]
    public async Task RunAsync_StopsAtOldestMessageIdAndDoesNotProcessBoundary()
    {
        var stateStore = new FakeStateStore(new BackprocessorState());
        var history = new FakeDiscordHistoryClient([
            CreateMessage(300),
            CreateMessage(200),
            CreateMessage(100),
        ]);
        var imageBackfill = new FakeImageBackfillService();
        var runner = CreateRunner(stateStore, history, imageBackfill, oldestMessageId: 200);

        await runner.RunAsync();

        imageBackfill.ProcessedMessageIds.Should().Equal(300);
        stateStore.SavedStates.Last().Channels[0].LastCompletedMessageId.Should().Be(300);
    }

    [Fact]
    public async Task RunAsync_SavesStateAfterEachProcessedMessage()
    {
        var stateStore = new FakeStateStore(new BackprocessorState());
        var history = new FakeDiscordHistoryClient([CreateMessage(300), CreateMessage(250)]);
        var imageBackfill = new FakeImageBackfillService(insertedImages: 1);
        var runner = CreateRunner(stateStore, history, imageBackfill);

        await runner.RunAsync();

        stateStore.SavedStates.Should().HaveCount(2);
        stateStore.SavedStates[0].Channels[0].LastCompletedMessageId.Should().Be(300);
        stateStore.SavedStates[1].Channels[0].LastCompletedMessageId.Should().Be(250);
        stateStore.SavedStates[1].Channels[0].ProcessedMessages.Should().Be(2);
        stateStore.SavedStates[1].Channels[0].InsertedImages.Should().Be(2);
    }

    private static BackprocessorRunner CreateRunner(
        FakeStateStore stateStore,
        FakeDiscordHistoryClient history,
        FakeImageBackfillService? imageBackfill = null,
        ulong oldestMessageId = 100
    ) =>
        new(
            NullLogger<BackprocessorRunner>.Instance,
            Options.Create(
                new BackprocessorOptions
                {
                    StateFilePath = "unused.json",
                    PageSize = 50,
                    PageDelay = TimeSpan.Zero,
                    MessageDelay = TimeSpan.Zero,
                    Channels =
                    [
                        new BackprocessorChannelOptions
                        {
                            Name = "general",
                            GuildId = 1,
                            ChannelId = 2,
                            OldestMessageId = oldestMessageId,
                            StartBeforeMessageId = 999,
                        },
                    ],
                }
            ),
            stateStore,
            history,
            imageBackfill ?? new FakeImageBackfillService()
        );

    private static HistoricalMessage CreateMessage(ulong messageId) =>
        new(1, 2, messageId, 4, false, DateTimeOffset.UtcNow, null, []);

    private sealed class FakeStateStore(BackprocessorState initialState) : IBackprocessorStateStore
    {
        public List<BackprocessorState> SavedStates { get; } = [];

        public Task<BackprocessorState> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(initialState);

        public Task SaveAsync(
            BackprocessorState state,
            CancellationToken cancellationToken = default
        )
        {
            SavedStates.Add(state);
            initialState = state;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeDiscordHistoryClient(IReadOnlyList<HistoricalMessage> page)
        : IDiscordHistoryClient
    {
        public List<(ulong? BeforeMessageId, int PageSize)> Requests { get; } = [];
        private bool returnedPage;

        public Task<IReadOnlyList<HistoricalMessage>> GetMessagesBeforeAsync(
            BackprocessorChannelOptions channelOptions,
            ulong? beforeMessageId,
            int pageSize,
            CancellationToken cancellationToken = default
        )
        {
            Requests.Add((beforeMessageId, pageSize));
            if (returnedPage)
            {
                return Task.FromResult<IReadOnlyList<HistoricalMessage>>([]);
            }

            returnedPage = true;
            return Task.FromResult(page);
        }
    }

    private sealed class FakeImageBackfillService(int insertedImages = 0)
        : ImageBackfillService(new ThrowingDbContext(), new ThrowingUnitOfWork(), FakeBus.Create())
    {
        public List<ulong> ProcessedMessageIds { get; } = [];

        public override Task<ImageBackfillResult> ProcessMessageAsync(
            HistoricalMessage message,
            CancellationToken cancellationToken = default
        )
        {
            ProcessedMessageIds.Add(message.MessageId);
            return Task.FromResult(new ImageBackfillResult(insertedImages, insertedImages == 0));
        }
    }

    private sealed class ThrowingDbContext : IDbContext
    {
        public DbSet<Post> Post => throw new NotSupportedException();
        public DbSet<ImagePost> ImagePost => throw new NotSupportedException();
        public DbSet<LinkPost> LinkPost => throw new NotSupportedException();
        public DbSet<WhitelistedPost> WhitelistedPost => throw new NotSupportedException();
        public DbSet<DailySlopEntry> DailySlopEntry => throw new NotSupportedException();
    }

    private sealed class ThrowingUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private class FakeBus : DispatchProxy
    {
        public static IBus Create() => DispatchProxy.Create<IBus, FakeBus>();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            throw new NotSupportedException(targetMethod?.Name);
        }
    }
}
