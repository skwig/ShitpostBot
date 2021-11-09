using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Worker
{
    public class RepostMatchBotCommandHandler : IBotCommandHandler
    {
        private readonly IImagePostsReader imagePostsReader;
        private readonly IChatClient chatClient;

        public RepostMatchBotCommandHandler(IImagePostsReader imagePostsReader, IChatClient chatClient)
        {
            this.chatClient = chatClient;
            this.imagePostsReader = imagePostsReader;
        }

        public string GetHelpMessage() => $"`repost match` - shows maximum match value of the replied post with existing posts";

        public async Task<bool> TryHandle(MessageIdentification commandMessageIdentification, MessageIdentification? referencedMessageIdentification,
            BotCommand command)
        {
            if (command.Command != "repost match")
            {
                return false;
            }

            var messageDestination = new MessageDestination(
                commandMessageIdentification.GuildId,
                commandMessageIdentification.ChannelId,
                commandMessageIdentification.MessageId
            );
        
            if (referencedMessageIdentification == null)
            {
                await chatClient.SendMessage(
                    messageDestination,
                    "Invalid usage: you need to reply to a post to get the match value"
                );

                return true;
            }
        
            var imagePost = await imagePostsReader.All
                .Where(x => x.ChatMessageId == referencedMessageIdentification.MessageId)
                .SingleOrDefaultAsync();

            if (imagePost == null)
            {
                await chatClient.SendMessage(
                    messageDestination,
                    "This post is not tracked. Maybe the resolution is too low?"
                );

                return true;
            }
        
            await chatClient.SendMessage(
                messageDestination,
                $"Match value of `{imagePost.ImagePostContent.ImagePostStatistics?.LargestSimilaritySoFar ?? 0m}` with existing posts"
            );
        
            return true;
        }
    }
}