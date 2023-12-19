using System;
using CSharpFunctionalExtensions;

namespace ShitpostBot.Domain;
    
public abstract class Post : Entity<long>
{
    public PostType Type { get; private set; }
    public DateTimeOffset PostedOn { get; private set; }
    public ulong ChatGuildId { get; private set; } // TODO: "External identification" or sth
    public ulong ChatChannelId { get; private set; }
    public ulong ChatMessageId { get; private set; }
    public ulong PosterId { get; private set; }
    public DateTimeOffset TrackedOn { get; private set; }
    public DateTimeOffset? EvaluatedOn { get; protected set; }

    protected Post()
    {
        // For EF
    }
        
    protected Post(PostType type, DateTimeOffset postedOn, ulong chatGuildId, ulong chatChannelId, ulong chatMessageId, ulong posterId, DateTimeOffset trackedOn)
    {
        Type = type;
        PostedOn = postedOn;
        ChatGuildId = chatGuildId;
        ChatChannelId = chatChannelId;
        ChatMessageId = chatMessageId;
        PosterId = posterId;
        TrackedOn = trackedOn;
    }
        
    public override string ToString()
    {
        return $"{nameof(Type)}: {Type}, {nameof(PostedOn)}: {PostedOn}, {nameof(ChatMessageId)}: {ChatMessageId}, {nameof(PosterId)}: {PosterId}, {nameof(TrackedOn)}: {TrackedOn}, {nameof(EvaluatedOn)}: {EvaluatedOn}";
    }
}

public enum PostType
{
    Image = 0,
    Link = 1
}

public record ChatMessageIdentifier(ulong GuildId, ulong ChannelId, ulong MessageId);

public record PosterIdentifier(ulong Id);