using System;

namespace ShitpostBot.Domain
{
    public enum PostType
    {
        Image = 0,
        Link = 1
    }
    
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
        public virtual PostContent Content { get; }
        public PostStatistics? Statistics { get; private set; }

        protected Post()
        {
        }
        
        protected Post(PostType type, DateTimeOffset postedOn, ulong chatGuildId, ulong chatChannelId, ulong chatMessageId, ulong posterId, DateTimeOffset trackedOn, PostContent content)
        {
            Type = type;
            PostedOn = postedOn;
            ChatGuildId = chatGuildId;
            ChatChannelId = chatChannelId;
            ChatMessageId = chatMessageId;
            PosterId = posterId;
            Content = content;
            TrackedOn = trackedOn;
        }

        public abstract double GetSimilarityTo(Post other);

        public void SetPostStatistics(PostStatistics statistics)
        {
            if (Statistics != null)
            {
                throw new NotImplementedException("TODO: handle");
            }

            Statistics = statistics;
        }
        
        public override string ToString()
        {
            return $"{nameof(Type)}: {Type}, {nameof(PostedOn)}: {PostedOn}, {nameof(ChatMessageId)}: {ChatMessageId}, {nameof(PosterId)}: {PosterId}, {nameof(TrackedOn)}: {TrackedOn}, {nameof(EvaluatedOn)}: {EvaluatedOn}, {nameof(Content)}: {Content}";
        }
    }
    
    public abstract class PostContent
    {
        public PostType Type { get; private set; }

        protected PostContent()
        {
        }
        
        protected PostContent(PostType type)
        {
            Type = type;
        }
    }
}