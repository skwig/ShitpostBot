using System;

namespace ShitpostBot.Domain;

public sealed class LinkPost : Post
{
    public Link Link { get; private set; }

    private LinkPost()
    {
        // For EF
        Link = null!;
    }

    internal LinkPost(DateTimeOffset postedOn, ulong chatGuildId, ulong chatChannelId, ulong chatMessageId,
        ulong posterId,
        DateTimeOffset trackedOn, Link link)
        : base(PostType.Link, postedOn, chatGuildId, chatChannelId, chatMessageId, posterId, trackedOn)
    {
        Link = link;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="postedOn"></param>
    /// <param name="messageId"></param>
    /// <param name="posterId"></param>
    /// <param name="trackedOn"></param>
    /// <param name="link"></param>
    /// <returns></returns>
    public static LinkPost Create(DateTimeOffset postedOn, ChatMessageIdentifier messageId, PosterIdentifier posterId,
        DateTimeOffset trackedOn, Link link)
    {
        return new LinkPost(
            postedOn,
            messageId.GuildId, messageId.ChannelId, messageId.MessageId,
            posterId.Id,
            trackedOn,
            link
        );
    }
}