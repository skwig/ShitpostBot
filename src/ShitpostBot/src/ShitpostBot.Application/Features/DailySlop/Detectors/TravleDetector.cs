using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.Features.DailySlop.Detectors;

public class TravleDetector : IDailySlopDetector
{
    public string GameId => "travle";

    public bool Matches(IncomingMessage msg)
    {
        if (
            msg.Content == null
            || !msg.Content.Contains("#travle", StringComparison.OrdinalIgnoreCase)
        )
        {
            return false;
        }

        return DailySlopHelper.MessageHasUrl(msg, "travle.earth");
    }
}