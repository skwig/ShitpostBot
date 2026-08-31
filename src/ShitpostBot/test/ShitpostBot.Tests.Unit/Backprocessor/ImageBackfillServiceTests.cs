using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using ShitpostBot.Backprocessor;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Messages;
using Xunit;

namespace ShitpostBot.Tests.Unit.Backprocessor;

public class ImageBackfillServiceTests
{
    [Fact]
    public async Task ProcessMessageAsync_BotMessage_SkipsMessage()
    {
        var dbContext = new FakeDbContext();
        var unitOfWork = new FakeUnitOfWork();
        var bus = FakeBus.Create();
        var service = new ImageBackfillService(dbContext, unitOfWork, bus);

        var result = await service.ProcessMessageAsync(CreateMessage(isBot: true));

        result.InsertedImages.Should().Be(0);
        result.Skipped.Should().BeTrue();
        dbContext.ImagePost.Should().BeEmpty();
        FakeBus.Published(bus).Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessMessageAsync_LinkOnlyMessage_SkipsMessage()
    {
        var dbContext = new FakeDbContext();
        var unitOfWork = new FakeUnitOfWork();
        var bus = FakeBus.Create();
        var service = new ImageBackfillService(dbContext, unitOfWork, bus);
        var message = CreateMessage(attachments: [], content: "https://example.com/no-links-in-v1");

        var result = await service.ProcessMessageAsync(message);

        result.InsertedImages.Should().Be(0);
        result.Skipped.Should().BeTrue();
        FakeBus.Published(bus).Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessMessageAsync_SmallImage_SkipsAttachment()
    {
        var dbContext = new FakeDbContext();
        var unitOfWork = new FakeUnitOfWork();
        var bus = FakeBus.Create();
        var service = new ImageBackfillService(dbContext, unitOfWork, bus);
        var message = CreateMessage(
            attachments:
            [
                new HistoricalAttachment(
                    10,
                    new Uri("https://cdn.discordapp.com/a.png"),
                    "image/png",
                    299,
                    300
                ),
            ]
        );

        var result = await service.ProcessMessageAsync(message);

        result.InsertedImages.Should().Be(0);
        result.Skipped.Should().BeTrue();
        FakeBus.Published(bus).Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessMessageAsync_DuplicateImage_SkipsAttachment()
    {
        var dbContext = new FakeDbContext([CreateExistingPost(10)]);
        var unitOfWork = new FakeUnitOfWork();
        var bus = FakeBus.Create();
        var service = new ImageBackfillService(dbContext, unitOfWork, bus);
        var message = CreateMessage(
            attachments:
            [
                new HistoricalAttachment(
                    10,
                    new Uri("https://cdn.discordapp.com/a.png"),
                    "image/png",
                    640,
                    640
                ),
            ]
        );

        var result = await service.ProcessMessageAsync(message);

        result.InsertedImages.Should().Be(0);
        result.Skipped.Should().BeTrue();
        dbContext.ImagePost.Should().ContainSingle();
        FakeBus.Published(bus).Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessMessageAsync_NewImage_InsertsAndPublishesReevaluation()
    {
        var dbContext = new FakeDbContext();
        var unitOfWork = new FakeUnitOfWork();
        var bus = FakeBus.Create();
        var service = new ImageBackfillService(dbContext, unitOfWork, bus);
        var postedOn = new DateTimeOffset(2018, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var message = CreateMessage(
            postedOn: postedOn,
            attachments:
            [
                new HistoricalAttachment(
                    10,
                    new Uri("https://cdn.discordapp.com/a.png"),
                    "image/png",
                    640,
                    640
                ),
            ]
        );

        var result = await service.ProcessMessageAsync(message);

        result.InsertedImages.Should().Be(1);
        result.Skipped.Should().BeFalse();
        dbContext.ImagePost.Should().ContainSingle();
        var imagePost = dbContext.ImagePost.Single();
        imagePost.PostedOn.Should().Be(postedOn);
        unitOfWork.SaveChangesCalls.Should().Be(1);
        FakeBus
            .Published(bus)
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new ImagePostTracked { ImagePostId = imagePost.Id, IsReevaluation = true }
            );
    }

    private static HistoricalMessage CreateMessage(
        bool isBot = false,
        DateTimeOffset? postedOn = null,
        IReadOnlyList<HistoricalAttachment>? attachments = null,
        string? content = null
    ) =>
        new(
            GuildId: 1,
            ChannelId: 2,
            MessageId: 3,
            AuthorId: 4,
            IsBot: isBot,
            PostedOn: postedOn ?? DateTimeOffset.UtcNow,
            Content: content,
            Attachments: attachments ?? []
        );

    private static ImagePost CreateExistingPost(ulong imageId)
    {
        var now = DateTimeOffset.UtcNow;
        var image = Image.CreateOrDefault(
            imageId,
            new Uri("https://cdn.discordapp.com/existing.png"),
            "image/png",
            now
        )!;

        return ImagePost.Create(
            now,
            new ChatMessageIdentifier(1, 2, 3),
            new PosterIdentifier(4),
            now,
            image
        );
    }

    private sealed class FakeDbContext(IReadOnlyCollection<ImagePost>? initialImagePosts = null)
        : IDbContext
    {
        private readonly List<ImagePost> imagePosts = initialImagePosts?.ToList() ?? [];

        public DbSet<Post> Post => throw new NotSupportedException();
        public DbSet<ImagePost> ImagePost => new FakeImagePostDbSet(imagePosts);
        public DbSet<LinkPost> LinkPost => throw new NotSupportedException();
        public DbSet<WhitelistedPost> WhitelistedPost => throw new NotSupportedException();
        public DbSet<DailySlopEntry> DailySlopEntry => throw new NotSupportedException();
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCalls { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCalls++;
            return Task.FromResult(1);
        }
    }

    private sealed class FakeImagePostDbSet(List<ImagePost> imagePosts)
        : DbSet<ImagePost>,
            IQueryable<ImagePost>,
            IAsyncEnumerable<ImagePost>
    {
        public override IEntityType EntityType => null!;

        public override EntityEntry<ImagePost> Add(ImagePost entity)
        {
            imagePosts.Add(entity);
            return null!;
        }

        public override IAsyncEnumerator<ImagePost> GetAsyncEnumerator(
            CancellationToken cancellationToken = default
        ) => new TestAsyncEnumerator<ImagePost>(imagePosts.GetEnumerator());

        Type IQueryable.ElementType => imagePosts.AsQueryable().ElementType;
        Expression IQueryable.Expression => imagePosts.AsQueryable().Expression;
        IQueryProvider IQueryable.Provider =>
            new TestAsyncQueryProvider<ImagePost>(imagePosts.AsQueryable().Provider);

        IEnumerator<ImagePost> IEnumerable<ImagePost>.GetEnumerator() => imagePosts.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => imagePosts.GetEnumerator();
    }

    private sealed class TestAsyncQueryProvider<TEntity>(IQueryProvider inner) : IAsyncQueryProvider
    {
        public IQueryable CreateQuery(Expression expression) =>
            new TestAsyncEnumerable<TEntity>(expression);

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
            new TestAsyncEnumerable<TElement>(expression);

        public object? Execute(Expression expression) => inner.Execute(expression);

        public TResult Execute<TResult>(Expression expression) =>
            inner.Execute<TResult>(expression);

        public TResult ExecuteAsync<TResult>(
            Expression expression,
            CancellationToken cancellationToken = default
        )
        {
            var resultType = typeof(TResult).GetGenericArguments()[0];
            var executionResult = typeof(IQueryProvider)
                .GetMethod(nameof(IQueryProvider.Execute), 1, [typeof(Expression)])!
                .MakeGenericMethod(resultType)
                .Invoke(inner, [expression]);

            return (TResult)
                typeof(Task)
                    .GetMethod(nameof(Task.FromResult))!
                    .MakeGenericMethod(resultType)
                    .Invoke(null, [executionResult])!;
        }
    }

    private sealed class TestAsyncEnumerable<T>
        : EnumerableQuery<T>,
            IAsyncEnumerable<T>,
            IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable)
            : base(enumerable) { }

        public TestAsyncEnumerable(Expression expression)
            : base(expression) { }

        public IAsyncEnumerator<T> GetAsyncEnumerator(
            CancellationToken cancellationToken = default
        ) => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
    }

    private sealed class TestAsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
    {
        public T Current => inner.Current;

        public ValueTask DisposeAsync()
        {
            inner.Dispose();
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> MoveNextAsync() => new(inner.MoveNext());
    }

    private class FakeBus : DispatchProxy
    {
        public List<ImagePostTracked> Messages { get; } = [];

        public static IBus Create() => DispatchProxy.Create<IBus, FakeBus>();

        public static IReadOnlyList<ImagePostTracked> Published(IBus bus) =>
            ((FakeBus)(object)bus).Messages;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(IBus.Publish) && args?[0] is ImagePostTracked message)
            {
                Messages.Add(message);
                return Task.CompletedTask;
            }

            throw new NotSupportedException(targetMethod?.Name);
        }
    }
}
