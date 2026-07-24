using System.Text.RegularExpressions;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.Features.DailySlop;

public static partial class DailySlopHelper
{
    public static bool MessageHasUrl(IncomingMessage msg, string domain)
    {
        if (msg.Embeds.Any(e => e.Url.Host.Contains(domain, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (msg.Content != null)
        {
            var urlPattern = UrlPattern();
            var matches = urlPattern.Matches(msg.Content);

            foreach (Match match in matches)
            {
                if (match.Value.Contains(domain, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    [GeneratedRegex(
        @"https?://[^\s<>]+|(?:www\.)[a-zA-Z0-9-]+\.[a-zA-Z]{2,}(?:/[^\s<>]*)?",
        RegexOptions.IgnoreCase
    )]
    private static partial Regex UrlPattern();
}