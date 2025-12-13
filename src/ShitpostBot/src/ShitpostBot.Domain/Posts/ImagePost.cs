using System;

namespace ShitpostBot.Domain;

public sealed class ImagePost : Post
{
    public Image Image { get; private set; }

    private ImagePost()
    {
        // For EF
        Image = null!;
    }

    internal ImagePost(DateTimeOffset postedOn, ulong chatGuildId, ulong chatChannelId, ulong chatMessageId,
        ulong posterId,
        DateTimeOffset trackedOn, Image image)
        : base(PostType.Image, postedOn, chatGuildId, chatChannelId, chatMessageId, posterId, trackedOn)
    {
        Image = image;
    }

    public void SetImageFeatures(ImageFeatures imageFeatures, DateTimeOffset utcNow)
    {
        Image = Image.WithImageFeatures(imageFeatures);
        EvaluatedOn = utcNow;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="postedOn"></param>
    /// <param name="messageId"></param>
    /// <param name="posterId"></param>
    /// <param name="trackedOn"></param>
    /// <param name="image"></param>
    /// <returns></returns>
    public static ImagePost Create(DateTimeOffset postedOn, ChatMessageIdentifier messageId, PosterIdentifier posterId,
        DateTimeOffset trackedOn, Image image)
    {
        return new ImagePost(
            postedOn,
            messageId.GuildId, messageId.ChannelId, messageId.MessageId,
            posterId.Id,
            trackedOn,
            image
        );
    }
}