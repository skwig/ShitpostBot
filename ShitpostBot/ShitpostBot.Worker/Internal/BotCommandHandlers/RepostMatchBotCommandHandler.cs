using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Worker;

public class RepostMatchBotCommandHandler : IBotCommandHandler
{
    private readonly IPostsReader postsReader;
    private readonly IChatClient chatClient;

    public RepostMatchBotCommandHandler(IPostsReader postsReader, IChatClient chatClient)
    {
        this.chatClient = chatClient;
        this.postsReader = postsReader;
    }

    public string? GetHelpMessage() => $"`repost match` - shows maximum match value of the replied post with existing posts during the repost window";

    public async Task<bool> TryHandle(MessageIdentification commandMessageIdentification, MessageIdentification? referencedMessageIdentification,
        BotCommand command)
    {
        if (command.Command != "repost match" || command.Command != "repost where")
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
        
        var post = await postsReader.All()
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
        
        // var originalPost = await postsReader.All()
        //     .Where(x => x.Id == post.Statistics.MostSimilarTo.SimilarToPostId)
        //     .SingleOrDefaultAsync();
        //
        // if (originalPost == null)
        // {
        //     await chatClient.SendMessage(
        //         messageDestination,
        //         $"Match value of `{post.Statistics?.MostSimilarTo?.Similarity}` with post, that cannot be found"
        //     );
        //     return true;
        // }
        //     
        // var originalPostUri = new Uri(
        //     $"https://discordapp.com/channels/{originalPost.ChatGuildId}/{originalPost.ChatChannelId}/{originalPost.ChatMessageId}"
        // );
        // await chatClient.SendMessage(
        //     messageDestination,
        //     $"Match value of `{post.Statistics?.MostSimilarTo?.Similarity}` with {originalPostUri}"
        // );
        
        return true;
    }
}