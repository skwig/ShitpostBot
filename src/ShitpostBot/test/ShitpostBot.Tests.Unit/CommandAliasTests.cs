using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Refit;
using ShitpostBot.Application;
using ShitpostBot.Application.Features.DailySlop;
using ShitpostBot.Application.Features.DailySlop.Detectors;
using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;
using Xunit;

namespace ShitpostBot.Tests.Unit;

public class CommandAliasTests
{
    [Theory]
    [InlineData("<@42> leaderboard", true)]
    [InlineData("<@42>   leaderboard  ", true)]
    [InlineData("<@42> leaderboard travle", false)]
    [InlineData("<@42> leaderboards", false)]
    [InlineData("<@42> Leaderboard", false)]
    [InlineData("leaderboard", false)]
    [InlineData("<@43> leaderboard", false)]
    public async Task Create_ExactAlias_ShowsTargetLeaderboard(string content, bool expectedHandled)
    {
        using var db = new DailySlopTestDbContext();
        var chat = new DailySlopTestChatClient();
        var target = new DailySlopLeaderboardCommand(
            db,
            chat,
            new Clock(),
            [new RngdleDetector()],
            []
        );
        var alias = new CommandAlias(chat, target, "leaderboard", "dailyslop rngdle");

        var handled = await alias.TryHandleCreate(Message(content), default);

        handled.Should().Be(expectedHandled);
        if (expectedHandled)
        {
            chat.Messages.Should()
                .ContainSingle()
                .Which.Should()
                .Be("No entries today for rngdle.");
        }
        else
        {
            chat.Messages.Should().BeEmpty();
        }
    }

    [Theory]
    [InlineData("<@42> daily rngdle", true)]
    [InlineData("<@42> daily\trngdle", true)]
    [InlineData("<@42> daily", false)]
    [InlineData("<@42> dailyish rngdle", false)]
    public async Task Create_ArgumentAlias_ForwardsArgumentsOnWordBoundary(
        string content,
        bool expectedHandled
    )
    {
        using var db = new DailySlopTestDbContext();
        var chat = new DailySlopTestChatClient();
        var target = new DailySlopLeaderboardCommand(
            db,
            chat,
            new Clock(),
            [new RngdleDetector()],
            []
        );
        var alias = new CommandAlias(chat, target, "daily", "dailyslop", forwardArguments: true);

        var handled = await alias.TryHandleCreate(Message(content), default);

        handled.Should().Be(expectedHandled);
        chat.Messages.Should().HaveCount(expectedHandled ? 1 : 0);
        if (expectedHandled)
        {
            chat.Messages[0].Should().Be("No entries today for rngdle.");
        }
    }

    [Fact]
    public async Task Update_Alias_PreservesMessageReplyCancellationAndEditContext()
    {
        var chat = new DailySlopTestChatClient { ReplyMessageId = 123 };
        var target = new ContextCommand(chat);
        var alias = new CommandAlias(chat, target, "alias", "canonical");
        var message = Message("<@42> alias") with
        {
            RepliedToId = new MessageIdentification(1, 2, 3, 4),
        };
        using var cancellation = new CancellationTokenSource();

        var handled = await alias.TryHandleUpdate(
            Message("old content"),
            message,
            cancellation.Token
        );

        handled.Should().BeTrue();
        target
            .Received.Should()
            .Be((message.Id, "canonical", message.RepliedToId, (ulong?)123, cancellation.Token));

        await alias.TryHandleCreate(message, cancellation.Token);
        target
            .Received.Should()
            .Be((message.Id, "canonical", message.RepliedToId, (ulong?)null, cancellation.Token));
    }

    [Fact]
    public async Task Registration_ReusesScopedTargetAndExposesAliasHelp()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IChatClient>(new DailySlopTestChatClient());
        services
            .AddMessageFeature<ContextCommand>()
            .WithAlias(alias: "alias", canonical: "canonical")
            .WithAlias(alias: "second alias", canonical: "canonical");
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var features = scope.ServiceProvider.GetServices<IMessageFeature>().ToArray();
        var target = scope.ServiceProvider.GetRequiredService<ContextCommand>();

        features.Should().HaveCount(3);
        features[0].Should().BeSameAs(target);
        await features[1].TryHandleCreate(Message("<@42> alias"), default);

        target.Received!.Value.Command.Should().Be("canonical");
        ((BotCommandFeature)features[1])
            .HelpMessage.Should()
            .Contain("`alias`")
            .And.Contain("`canonical`");

        (await features[2].TryHandleCreate(Message("<@42> second alias"), default))
            .Should()
            .BeTrue();
        target.Received!.Value.Command.Should().Be("canonical");
    }

    [Theory]
    [InlineData("leaderboard", "No entries today for rngdle.")]
    [InlineData("daily rngdle", "No entries today for rngdle.")]
    [InlineData("daily", "No daily slop today.")]
    [InlineData("updated 5", "No edited messages recorded in this channel yet.")]
    [InlineData(
        "repost where",
        "Invalid usage: you need to reply to a post to get the match value"
    )]
    [InlineData(
        "repost match all cos",
        "Invalid usage: you need to reply to a post to get the match value"
    )]
    public async Task ApplicationRegistration_Alias_RoutesToExpectedCommand(
        string command,
        string expected
    )
    {
        using var db = new DailySlopTestDbContext();
        var chat = new DailySlopTestChatClient();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.AddMassTransit(x => x.UsingInMemory());
        services.AddSingleton<IChatClient>(chat);
        services.AddSingleton<IDbContext>(db);
        services.AddSingleton<IUnitOfWork>(db);
        services.AddSingleton<IDateTimeProvider>(new Clock());
        services.AddSingleton<IMetrics>(new TestMetrics());
        services.AddSingleton<IHostEnvironment>(new HostingEnvironment());
        services.AddSingleton(RestService.For<IImageFeatureExtractorApi>("http://localhost"));
        services.AddShitpostBotApplication(new ConfigurationBuilder().Build());
        await using var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<MessageRouter>().RouteCreate(Message($"<@42> {command}"));

        chat.Messages.Should().ContainSingle().Which.Should().Be(expected);
    }

    private static IncomingMessage Message(string content) =>
        new(new MessageIdentification(1, 2, 999, 99), null, content, [], [], DateTimeOffset.UtcNow);

    private sealed class Clock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => new(2026, 10, 7, 12, 0, 0, TimeSpan.Zero);
        public DateTimeOffset Now => UtcNow;
    }

    private sealed class TestMetrics : IMetrics
    {
        public DateTimeOffset DeployedOn => DateTimeOffset.UtcNow;
        public DateTimeOffset? LastLinkSaveTimestamp { get; set; }
        public DateTimeOffset? LastImageSaveTimestamp { get; set; }
        public DateTimeOffset? LastImageEvaluationTimestamp { get; set; }
    }

    public sealed class ContextCommand(IChatClient chat) : BotCommandFeature(chat)
    {
        public (
            MessageIdentification Id,
            string Command,
            MessageIdentification? Reference,
            ulong? EditId,
            CancellationToken Token
        )? Received { get; private set; }

        protected override Task<bool> TryHandleCommand(
            MessageIdentification id,
            string command,
            MessageIdentification? referenced,
            CancellationToken ct
        )
        {
            if (command != "canonical")
            {
                return Task.FromResult(false);
            }

            Received = (id, command, referenced, EditBotResponseMessageId, ct);
            return Task.FromResult(true);
        }
    }
}
