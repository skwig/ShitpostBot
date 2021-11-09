using System;
using System.Net.Mime;

namespace ShitpostBot.Domain
{
    public sealed class ImagePost : Post
    {
        private ImagePost()
        {
        }

        public ImagePost(DateTimeOffset postedOn, Uri postUri, ulong chatGuildId, ulong chatChannelId, ulong chatMessageId, ulong posterId,
            DateTimeOffset trackedOn, ImagePostContent content)
            : base(PostType.Image, postedOn, postUri, chatGuildId, chatChannelId, chatMessageId, posterId, trackedOn, content)
        {
        }

        // public override ImagePostContent Content { get; }
        public ImagePostContent ImagePostContent => (ImagePostContent)Content;

        public override double GetSimilarityTo(Post other)
        {
            var otherImagePost = other as ImagePost;
            if (otherImagePost == null)
            {
                return 0;
            }

            return ImagePostContent.Image.GetSimilarityTo(otherImagePost.ImagePostContent.Image);
        }
    }

    public class ImagePostContent : PostContent
    {
        public Image Image { get; set; } // TODO?
        public ImagePostStatistics? ImagePostStatistics { get; private set; }

        private ImagePostContent()
        {
        }

        public ImagePostContent(Image image) : base(PostType.Image)
        {
            Image = image;
        }

        public void SetImagePostStatistics(ImagePostStatistics imagePostStatistics)
        {
            if (ImagePostStatistics != null)
            {
                throw new NotImplementedException("TODO: handle");
            }

            ImagePostStatistics = imagePostStatistics;
        }
    }
}