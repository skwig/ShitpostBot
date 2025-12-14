using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.WebApi.Services;

public class NullChatClient : IChatClient
{
    private readonly ILogger<NullChatClient> _logger;
    private readonly ITestEventPublisher _eventPublisher;

    public NullChatClient(ILogger<NullChatClient> logger, ITestEventPublisher eventPublisher)
    {
        _logger = logger;
        _eventPublisher = eventPublisher;
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
        
        await _eventPublisher.PublishAsync("chat-message", new
        {
            destination = new
            {
                guildId = destination.GuildId,
                channelId = destination.ChannelId,
                replyToMessageId = destination.ReplyToMessageId
            },
            content = messageContent
        });
    }

    public async Task SendMessage(MessageDestination destination, DiscordMessageBuilder messageBuilder)
    {
        _logger.LogInformation("Would send message builder to {Destination}", destination);
        
        await _eventPublisher.PublishAsync("chat-message", new
        {
            destination = new
            {
                guildId = destination.GuildId,
                channelId = destination.ChannelId,
                replyToMessageId = destination.ReplyToMessageId
            },
            content = "[DiscordMessageBuilder content]"
        });
    }

    public async Task SendEmbeddedMessage(MessageDestination destination, DiscordEmbed embed)
    {
        _logger.LogInformation("Would send embedded message to {Destination}", destination);
        
        await _eventPublisher.PublishAsync("chat-embed", new
        {
            destination = new
            {
                guildId = destination.GuildId,
                channelId = destination.ChannelId,
                replyToMessageId = destination.ReplyToMessageId
            },
            embedTitle = embed.Title,
            embedDescription = embed.Description
        });
    }

    public async Task React(MessageIdentification messageIdentification, string emoji)
    {
        _logger.LogInformation("Would react to message {MessageId} with {Emoji}", 
            messageIdentification.MessageId, emoji);
        
        await _eventPublisher.PublishAsync("chat-reaction", new
        {
            messageId = messageIdentification.MessageId,
            emoji = emoji
        });
    }
}

public class NullChatClientUtils : IChatClientUtils
{
    public string Emoji(string name) => $":{name}:";
    public ulong ShitpostBotId() => 0;
    public string Mention(ulong posterId, bool useDesktop = false) => $"<@{posterId}>";
    public string RelativeTimestamp(DateTimeOffset timestamp) => timestamp.ToString("R");
}