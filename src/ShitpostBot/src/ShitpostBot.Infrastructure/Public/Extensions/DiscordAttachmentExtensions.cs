using System.Web;
using DSharpPlus.Entities;

namespace ShitpostBot.Infrastructure.Extensions;

public static class DiscordAttachmentExtensions
{
    extension(DiscordAttachment attachment)
    {
        /// <summary>
        /// Gets the appropriate URI for the attachment.
        /// For videos, returns a preview thumbnail URL with format=webp
        /// For images, returns the original URL.
        /// </summary>
        public Uri GetAttachmentUri()
        {
            if (attachment.IsVideo())
            {
                // Transform: cdn.discordapp.com -> media.discordapp.net with format=webp
                var builder = new UriBuilder(attachment.ProxyUrl);

                var query = HttpUtility.ParseQueryString(builder.Query);
                query["format"] = "webp";
                builder.Query = query.ToString();

                return builder.Uri;
            }

            return new Uri(attachment.Url);
        }

        /// <summary>
        /// Determines if the attachment is a video.
        /// </summary>
        public bool IsVideo()
        {
            return attachment.MediaType?.StartsWith("video/", StringComparison.OrdinalIgnoreCase)
                == true;
        }
    }
}
