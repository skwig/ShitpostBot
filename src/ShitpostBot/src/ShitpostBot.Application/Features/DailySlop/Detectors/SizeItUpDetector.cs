using System.Text.RegularExpressions;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.Features.DailySlop.Detectors;

public partial class SizeItUpDetector(string gameId, string path) : IDailySlopDetector
{
    public string GameId => gameId;

    public bool Matches(IncomingMessage msg)
    {
        return msg.Embeds.Any(e => IsMatchingUrl(e.Url))
            || (
                msg.Content != null
                && UrlPattern()
                    .Matches(msg.Content)
                    .Any(match =>
                        Uri.TryCreate(match.Value, UriKind.Absolute, out var url)
                        && IsMatchingUrl(url)
                    )
            );
    }

    private bool IsMatchingUrl(Uri url) =>
        url.Host.Equals("magnitudle.com", StringComparison.OrdinalIgnoreCase)
        && url.AbsolutePath.TrimEnd('/').Equals(path, StringComparison.OrdinalIgnoreCase)
        && url.Query.Length == 0;

    [GeneratedRegex(@"https?://[^\s<>]+", RegexOptions.IgnoreCase)]
    private static partial Regex UrlPattern();
}
