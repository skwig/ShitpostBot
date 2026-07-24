using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.Features.DailySlop.Detectors;

public class KindahardGolfDetector : IDailySlopDetector
{
    public string GameId => "kindahard.golf";

    public bool Matches(IncomingMessage msg)
    {
        if (
            msg.Content == null
            || !msg.Content.Contains("kindahard.golf", StringComparison.OrdinalIgnoreCase)
        )
        {
            return false;
        }

        return DailySlopHelper.MessageHasUrl(msg, "kindahard.golf");
    }
}