using System.Globalization;
using System.Text.RegularExpressions;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.Features.DailySlop.Detectors;

public partial class RunedleDetector(string gameId, string shareName)
    : IDailySlopDetector,
        IDailySlopScoringStrategy
{
    public string GameId => gameId;
    public bool IsHidden => true;

    public bool Matches(IncomingMessage msg)
    {
        var header = GetHeader(msg);
        return header.Success
            && header.Groups["name"].Value == shareName
            && (
                msg.Embeds.Any(e => IsRunedleUrl(e.Url))
                || (
                    msg.Content != null
                    && UrlPattern()
                        .Matches(msg.Content)
                        .Any(match =>
                            Uri.TryCreate(match.Value, UriKind.Absolute, out var url)
                            && IsRunedleUrl(url)
                        )
                )
            );
    }

    public DailySlopScore? ExtractScore(IncomingMessage message)
    {
        var header = GetHeader(message);
        return
            header.Success
            && header.Groups["name"].Value == shareName
            && int.TryParse(
                header.Groups["attempts"].Value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var attempts
            )
            && attempts > 0
            ? new RunedleScore(attempts)
            : null;
    }

    public int Compare(DailySlopScore left, DailySlopScore right) =>
        ((RunedleScore)left).Attempts.CompareTo(((RunedleScore)right).Attempts);

    public string FormatScore(DailySlopScore score) =>
        $"{((RunedleScore)score).Attempts.ToString(CultureInfo.InvariantCulture)} attempts";

    private static Match GetHeader(IncomingMessage message) =>
        HeaderPattern().Match(message.Content ?? "");

    private static bool IsRunedleUrl(Uri url) =>
        (url.Scheme == Uri.UriSchemeHttp || url.Scheme == Uri.UriSchemeHttps)
        && url.Host.Equals("runedle.com", StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex(
        @"^[\t ]*(?<name>Runedle(?: \(expert\))?) in (?<attempts>[0-9]+) attempts?[\t ]*\r?$",
        RegexOptions.Multiline
    )]
    private static partial Regex HeaderPattern();

    [GeneratedRegex(@"https?://[^\s<>]+", RegexOptions.IgnoreCase)]
    private static partial Regex UrlPattern();
}
