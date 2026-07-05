using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.Features.DailySlop;

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

        return msg.Embeds.Any(e => e.Url.Host.Contains("kindahard.golf"));
    }
}
