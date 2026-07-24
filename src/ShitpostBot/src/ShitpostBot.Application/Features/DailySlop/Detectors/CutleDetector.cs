using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.Features.DailySlop.Detectors;

public class CutleDetector : IDailySlopDetector
{
    public string GameId => "cutle";

    public bool Matches(IncomingMessage msg)
    {
        if (
            msg.Content == null
            || !msg.Content.Contains("Cutle #", StringComparison.OrdinalIgnoreCase)
        )
        {
            return false;
        }

        return DailySlopHelper.MessageHasUrl(msg, "pfiffel.com");
    }
}