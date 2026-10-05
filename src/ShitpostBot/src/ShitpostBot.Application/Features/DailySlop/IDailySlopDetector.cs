using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.Features.DailySlop;

public interface IDailySlopDetector
{
    string GameId { get; }
    bool IsHidden => false;
    bool Matches(IncomingMessage msg);
}
