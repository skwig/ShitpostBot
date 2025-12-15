using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using ShitpostBot.Infrastructure;
using System.Text.Json;

namespace ShitpostBot.WebApi.Services;

public class NullChatClient : IChatClient
{
    private readonly ILogger<NullChatClient> _logger;
    private readonly ITestActionLogger _actionLogger;

    public NullChatClient(ILogger<NullChatClient> logger, ITestActionLogger actionLogger)
    {
        _logger = logger;
        _actionLogger = actionLogger;
    }

    public IChatClientUtils Utils { get; } = new NullChatClientUtils();

    public event AsyncEventHandler<MessageCreateEventArgs>? MessageCreated;
    public event AsyncEventHandler<MessageDeleteEventArgs>? MessageDeleted;

    public Task ConnectAsync()
    {
        _logger.LogInformation("NullChatClient.ConnectAsync - no-op");
        return Task.CompletedTask;
    }

    public async Task SendMessage(MessageDestination destination, string? messageContent)
    {
        _logger.LogInformation("Would send message to {Destination}: {Content}", destination, messageContent);
        
        await _actionLogger.LogActionAsync(
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
        _logger.LogInformation("Would send message builder to {Destination}", destination);
        
        await _actionLogger.LogActionAsync(
            destination.ReplyToMessageId ?? 0,
            new TestAction(
                "message",
                JsonSerializer.Serialize(new { content = "[DiscordMessageBuilder content]" }),
                DateTimeOffset.UtcNow
            )
        );
    }

    public async Task SendEmbeddedMessage(MessageDestination destination, DiscordEmbed embed)
    {
        _logger.LogInformation("Would send embedded message to {Destination}", destination);
        
        await _actionLogger.LogActionAsync(
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
        _logger.LogInformation("Would react to message {MessageId} with {Emoji}", 
            messageIdentification.MessageId, emoji);
        
        await _actionLogger.LogActionAsync(
            messageIdentification.MessageId,
            new TestAction(
                "reaction",
                JsonSerializer.Serialize(new { emoji }),
                DateTimeOffset.UtcNow
            )
        );
    }
}

public class NullChatClientUtils : IChatClientUtils
{
    public string Emoji(string name) => $":{name}:";
    public ulong ShitpostBotId() => 0;
    public string Mention(ulong posterId, bool useDesktop = false) => $"<@{posterId}>";
    public string RelativeTimestamp(DateTimeOffset timestamp) => timestamp.ToString("R");
}