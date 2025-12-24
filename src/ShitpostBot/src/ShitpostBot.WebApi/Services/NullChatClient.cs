using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using ShitpostBot.Infrastructure;
using System.Text.Json;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.WebApi.Services;

public class NullChatClient(ILogger<NullChatClient> logger, IBotActionStore botActionStore)
    : IChatClient
{
    public IChatClientUtils Utils { get; } = new NullChatClientUtils();

    public event AsyncEventHandler<MessageCreateEventArgs>? MessageCreated;
    public event AsyncEventHandler<MessageDeleteEventArgs>? MessageDeleted;
    public event AsyncEventHandler<MessageUpdateEventArgs>? MessageUpdated;

    public Task ConnectAsync()
    {
        logger.LogInformation("NullChatClient.ConnectAsync - no-op");
        return Task.CompletedTask;
    }

    public async Task SendMessage(MessageDestination destination, string? messageContent)
    {
        logger.LogInformation("Would send message to {Destination}: {Content}", destination, messageContent);
        
        await botActionStore.StoreActionAsync(
            destination.ReplyToMessageId ?? 0,
            new TestAction(
                "message",
                JsonSerializer.Serialize(new { content = messageContent }),
                DateTimeOffset.UtcNow
            )
        );
    }

    public async Task SendMessage(MessageDestination destination, DiscordMessageBuilder messageBuilder)
    {
        logger.LogInformation("Would send message builder to {Destination}", destination);
        
        // Serialize embeds if present
        var embeds = messageBuilder.Embeds?.Select(e => new
        {
            title = e.Title,
            description = e.Description,
            thumbnail = e.Thumbnail?.Url?.ToString()
        }).ToList();
        
        await botActionStore.StoreActionAsync(
            destination.ReplyToMessageId ?? 0,
            new TestAction(
                "message",
                JsonSerializer.Serialize(new { 
                    content = messageBuilder.Content,
                    embeds = embeds
                }),
                DateTimeOffset.UtcNow
            )
        );
    }

    public async Task SendEmbeddedMessage(MessageDestination destination, DiscordEmbed embed)
    {
        logger.LogInformation("Would send embedded message to {Destination}", destination);
        
        await botActionStore.StoreActionAsync(
            destination.ReplyToMessageId ?? 0,
            new TestAction(
                "embed",
                JsonSerializer.Serialize(new { 
                    title = embed.Title, 
                    description = embed.Description 
                }),
                DateTimeOffset.UtcNow
            )
        );
    }

    public async Task React(MessageIdentification messageIdentification, string emoji)
    {
        logger.LogInformation("Would react to message {MessageId} with {Emoji}", 
            messageIdentification.MessageId, emoji);
        
        await botActionStore.StoreActionAsync(
            messageIdentification.MessageId,
            new TestAction(
                "reaction",
                JsonSerializer.Serialize(new { emoji }),
                DateTimeOffset.UtcNow
            )
        );
    }

    public Task<FetchedMessage?> GetMessageWithAttachmentsAsync(MessageIdentification messageIdentification)
    {
        logger.LogInformation("Would fetch message {MessageId}", messageIdentification.MessageId);
        // NullChatClient returns null since it doesn't have access to real Discord messages
        return Task.FromResult<FetchedMessage?>(null);
    }

    public Task<ulong?> FindReplyToMessage(MessageIdentification replyToMessage)
    {
        logger.LogInformation("Would find reply to message {MessageId}", replyToMessage.MessageId);
        // For testing: stub returns null (fallback to send new message)
        return Task.FromResult<ulong?>(null);
    }

    public async Task<bool> UpdateMessage(MessageIdentification messageToUpdate, DiscordMessageBuilder newContent)
    {
        logger.LogInformation("Would update message {MessageId}", messageToUpdate.MessageId);
        
        // Log the update action for testing verification
        var embeds = newContent.Embeds?.Select(e => new
        {
            title = e.Title,
            description = e.Description,
            thumbnail = e.Thumbnail?.Url?.ToString()
        }).ToList();
        
        await botActionStore.StoreActionAsync(
            messageToUpdate.MessageId,
            new TestAction(
                "message_update",
                JsonSerializer.Serialize(new { 
                    content = newContent.Content,
                    embeds = embeds
                }),
                DateTimeOffset.UtcNow
            )
        );
        
        // For testing: stub returns true (assume success)
        return true;
    }
}

public class NullChatClientUtils : IChatClientUtils
{
    public string Emoji(string name) => $":{name}:";
    public ulong ShitpostBotId() => 0;
    public string Mention(ulong posterId, bool useDesktop = false) => $"<@{posterId}>";
    public string RelativeTimestamp(DateTimeOffset timestamp) => timestamp.ToString("R");
}