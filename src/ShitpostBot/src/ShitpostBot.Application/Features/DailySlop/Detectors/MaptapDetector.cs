using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.Features.DailySlop.Detectors;

public class MaptapDetector : IDailySlopDetector
{
    public string GameId => "maptap";

    public bool Matches(IncomingMessage msg)
    {
        if (msg.Content == null)
        {
            return false;
        }

        if (
            !msg.Content.Contains("www.maptap.gg", StringComparison.OrdinalIgnoreCase)
            || !msg.Content.Contains("Final score:", StringComparison.OrdinalIgnoreCase)
        )
        {
            return false;
        }

        return DailySlopHelper.MessageHasUrl(msg, "maptap.gg");
    }
}
