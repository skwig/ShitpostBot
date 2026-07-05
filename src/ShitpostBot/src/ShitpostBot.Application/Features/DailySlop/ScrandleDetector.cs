using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.Features.DailySlop;

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

        return msg.Embeds.Any(e => e.Url.Host.Contains("scrandle.com"));
    }
}
