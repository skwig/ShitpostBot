using System;
using System.Web;

namespace ShitpostBot.Domain
{
    public sealed class Link : ValueObject<Link>
    {
        public string LinkId { get; private set; }
        public Uri LinkUri { get; private set; }
        public LinkProvider LinkProvider { get; private set; }

        private Link()
        {
        }

        internal Link(string linkId, Uri linkUri, LinkProvider linkProvider)
        {
            LinkId = linkId;
            LinkUri = linkUri;
            LinkProvider = linkProvider;
        }
        
        public double GetSimilarityTo(Link otherLink)
        {
            return LinkProvider == otherLink.LinkProvider && LinkId == otherLink.LinkId ? 1 : 0;
        }

        /// <summary>
        /// Returns null if link has no path (eg. www.google.com)
        /// </summary>
        /// <param name="linkUri"></param>
        /// <returns></returns>
        public static Link? CreateOrDefault(Uri linkUri)
        {
            LinkProvider linkProvider;
            string linkId;
            switch (linkUri.Host)
            {
                case "tenor.com":
                case "www.tenor.com":
                case "media.discordapp.net":
                {
                    return null;
                }
                case "www.youtube.com":
                case "youtube.com":
                {
                    linkProvider = LinkProvider.YouTube;
                    linkId = HttpUtility.ParseQueryString(linkUri.Query)["v"]!;
                    break;
                }
                case "www.youtu.be":
                case "youtu.be":
                {
                    linkProvider = LinkProvider.YouTube;
                    linkId = linkUri.LocalPath.Remove(0, 1);
                    break;
                }
                case "www.steamcommunity.com" when linkUri.LocalPath == "/sharedfiles/filedetails/":
                case "steamcommunity.com" when linkUri.LocalPath == "/sharedfiles/filedetails/":
                {
                    linkProvider = LinkProvider.SteamWorkshop;
                    linkId = HttpUtility.ParseQueryString(linkUri.Query)["id"]!;
                    break;
                }
                default:
                {
                    linkProvider = LinkProvider.Generic;
                    linkId = linkUri.LocalPath.Remove(0, 1);
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(linkId))
            {
                return null;
            }

            // TODO: better link whitelisting mechanism
            if (linkId == "9R4MtYRk6bA")
            {
                return null;
            }

            return new Link(linkId, linkUri, linkProvider);
        }
    }
}
