using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace ShitpostBot.Infrastructure
{
    public class DiscordChatClientOptions
    {
        public string Token { get; set; }
    }

    internal class DiscordChatClientUtils : IChatClientUtils
    {
        private readonly DiscordClient discordClient;

        public DiscordChatClientUtils(DiscordClient discordClient)
        {
            this.discordClient = discordClient;
        }
        
        public ulong ShitpostBotId()
        {
            return discordClient.CurrentUser.Id;
        }

        public string Emoji(string name)
        {
            try
            {
                return DiscordEmoji.FromName(discordClient, name);
            }
            catch
            {
                return name;
            }
        }

        public string Mention(ulong posterId, bool useDesktop = true) => useDesktop ? $"<@!{posterId}>" : $"<@{posterId}>";
    }

    internal class DiscordChatClient : IChatClient
    {
        private readonly DiscordClient discordClient;
        private readonly DiscordChatClientUtils utils;

        public DiscordChatClient(DiscordClient discordClient)
        {
            this.discordClient = discordClient;
            this.utils = new DiscordChatClientUtils(discordClient);
        }

        public async Task SendMessage(MessageDestination destination, string? messageContent)
        {
            var messageBuilder = new DiscordMessageBuilder()
                .WithContent(messageContent);

            await SendMessage(destination, messageBuilder);
        }

        public async Task SendEmbeddedMessage(MessageDestination destination, DiscordEmbed? discordEmbed)
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
}
