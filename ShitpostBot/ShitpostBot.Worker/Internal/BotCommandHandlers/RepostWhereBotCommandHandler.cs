using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Worker
{
    public class RepostWhereBotCommandHandler : IBotCommandHandler
    {
        private readonly IPostsReader postsReader;
        private readonly IChatClient chatClient;
        private readonly IOptions<RepostServiceOptions> repostServiceOptions;

        public RepostWhereBotCommandHandler(IChatClient chatClient, IPostsReader postsReader, IOptions<RepostServiceOptions> repostServiceOptions)
        {
            this.chatClient = chatClient;
            this.postsReader = postsReader;
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

            var potentialRepost = await postsReader.All
                .Where(x => x.ChatMessageId == referencedMessageIdentification.MessageId)
                .SingleOrDefaultAsync();

            if (potentialRepost == null)
            {
                await chatClient.SendMessage(
                    messageDestination,
                    "This post is not tracked"
                );

                return true;
            }

            if (potentialRepost.Statistics == null)
            {
                await chatClient.SendMessage(
                    messageDestination,
                    "This post isn't evaluated yet"
                );
                return true;
            }

            if (potentialRepost.Statistics.MostSimilarTo == null
                || potentialRepost.Statistics.MostSimilarTo.Similarity < repostServiceOptions.Value.RepostSimilarityThreshold)
            {
                await chatClient.SendMessage(
                    messageDestination,
                    $"Not a repost"
                );
                return true;
            }

            var originalPost = await postsReader.All
                .Where(x => x.Id == potentialRepost.Statistics.MostSimilarTo.SimilarToPostId)
                .SingleOrDefaultAsync();

            if (originalPost == null)
            {
                await chatClient.SendMessage(
                    messageDestination,
                    $"Match value of `{potentialRepost.Statistics?.MostSimilarTo?.Similarity}` with post, that cannot be found"
                );
                return true;
            }

            var originalPostUri = new Uri(
                $"https://discordapp.com/channels/{originalPost.ChatGuildId}/{originalPost.ChatChannelId}/{originalPost.ChatMessageId}"
            );
            await chatClient.SendMessage(
                messageDestination,
                $"Match value of `{potentialRepost.Statistics?.MostSimilarTo?.Similarity}` with {originalPostUri}"
            );
            return true;
        }
    }
}