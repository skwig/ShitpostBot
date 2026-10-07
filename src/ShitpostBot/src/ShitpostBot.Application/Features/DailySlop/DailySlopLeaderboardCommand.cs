using System.Globalization;
using Microsoft.EntityFrameworkCore;
using ShitpostBot.Application.Extensions;
using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.DailySlop;

public class DailySlopLeaderboardCommand(
    IDbContext dbContext,
    IChatClient chatClient,
    IDateTimeProvider dateTimeProvider,
    IEnumerable<IDailySlopDetector> detectors,
    IEnumerable<IDailySlopScoringStrategy> scoringStrategies
) : BotCommandFeature(chatClient)
{
    private readonly string[] knownGames = detectors.Select(d => d.GameId).Distinct().ToArray();

    public override string? HelpMessage =>
        "`dailyslop <game>` - shows today's leaderboard for a daily game";

    protected override async Task<bool> TryHandleCommand(
        MessageIdentification commandMessageIdentification,
        string command,
        MessageIdentification? referenced,
        CancellationToken ct
    )
    {
        var arguments = command.Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries);
        if (arguments.Length != 2 || arguments[0] != "dailyslop")
        {
            return false;
        }

        var destination = new MessageDestination(
            commandMessageIdentification.GuildId,
            commandMessageIdentification.ChannelId,
            commandMessageIdentification.MessageId
        );
        var requestedGame = arguments[1].Trim();
        var gameId = knownGames.FirstOrDefault(g =>
            g.Equals(requestedGame, StringComparison.OrdinalIgnoreCase)
        );
        if (gameId == null)
        {
            await chatClient.SendMessage(
                destination,
                $"Dailyslop '{requestedGame}' does not exist. Known dailyslops: {string.Join(", ", knownGames)}."
            );
            return true;
        }

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Bratislava");
        var timeZoneNow = TimeZoneInfo.ConvertTime(dateTimeProvider.UtcNow, timeZone);
        var localMidnight = timeZoneNow.Date;
        var dayStart = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localMidnight, timeZone));
        var dayEnd = new DateTimeOffset(
            TimeZoneInfo.ConvertTimeToUtc(localMidnight.AddDays(1), timeZone)
        );

        var entries = await dbContext
            .DailySlopEntry.AsNoTracking()
            .Where(e => dayStart <= e.PostedOn && e.PostedOn < dayEnd && e.GameId == gameId)
            .ToListAsync(ct);
        if (entries.Count == 0)
        {
            await chatClient.SendMessage(destination, $"No entries today for {gameId}.");
            return true;
        }

        var strategy = scoringStrategies.FirstOrDefault(s => s.GameId == gameId);
        var scoreComparer = CreateScoreComparer(strategy);
        var results = entries
            .GroupBy(e => e.PosterId)
            .Select(g =>
                g.OrderByDescending(e => e, scoreComparer)
                    .ThenByDescending(e => e.PostedOn)
                    .ThenByDescending(e => e.ChatMessageId)
                    .First()
            )
            .OrderBy(e => e, scoreComparer)
            .ThenBy(e => e.PostedOn)
            .ThenBy(e => e.ChatMessageId);
        var heading = $"{gameId} Leaderboard ({timeZoneNow:MMM dd, yyyy}):";
        var page = heading;
        var rank = 1;
        foreach (var entry in results)
        {
            var name = await chatClient.GetMemberDisplayNameAsync(
                commandMessageIdentification.GuildId,
                entry.PosterId
            );
            var identifier = new ChatMessageIdentifier(
                entry.ChatGuildId,
                entry.ChatChannelId,
                entry.ChatMessageId
            );
            var score =
                strategy == null
                    ? ""
                    : $"{(entry.Score is { } value ? strategy.FormatScore(value) : "score unavailable")} - ";
            var rankEmoji =
                rank == 10
                    ? "🔟"
                    : string.Concat(
                        rank.ToString(CultureInfo.InvariantCulture)
                            .Select(digit => $"{digit}\uFE0F\u20E3")
                    );
            var line =
                $"{rankEmoji} {score}{name ?? $"User {entry.PosterId}"} {identifier.GetUri()}";
            if (page.Length + 1 + line.Length > 2000)
            {
                await chatClient.SendMessage(destination, page);
                page = heading;
            }

            page += $"\n{line}";
            rank++;
        }
        await chatClient.SendMessage(destination, page);
        return true;
    }

    private static IComparer<DailySlopEntry> CreateScoreComparer(
        IDailySlopScoringStrategy? strategy
    ) =>
        Comparer<DailySlopEntry>.Create(
            (left, right) =>
            {
                if (strategy == null)
                {
                    return 0;
                }

                if (left.Score == null || right.Score == null)
                {
                    return left.Score == null ? (right.Score == null ? 0 : 1) : -1;
                }

                return strategy.Compare(left.Score, right.Score);
            }
        );
}
