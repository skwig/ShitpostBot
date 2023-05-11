using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Worker
{
    public class RepostMatchAllBotCommandHandler : IBotCommandHandler
    {
        private readonly IPostsReader postsReader;
        private readonly IChatClient chatClient;

        public RepostMatchAllBotCommandHandler(IPostsReader postsReader, IChatClient chatClient)
        {
            this.chatClient = chatClient;
            this.postsReader = postsReader;
        }

        public string? GetHelpMessage() => $"`repost match all` - shows maximum match value of the replied post with existing posts";

        public async Task<bool> TryHandle(MessageIdentification commandMessageIdentification, MessageIdentification? referencedMessageIdentification,
            BotCommand command)
        {
            if (command.Command != "repost match all")
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

            var post = await postsReader.All
                .Where(x => x.ChatMessageId == referencedMessageIdentification.MessageId)
                .SingleOrDefaultAsync();

            if (post == null)
            {
                await chatClient.SendMessage(
                    messageDestination,
                    "This post is not tracked"
                );

                return true;
            }

            await chatClient.SendMessage(messageDestination, $"Starting to match. Čekej píčo {chatClient.Utils.Emoji(":PauseChamp:")} ...");

            var allOtherPosts = await postsReader.All.Where(p => p.Id != post.Id).OrderBy(p => p.PostedOn).ToListAsync();
            var similarPosts = allOtherPosts
                .Select(p => new { Post = p, Similarity = post.GetSimilarityTo(p) })
                .OrderByDescending(p => p.Similarity)
                .Take(5);

            await chatClient.SendMessage(
                messageDestination,
                string.Join("\n",
                    similarPosts.Select((p, i) =>
                        $"{i + 1}. Match value of `{p.Similarity}` with https://discordapp.com/channels/{p.Post.ChatGuildId}/{p.Post.ChatChannelId}/{p.Post.ChatMessageId}"
                    )
                )
            );

            return true;
        }
    }
}