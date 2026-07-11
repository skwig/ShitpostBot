using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.Features.DailySlop.Detectors;

public class GlobleDetector : IDailySlopDetector
{
    public string GameId => "globle";

    public bool Matches(IncomingMessage msg)
    {
        if (
            msg.Content == null
            || !msg.Content.Contains("#globle", StringComparison.OrdinalIgnoreCase)
        )
        {
            return false;
        }

        return DailySlopHelper.MessageHasUrl(msg, "globle-game.com");
    }
}
