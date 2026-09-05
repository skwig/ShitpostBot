namespace ShitpostBot.Infrastructure;

public static class Tags
{
    public static class Messaging
    {
        public const string System = "messaging.system";
    }

    public static class Discord
    {
        public static class Guild
        {
            public const string Id = "discord.guild.id";
        }

        public static class Channel
        {
            public const string Id = "discord.channel.id";
        }

        public static class Message
        {
            public const string Id = "discord.message.id";
        }

        public static class User
        {
            public const string Id = "discord.user.id";
        }
    }

    public static class ShitpostBot
    {
        public static class ImagePost
        {
            public const string Id = "shitpostbot.image_post.id";
        }

        public static class LinkPost
        {
            public const string Id = "shitpostbot.link_post.id";
        }

        public const string Reevaluation = "shitpostbot.reevaluation";

        public static class Repost
        {
            public const string Outcome = "shitpostbot.repost.outcome";

            public static class Match
            {
                public static class ImagePost
                {
                    public const string Id = "shitpostbot.repost.match.image_post.id";
                }

                public static class LinkPost
                {
                    public const string Id = "shitpostbot.repost.match.link_post.id";
                }

                public const string Similarity = "shitpostbot.repost.match.similarity";
            }
        }
    }
}
