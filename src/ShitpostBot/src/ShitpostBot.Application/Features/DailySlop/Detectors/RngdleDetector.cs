using System.Text.RegularExpressions;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.Features.DailySlop.Detectors;

public partial class RngdleDetector : IDailySlopDetector
{
    public string GameId => "rngdle";

    public bool Matches(IncomingMessage msg)
    {
        return msg.Embeds.Any(e => IsRngdleUrl(e.Url))
            || (
                msg.Content != null
                && UrlPattern()
                    .Matches(msg.Content)
                    .Any(match =>
                        Uri.TryCreate(match.Value, UriKind.Absolute, out var url)
                        && IsRngdleUrl(url)
                    )
            );
    }

    private static bool IsRngdleUrl(Uri url) =>
        (url.Scheme == Uri.UriSchemeHttp || url.Scheme == Uri.UriSchemeHttps)
        && url.Host.Equals("rngdle.com", StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex(@"https?://[^\s<>]+", RegexOptions.IgnoreCase)]
    private static partial Regex UrlPattern();
}
