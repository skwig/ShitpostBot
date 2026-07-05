using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.Features.DailySlop;

public class FoodguessrDetector : IDailySlopDetector
{
    public string GameId => "foodguessr";

    public bool Matches(IncomingMessage msg)
    {
        if (msg.Content == null)
        {
            return false;
        }

        if (!msg.Content.Contains("FoodGuessr", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return msg.Embeds.Any(e =>
            e.Url.Host.Contains("foodguessr.com")
            && !e.Url.AbsolutePath.Contains("plate-off", StringComparison.OrdinalIgnoreCase)
        );
    }
}
