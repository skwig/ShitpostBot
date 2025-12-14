using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.WebApi.Services;

public class NullChatClient(ILogger<NullChatClient> logger) : IChatClient
{
    public IChatClientUtils Utils { get; } = new NullChatClientUtils();

    public event AsyncEventHandler<MessageCreateEventArgs>? MessageCreated;
    public event AsyncEventHandler<MessageDeleteEventArgs>? MessageDeleted;

    public Task ConnectAsync()
    {
        logger.LogInformation("NullChatClient.ConnectAsync - no-op");
        return Task.CompletedTask;
    }

    public Task SendMessage(MessageDestination destination, string? messageContent)
    {
        logger.LogInformation("Would send message to {Destination}: {Content}", destination, messageContent);
        return Task.CompletedTask;
    }

    public Task SendMessage(MessageDestination destination, DiscordMessageBuilder messageBuilder)
    {
        logger.LogInformation("Would send message builder to {Destination}", destination);
        return Task.CompletedTask;
    }

    public Task SendEmbeddedMessage(MessageDestination destination, DiscordEmbed embed)
    {
        logger.LogInformation("Would send embedded message to {Destination}", destination);
        return Task.CompletedTask;
    }

    public Task React(MessageIdentification messageIdentification, string emoji)
    {
        logger.LogInformation("Would react to message {MessageId} with {Emoji}", 
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