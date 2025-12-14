using DSharpPlus.AsyncEvents;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.WebApi.Services;

public class NullChatClient : IChatClient
{
    private readonly ILogger<NullChatClient> _logger;
    
    public NullChatClient(ILogger<NullChatClient> logger)
    {
        _logger = logger;
        Utils = new NullChatClientUtils();
    }
    
    public IChatClientUtils Utils { get; }

    public event AsyncEventHandler<MessageCreateEventArgs>? MessageCreated;
    public event AsyncEventHandler<MessageDeleteEventArgs>? MessageDeleted;

    public Task ConnectAsync()
    {
        _logger.LogInformation("NullChatClient.ConnectAsync - no-op");
        return Task.CompletedTask;
    }

    public Task SendMessage(MessageDestination destination, string? messageContent)
    {
        _logger.LogInformation("Would send message to {Destination}: {Content}", destination, messageContent);
        return Task.CompletedTask;
    }

    public Task SendMessage(MessageDestination destination, DiscordMessageBuilder messageBuilder)
    {
        _logger.LogInformation("Would send message builder to {Destination}", destination);
        return Task.CompletedTask;
    }

    public Task SendEmbeddedMessage(MessageDestination destination, DiscordEmbed embed)
    {
        _logger.LogInformation("Would send embedded message to {Destination}", destination);
        return Task.CompletedTask;
    }

    public Task React(MessageIdentification messageIdentification, string emoji)
    {
        _logger.LogInformation("Would react to message {MessageId} with {Emoji}", 
            messageIdentification.MessageId, emoji);
        return Task.CompletedTask;
    }
}

public class NullChatClientUtils : IChatClientUtils
{
    public string Emoji(string name) => $":{name}:";
    public ulong ShitpostBotId() => 0;
    public string Mention(ulong posterId, bool useDesktop = false) => $"<@{posterId}>";
    public string RelativeTimestamp(DateTimeOffset timestamp) => timestamp.ToString("R");
}