using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.Features.DailySlop;

public class PlateOffDetector : IDailySlopDetector
{
    public string GameId => "foodguessr-plateoff";

    public bool Matches(IncomingMessage msg)
    {
        if (
            msg.Content == null
            || !msg.Content.Contains("Plate-Off", StringComparison.OrdinalIgnoreCase)
        )
        {
            return false;
        }

        return msg.Embeds.Any(e =>
            e.Url.Host.Contains("foodguessr.com")
            && e.Url.AbsolutePath.Contains("plate-off", StringComparison.OrdinalIgnoreCase)
        );
    }
}
