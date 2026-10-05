using Microsoft.EntityFrameworkCore;
using ShitpostBot.Application.Extensions;
using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.DailySlop;

public class DailySlopCommand(
    IDbContext dbContext,
    IChatClient chatClient,
    IDateTimeProvider dateTimeProvider,
    IEnumerable<IDailySlopDetector> detectors
) : BotCommandFeature(chatClient)
{
    private readonly string[] knownGames = detectors.Select(d => d.GameId).Distinct().ToArray();

    public override string? HelpMessage =>
        "`dailyslop` / `daily` - shows today's daily game leaderboard";

    protected override async Task<bool> TryHandleCommand(
        MessageIdentification commandMessageIdentification,
        string command,
        MessageIdentification? referenced,
        CancellationToken ct
    )
    {
        if (command != "dailyslop" && command != "daily")
        {
            return false;
        }

        var destination = new MessageDestination(
            commandMessageIdentification.GuildId,
            commandMessageIdentification.ChannelId,
            commandMessageIdentification.MessageId
        );

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Bratislava");
        var timeZoneNow = TimeZoneInfo.ConvertTime(dateTimeProvider.UtcNow, timeZone);

        var localMidnight = timeZoneNow.Date;
        var dayStart = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localMidnight, timeZone));
        var dayEnd = new DateTimeOffset(
            TimeZoneInfo.ConvertTimeToUtc(localMidnight.AddDays(1), timeZone)
        );

        var entries = await dbContext
            .DailySlopEntry.AsNoTracking()
            .Where(e => dayStart <= e.PostedOn && e.PostedOn < dayEnd)
            .ToListAsync(ct);

        if (entries.Count == 0)
        {
            await chatClient.SendMessage(destination, "No daily slop today.");
            return true;
        }

        var byUser = entries.GroupBy(e => e.PosterId).OrderByDescending(g => g.Count());
        var parts = new List<string> { $"Daily Slop Leaderboard ({timeZoneNow:MMM dd, yyyy}):" };

        foreach (var userGroup in byUser)
        {
            var posted = userGroup
                .GroupBy(e => e.GameId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(e => e.PostedOn).First());
            var displayName = await chatClient.GetMemberDisplayNameAsync(
                commandMessageIdentification.GuildId,
                userGroup.Key
            );
            var userLines = new List<string> { $"{displayName ?? $"User {userGroup.Key}"}:" };

            foreach (var knownGame in knownGames)
            {
                if (posted.TryGetValue(knownGame, out var entry))
                {
                    var identifier = new ChatMessageIdentifier(
                        entry.ChatGuildId,
                        entry.ChatChannelId,
                        entry.ChatMessageId
                    );
                    userLines.Add($"  ✅ {knownGame} {identifier.GetUri()}");
                }
                else
                {
                    userLines.Add($"  ❌ {knownGame}");
                }
            }

            parts.Add(string.Join('\n', userLines));
        }

        await chatClient.SendMessage(destination, string.Join("\n\n", parts));

        return true;
    }
}
