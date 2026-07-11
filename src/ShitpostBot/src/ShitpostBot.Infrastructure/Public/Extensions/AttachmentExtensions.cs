using System.Web;

namespace ShitpostBot.Infrastructure.Extensions;

public static class AttachmentExtensions
{
    extension(Attachment attachment)
    {
        /// <summary>
        /// Determines if the attachment is an image or video suitable for processing.
        /// </summary>
        public bool IsImageOrVideo()
        {
            return IsImage(attachment) || IsVideo(attachment);
        }

        /// <summary>
        /// Determines if the attachment is an image
        /// </summary>
        public bool IsImage()
        {
            return attachment.MediaType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
                == true;
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