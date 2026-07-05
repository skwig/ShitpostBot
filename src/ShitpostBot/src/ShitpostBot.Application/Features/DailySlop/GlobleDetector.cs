using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.Features.DailySlop;

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

        return msg.Embeds.Any(e => e.Url.Host.Contains("globle-game.com"));
    }
}
