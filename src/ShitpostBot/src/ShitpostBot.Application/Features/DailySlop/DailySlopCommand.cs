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
    IDateTimeProvider dateTimeProvider
) : BotCommandFeature(chatClient)
{
    private static readonly string[] KnownGames =
    [
        "travle",
        "globle",
        "maptap",
        "cutle",
        "foodguessr",
        "foodguessr-plateoff",
        "kindahard.golf",
        "scrandle",
    ];

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

        var dayStart = new DateTimeOffset(
            timeZoneNow.Year,
            timeZoneNow.Month,
            timeZoneNow.Day,
            0,
            0,
            0,
            timeZone.GetUtcOffset(
                new DateTime(timeZoneNow.Year, timeZoneNow.Month, timeZoneNow.Day)
            )
        ).ToUniversalTime();

        var dayEnd = dayStart.AddDays(1);

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
            var userLines = new List<string> { $"<@{userGroup.Key}>:" };

            foreach (var gameId in KnownGames)
            {
                if (posted.TryGetValue(gameId, out var entry))
                {
                    var identifier = new ChatMessageIdentifier(
                        entry.ChatGuildId,
                        entry.ChatChannelId,
                        entry.ChatMessageId
                    );
                    userLines.Add($"  ✅ {gameId} {identifier.GetUri()}");
                }
                else
                {
                    userLines.Add($"  ❌ {gameId}");
                }
            }

            parts.Add(string.Join('\n', userLines));
        }

        await chatClient.SendMessage(destination, string.Join("\n\n", parts));

        return true;
    }
}
