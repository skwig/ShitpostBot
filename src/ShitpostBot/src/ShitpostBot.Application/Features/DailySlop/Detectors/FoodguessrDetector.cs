using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.Features.DailySlop.Detectors;

public class FoodguessrDetector : IDailySlopDetector
{
    public string GameId => "foodguessr";

    public bool Matches(IncomingMessage msg)
    {
        if (
            msg.Content == null
            || !msg.Content.Contains("FoodGuessr", StringComparison.OrdinalIgnoreCase)
        )
        {
            return false;
        }

        if (!DailySlopHelper.MessageHasUrl(msg, "foodguessr.com"))
        {
            return false;
        }

        if (msg.Content.Contains("plate-off", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (
            msg.Embeds.Any(e =>
                e.Url.AbsolutePath.Contains("plate-off", StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            return false;
        }

        return true;
    }
}
