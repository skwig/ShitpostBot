using Microsoft.EntityFrameworkCore;
using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.DailySlop;

public class DailySlopCommand(IDbContext dbContext, IChatClient chatClient)
    : BotCommandFeature(chatClient)
{
    public override string? HelpMessage =>
        "`daily` / `dailyslop` - shows today's daily game leaderboard";

    protected override async Task<bool> TryHandleCommand(
        MessageIdentification commandMessageIdentification,
        string command,
        MessageIdentification? referenced,
        CancellationToken ct
    )
    {
        if (command != "daily" && command != "dailyslop")
        {
            return false;
        }

        var destination = new MessageDestination(
            commandMessageIdentification.GuildId,
            commandMessageIdentification.ChannelId,
            commandMessageIdentification.MessageId
        );

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Bratislava");
        var bratislavaNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone);
        var bratislavaDayStart = new DateTimeOffset(
            bratislavaNow.Year,
            bratislavaNow.Month,
            bratislavaNow.Day,
            0,
            0,
            0,
            timeZone.GetUtcOffset(
                new DateTime(bratislavaNow.Year, bratislavaNow.Month, bratislavaNow.Day)
            )
        );

        var bratislavaDayStartUtc = bratislavaDayStart.ToUniversalTime();

        var leaderboard = await dbContext
            .DailySlopEntry.AsNoTracking()
            .Where(e => e.PostedOn >= bratislavaDayStartUtc)
            .GroupBy(e => e.PosterId)
            .Select(g => new { PosterId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync(ct);

        if (leaderboard.Count == 0)
        {
            await chatClient.SendMessage(destination, "No daily slop today.");
            return true;
        }

        var lines = leaderboard.Select(
            (x, i) => $"{i + 1}. <@{x.PosterId}> — {x.Count} slop{(x.Count > 1 ? "s" : "")}"
        );

        await chatClient.SendMessage(
            destination,
            $"Daily Slop Leaderboard ({bratislavaNow:MMM dd, yyyy}):\n" + string.Join('\n', lines)
        );

        return true;
    }
}
