using System;

namespace ShitpostBot.Domain
{
    public sealed class LinkPost : Post
    {
        private LinkPost()
        {
        }

        public LinkPost(DateTimeOffset postedOn, Uri postUri, ulong chatGuildId, ulong chatChannelId, ulong chatMessageId, ulong posterId,
            DateTimeOffset trackedOn, LinkPostContent content)
            : base(PostType.Link, postedOn, postUri, chatGuildId, chatChannelId, chatMessageId, posterId, trackedOn, content)
        {
        }

        // public override LinkPostContent Content { get; }
        public LinkPostContent LinkPostContent => (LinkPostContent)Content;

        public void SetLinkPostStatistics(LinkPostStatistics linkPostStatistics)
        {
            LinkPostContent.SetLinkPostStatistics(linkPostStatistics);
        }

        public override double GetSimilarityTo(Post other)
        {
            var otherLinkPost = other as LinkPost;
            if (otherLinkPost == null)
            {
                return 0;
            }

            return LinkPostContent.Link.GetSimilarityTo(otherLinkPost.LinkPostContent.Link);
        }
    }

    public class LinkPostContent : PostContent
    {
        public Link Link { get; private set; }
        public LinkPostStatistics? LinkPostStatistics { get; private set; }

        private LinkPostContent()
        {
        }

        public LinkPostContent(Link link) : base(PostType.Link)
        {
            Link = link;
        }

        public void SetLinkPostStatistics(LinkPostStatistics linkPostStatistics)
        {
            if (LinkPostStatistics != null)
            {
                throw new NotImplementedException("TODO: handle");
            }

            LinkPostStatistics = linkPostStatistics;
        }
    }
}