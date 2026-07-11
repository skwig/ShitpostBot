using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.Features.DailySlop.Detectors;

public class ScrandleDetector : IDailySlopDetector
{
    public string GameId => "scrandle";

    public bool Matches(IncomingMessage msg)
    {
        if (
            msg.Content == null
            || !msg.Content.Contains("scrandle.com", StringComparison.OrdinalIgnoreCase)
        )
        {
            return false;
        }

        return DailySlopHelper.MessageHasUrl(msg, "scrandle.com");
    }
}