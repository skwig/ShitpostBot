using System;
using CSharpFunctionalExtensions;

namespace ShitpostBot.Domain;

public sealed class WhitelistedPost : Entity<long>
{
    public long PostId { get; private set; }
    public ImagePost Post { get; private set; }
    public DateTimeOffset WhitelistedOn { get; private set; }
    public ulong WhitelistedById { get; private set; }

    private WhitelistedPost()
    {
        // For EF
    }
    
    internal WhitelistedPost(long postId, ImagePost post, DateTimeOffset whitelistedOn, ulong whitelistedById)
    {
        PostId = postId;
        Post = post;
        WhitelistedOn = whitelistedOn;
        WhitelistedById = whitelistedById;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="post"></param>
    /// <param name="whitelistedOn"></param>
    /// <param name="whitelistedById"></param>
    /// <returns></returns>
    public static WhitelistedPost Create(ImagePost post, DateTimeOffset whitelistedOn, ulong whitelistedById)
    {
        return new WhitelistedPost(post.Id, post, whitelistedOn, whitelistedById);
    }
}