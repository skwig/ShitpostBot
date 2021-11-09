using System;
using System.Web;

namespace ShitpostBot.Domain
{
    public sealed record Link : ValueObject<Link>
    {
        public string LinkId { get; private set; }
        public Uri LinkUri { get; private set; }
        public LinkProvider LinkProvider { get; private set; }

        private Link()
        {
        }

        public Link(Uri linkUri)
        {
            LinkProvider linkProvider;
            string linkId;
            switch (linkUri.Host)
            {
                case "streamable.com":
                    linkProvider = LinkProvider.Streamable;
                    linkId = linkUri.LocalPath.Remove(0, 1);
                    break;
                case "www.youtube.com":
                case "youtube.com":
                    linkProvider = LinkProvider.YouTube;
                    linkId = HttpUtility.ParseQueryString(linkUri.Query)["v"];
                    break;
                case "www.youtu.be":
                case "youtu.be":
                    linkProvider = LinkProvider.YouTube;
                    linkId = linkUri.LocalPath.Remove(0, 1);
                    break;
                default:
                    throw new ArgumentException(linkUri.Host);
            }

            LinkId = linkId;
            LinkUri = linkUri;
            LinkProvider = linkProvider;
        }

        public Link(string linkId, Uri linkUri, LinkProvider linkProvider)
        {
            LinkId = linkId;
            LinkUri = linkUri;
            LinkProvider = linkProvider;
        }

        public double GetSimilarityTo(Link otherLink)
        {
            return LinkProvider == otherLink.LinkProvider && LinkId == otherLink.LinkId ? 1 : 0;
        }
    }
}