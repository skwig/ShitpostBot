using DSharpPlus.AsyncEvents;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace ShitpostBot.Infrastructure.Services;

public delegate Task AsyncEventHandler<in TArgs>(TArgs e) where TArgs : AsyncEventArgs;

public record MessageAttachment(ulong Id, Uri Url);

public record FetchedMessage(ulong MessageId, IReadOnlyList<MessageAttachment> Attachments);

public interface IChatClientUtils
{
    public string Emoji(string name);
    public ulong ShitpostBotId();
    public string Mention(ulong posterId, bool useDesktop = false);
    public string RelativeTimestamp(DateTimeOffset timestamp);
}

public interface IChatClient
{
    public IChatClientUtils Utils { get; }
    Task ConnectAsync();

    event AsyncEventHandler<MessageCreateEventArgs> MessageCreated;
    event AsyncEventHandler<MessageDeleteEventArgs> MessageDeleted;
    Task SendMessage(MessageDestination destination, string? messageContent);
    Task SendMessage(MessageDestination destination, DiscordMessageBuilder messageBuilder);
    Task SendEmbeddedMessage(MessageDestination destination, DiscordEmbed embed);
    Task React(MessageIdentification messageIdentification, string emoji);
    
    /// <summary>
    /// Returns null if channel or message not found.
    /// </summary>
    Task<FetchedMessage?> GetMessageWithAttachmentsAsync(MessageIdentification messageIdentification);
}

public interface IChatMessageCreatedListener
{
    public Task HandleMessageCreatedAsync(MessageCreateEventArgs message);
}

public interface IChatMessageDeletedListener
{
    /// <summary>
    /// This is invoked only if the message was received during the runtime.
    /// Therefore this doesn't work for messages deleted before startup
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public Task HandleMessageDeletedAsync(MessageDeleteEventArgs message);
}