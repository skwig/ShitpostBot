using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.Features.DailySlop.Detectors;

public class FoodguessrDetector(string gameId, bool isPlateOff) : IDailySlopDetector
{
    public string GameId => gameId;

    public bool Matches(IncomingMessage msg)
    {
        if (isPlateOff)
        {
            return msg.Embeds.Any(e =>
                    e.Url.Host.Contains("foodguessr.com")
                    && e.Url.AbsolutePath.Contains("plate-off", StringComparison.OrdinalIgnoreCase)
                )
                || (
                    msg.Content != null
                    && msg.Content.Contains(
                        "foodguessr.com/game/plate-off",
                        StringComparison.OrdinalIgnoreCase
                    )
                );
        }

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
