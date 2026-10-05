using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.Features.DailySlop;

public interface IDailySlopScoringStrategy
{
    string GameId { get; }
    DailySlopScore? ExtractScore(IncomingMessage message);

    /// <summary>Returns a negative value when left ranks better than right, zero for a tie.</summary>
    int Compare(DailySlopScore left, DailySlopScore right);
    string FormatScore(DailySlopScore score);
}
