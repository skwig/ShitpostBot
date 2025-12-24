using System.ComponentModel.DataAnnotations;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Infrastructure;

public class DiscordChatClientOptions
{
    [Required] public required string Token { get; init; }
}

internal class DiscordChatClientUtils(DiscordClient client) : IChatClientUtils
{
    public ulong ShitpostBotId()
    {
        return client.CurrentUser.Id;
    }

    public string Emoji(string name)
    {
        try
        {
            return DiscordEmoji.FromName(client, name);
        }
        catch
        {
            return name;
        }
    }

    public string Mention(ulong posterId, bool useDesktop = true) => useDesktop ? $"<@!{posterId}>" : $"<@{posterId}>";

    public string RelativeTimestamp(DateTimeOffset timestamp) => $"<t:{(int)(timestamp.ToUnixTimeMilliseconds() / 1000)}:R>";
}

internal class DiscordChatClient(DiscordClient discordClient) : IChatClient
{
    private readonly DiscordChatClientUtils utils = new(discordClient);

    public async Task SendMessage(MessageDestination destination, string? messageContent)
    {
        var messageBuilder = new DiscordMessageBuilder();

        if (messageContent != null)
        {
            messageBuilder.WithContent(messageContent.Length > 2000 ? messageContent[..2000] : messageContent); // Max message length
        }

        await SendMessage(destination, messageBuilder);
    }

    public async Task SendEmbeddedMessage(MessageDestination destination, DiscordEmbed discordEmbed)
    {
        var channel = (await discordClient.GetGuildAsync(destination.GuildId))?.GetChannel(destination.ChannelId);
        if (channel == null)
        {
            throw new NotImplementedException();
        }

        await channel.SendMessageAsync(discordEmbed);
    }

    public async Task SendMessage(MessageDestination destination, DiscordMessageBuilder messageBuilder)
    {
        var channel = (await discordClient.GetGuildAsync(destination.GuildId))?.GetChannel(destination.ChannelId);
        if (channel == null)
        {
            throw new NotImplementedException();
        }

        if (destination.ReplyToMessageId != null)
        {
            messageBuilder = messageBuilder.WithReply(destination.ReplyToMessageId.Value);
        }

        await channel.SendMessageAsync(messageBuilder);
    }

    public async Task React(MessageIdentification messageIdentification, string emoji)
    {
        // TODO: abstract away behind CreateReaction(string)
        var channel = (await discordClient.GetGuildAsync(messageIdentification.GuildId))?.GetChannel(messageIdentification.ChannelId);
        if (channel == null)
        {
            throw new NotImplementedException();
        }

        var message = await channel.GetMessageAsync(messageIdentification.MessageId);
        if (message != null)
        {
            await message.CreateReactionAsync(DiscordEmoji.FromName(discordClient, emoji));
        }
    }

    public async Task<FetchedMessage?> GetMessageWithAttachmentsAsync(MessageIdentification messageIdentification)
    {
        try
        {
            var guild = await discordClient.GetGuildAsync(messageIdentification.GuildId);
            if (guild == null)
            {
                return null;
            }

            var channel = guild.GetChannel(messageIdentification.ChannelId);
            if (channel == null)
            {
                return null;
            }
        
            var message = await channel.GetMessageAsync(messageIdentification.MessageId);
            if (message == null)
            {
                return null;
            }
        
            var attachments = message.Attachments
                .Select(a => new MessageAttachment(a.Id, new Uri(a.Url)))
                .ToList();
        
            return new FetchedMessage(message.Id, attachments);
        }
        catch (DSharpPlus.Exceptions.NotFoundException)
        {
            return null;
        }
    }

    public IChatClientUtils Utils => utils;

    public Task ConnectAsync()
    {
        return discordClient.ConnectAsync();
    }

    public event AsyncEventHandler<MessageCreateEventArgs> MessageCreated
    {
        add => discordClient.MessageCreated += (_, args) => value.Invoke(args);
        remove => throw new NotImplementedException();
    }

    public event AsyncEventHandler<MessageDeleteEventArgs> MessageDeleted
    {
        add => discordClient.MessageDeleted += (_, args) => value.Invoke(args);
        remove => throw new NotImplementedException();
    }
}