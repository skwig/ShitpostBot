using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Worker
{
    public class RepostWhereBotCommandHandler : IBotCommandHandler
    {
        private readonly IImagePostsReader imagePostsReader;
        private readonly IChatClient chatClient;
        private readonly IOptions<RepostServiceOptions> repostServiceOptions;

        public RepostWhereBotCommandHandler(IChatClient chatClient, IImagePostsReader imagePostsReader, IOptions<RepostServiceOptions> repostServiceOptions)
        {
            this.chatClient = chatClient;
            this.imagePostsReader = imagePostsReader;
            this.repostServiceOptions = repostServiceOptions;
        }

        public string GetHelpMessage() => $"`repost where` - links to the original post of the repost";

        public async Task<bool> TryHandle(MessageIdentification commandMessageIdentification, MessageIdentification? referencedMessageIdentification,
            BotCommand command)
        {
            if (command.Command != "repost where")
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
        
            var potentialRepost = await imagePostsReader.All
                .Where(x => x.ChatMessageId == referencedMessageIdentification.MessageId)
                .SingleOrDefaultAsync();

            if (potentialRepost == null)
            {
                await chatClient.SendMessage(
                    messageDestination,
                    "This post is not tracked. Maybe the resolution is too low?"
                );

                return true;
            }
        
            if (potentialRepost.ImagePostContent.ImagePostStatistics == null)
            {
                await chatClient.SendMessage(
                    messageDestination,
                    "This post is not tracked. Maybe the resolution is too low?"
                );

                return true;
            }

            if (potentialRepost.ImagePostContent.ImagePostStatistics == null 
                || potentialRepost.ImagePostContent.ImagePostStatistics.LargestSimilaritySoFar < repostServiceOptions.Value.RepostSimilarityThreshold)
            {
                await chatClient.SendMessage(
                    messageDestination,
                    $"Not a repost"
                );
                return true;
            }

            var originalPost = await imagePostsReader.All
                .Where(x => x.Id == potentialRepost.ImagePostContent.ImagePostStatistics.LargestSimilaritySoFarToId)
                .SingleOrDefaultAsync();

            if (originalPost == null)
            {
                await chatClient.SendMessage(
                    messageDestination,
                    $"Cannot find the original post"
                );    
            }
            
            await chatClient.SendMessage(
                messageDestination,
                $"Match value of `{potentialRepost.ImagePostContent.ImagePostStatistics?.LargestSimilaritySoFar}` with {originalPost.PostUri}"
            );
            
            return true;
        }
    }
}