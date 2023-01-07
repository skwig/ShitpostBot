using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.EventArgs;
using MediatR;
using Microsoft.Extensions.Logging;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Worker
{
    public class ChatMessageDeletedListener : IChatMessageDeletedListener
    {
        private readonly ILogger<ChatMessageDeletedListener> logger;
        private readonly IMediator mediator;

        public ChatMessageDeletedListener(ILogger<ChatMessageDeletedListener> logger, IMediator mediator)
        {
            this.logger = logger;
            this.mediator = mediator;
        }

        public async Task HandleMessageDeletedAsync(MessageDeleteEventArgs message)
        {
            // using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource();
            var cancellationToken = CancellationToken.None;

            if (message.Message?.Author == null)
            {
                return;
            }

            var isPosterBot = message.Message.Author.IsBot;
            var posterId = message.Message.Author.Id;
            var shitpostBotId = message.Guild.CurrentMember.Id;
            if (isPosterBot && (posterId != shitpostBotId))
            {
                return;
            }

            var messageIdentification = new MessageIdentification(message.Guild.Id, message.Channel.Id, message.Message.Author.Id, message.Message.Id);

            logger.LogDebug($"Deleted: '{message.Message.Id}' '{message.Message.Content}'");

            await TryHandleAsync(messageIdentification, message, cancellationToken);
        }

        private async Task<bool> TryHandleAsync(MessageIdentification messageIdentification, MessageDeleteEventArgs message,
            CancellationToken cancellationToken)
        {
            await mediator.Publish(new MessageDeleted(messageIdentification), cancellationToken);

            return true;
        }
    }
}
