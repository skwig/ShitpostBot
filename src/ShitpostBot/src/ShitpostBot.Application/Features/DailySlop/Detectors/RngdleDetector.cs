using System.Globalization;
using System.Text.RegularExpressions;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.Features.DailySlop.Detectors;

public partial class RngdleDetector : IDailySlopDetector, IDailySlopScoringStrategy
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

    public DailySlopScore? ExtractScore(IncomingMessage message)
    {
        if (message.Content == null)
        {
            return null;
        }

        var match = EpPattern().Match(message.Content);
        return
            match.Success
            && long.TryParse(
                match.Groups["ep"].Value.Replace(",", ""),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var ep
            )
            ? new RngdleScore(ep)
            : null;
    }

    public int Compare(DailySlopScore left, DailySlopScore right) =>
        ((RngdleScore)right).Ep.CompareTo(((RngdleScore)left).Ep);

    public string FormatScore(DailySlopScore score) =>
        $"{((RngdleScore)score).Ep.ToString("N0", CultureInfo.InvariantCulture)} EP";

    private static bool IsRngdleUrl(Uri url) =>
        (url.Scheme == Uri.UriSchemeHttp || url.Scheme == Uri.UriSchemeHttps)
        && url.Host.Equals("rngdle.com", StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex(@"https?://[^\s<>]+", RegexOptions.IgnoreCase)]
    private static partial Regex UrlPattern();

    [GeneratedRegex(
        @"^[\t ]*(?<ep>[0-9]{1,3}(?:,[0-9]{3})+|[0-9]+)[\t ]+EP[\t ]*\r?$",
        RegexOptions.Multiline
    )]
    private static partial Regex EpPattern();
}
