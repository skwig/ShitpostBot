using System.Globalization;
using System.Text.RegularExpressions;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.Features.DailySlop.Detectors;

internal partial class OriginleDetector : IDailySlopDetector, IDailySlopScoringStrategy
{
    public string GameId => "originle";

    public bool Matches(IncomingMessage msg) =>
        msg.Embeds.Any(e => IsOriginleUrl(e.Url))
        || (
            msg.Content != null
            && UrlPattern()
                .Matches(msg.Content)
                .Any(match =>
                    Uri.TryCreate(match.Value, UriKind.Absolute, out var url) && IsOriginleUrl(url)
                )
        );

    public DailySlopScore? ExtractScore(IncomingMessage message)
    {
        if (message.Content == null)
        {
            return null;
        }

        var match = ScorePattern().Match(message.Content);
        return
            match.Success
            && long.TryParse(
                match.Groups["points"].Value,
                NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture,
                out var points
            )
            ? new OriginleScore(points)
            : null;
    }

    public int Compare(DailySlopScore left, DailySlopScore right) =>
        ((OriginleScore)right).Points.CompareTo(((OriginleScore)left).Points);

    public string FormatScore(DailySlopScore score) =>
        ((OriginleScore)score).Points.ToString("N0", CultureInfo.InvariantCulture);

    private static bool IsOriginleUrl(Uri url) =>
        (url.Scheme == Uri.UriSchemeHttp || url.Scheme == Uri.UriSchemeHttps)
        && url.Host.Equals("originle.io", StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex(@"https?://[^\s<>]+", RegexOptions.IgnoreCase)]
    private static partial Regex UrlPattern();

    [GeneratedRegex(
        @"^Originle #[0-9]+ · (?<points>[0-9]+(?:,[0-9]{3})*)[ \t]*\r?$",
        RegexOptions.Multiline
    )]
    private static partial Regex ScorePattern();
}
