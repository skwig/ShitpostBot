using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.EventArgs;
using Emzi0767.Utilities;

namespace ShitpostBot.Infrastructure
{
    public delegate Task AsyncEventHandler<in TArgs>(TArgs e) where TArgs : AsyncEventArgs;

    public interface IChatClientUtils
    {
        public string Emoji(string name);
        public string Mention(ulong posterId, bool useDesktop = false);
    }

    public interface IChatClient
    {
        public IChatClientUtils Utils { get; }
        Task ConnectAsync();

        event AsyncEventHandler<MessageCreateEventArgs> MessageCreated;
        event AsyncEventHandler<MessageDeleteEventArgs> MessageDeleted;
        Task SendMessage(MessageDestination destination, string? messageContent);
        Task React(MessageIdentification messageIdentification, string emoji);
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
}