using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.Features.DailySlop.Detectors;

public class PlateOffDetector : IDailySlopDetector
{
    public string GameId => "foodguessr-plateoff";

    public bool Matches(IncomingMessage msg)
    {
        if (
            msg.Embeds.Any(e =>
                e.Url.Host.Contains("foodguessr.com")
                && e.Url.AbsolutePath.Contains("plate-off", StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            return true;
        }
        if (
            msg.Content != null
            && msg.Content.Contains(
                "foodguessr.com/game/plate-off",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            return true;
        }
        return false;
    }
}
