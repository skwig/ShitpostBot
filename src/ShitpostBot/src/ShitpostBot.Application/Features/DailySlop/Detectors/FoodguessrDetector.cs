using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.Features.DailySlop.Detectors;

public class FoodguessrDetector : IDailySlopDetector
{
    public string GameId => "foodguessr";

    public bool Matches(IncomingMessage msg)
    {
        if (
            msg.Embeds.Any(e =>
                e.Url.Host.Contains("foodguessr.com")
                && !e.Url.AbsolutePath.Contains("plate-off", StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            return true;
        }
        if (
            msg.Content != null
            && msg.Content.Contains("foodguessr.com", StringComparison.OrdinalIgnoreCase)
            && !msg.Content.Contains("plate-off", StringComparison.OrdinalIgnoreCase)
        )
        {
            return true;
        }
        return false;
    }
}
