using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.Features.DailySlop;

public interface IDailySlopDetector
{
    string GameId { get; }
    bool Matches(IncomingMessage msg);
}
