using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using MediatR;
using Microsoft.Extensions.Logging;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Worker
{
    public class ChatMessageCreatedListener : IChatMessageCreatedListener
    {
        private readonly ILogger<ChatMessageCreatedListener> logger;
        private readonly IChatClient chatClient;
        private readonly IMediator mediator;

        public ChatMessageCreatedListener(ILogger<ChatMessageCreatedListener> logger, IChatClient chatClient, IMediator mediator)
        {
            this.logger = logger;
            this.chatClient = chatClient;
            this.mediator = mediator;
        }

        public async Task HandleMessageCreatedAsync(MessageCreateEventArgs message)
        {
            // using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource();
            var cancellationToken = CancellationToken.None;

            var isPosterBot = message.Author.IsBot;
            if (isPosterBot)
            {
                return;
            }

            var messageIdentification = new MessageIdentification(message.Guild.Id, message.Channel.Id, message.Author.Id, message.Message.Id);
            var referencedMessageIdentification = message.Message.Reference != null
                ? new MessageIdentification(
                    message.Message.Reference.Guild.Id,
                    message.Message.Reference.Channel.Id,
                    message.Message.Reference.Message.Author.Id,
                    message.Message.Reference.Message.Id
                )
                : null;

            // var messageIdentification = new MessageIdentification(message.Re.Id, message.Channel.Id, message.Author.Id, message.Message.Id);

            logger.LogDebug($"Created: '{message.Message.Id}' '{message.Message.Content}'");

            if (await TryHandleBotCommandAsync(messageIdentification, referencedMessageIdentification, message, cancellationToken)) return;
            if (await TryHandleImageAsync(messageIdentification, message, cancellationToken)) return;
            if (await TryHandleTextAsync(messageIdentification, message, cancellationToken)) return;
        }

        private async Task<bool> TryHandleBotCommandAsync(MessageIdentification messageIdentification, MessageIdentification? referencedMessageIdentification,
            MessageCreateEventArgs message,
            CancellationToken cancellationToken)
        {
            var startsWithThisBotTag = message.Message.Content.StartsWith(chatClient.Utils.Mention(message.Guild.CurrentMember.Id, true))
                                       || message.Message.Content.StartsWith(chatClient.Utils.Mention(message.Guild.CurrentMember.Id, false));
            if (!startsWithThisBotTag)
            {
                return false;
            }

            var command = string.Join(' ', message.Message.Content.Split(" ", StringSplitOptions.RemoveEmptyEntries).Skip(1)); // Ghetto SubstringAfter

            if (string.IsNullOrWhiteSpace(command))
            {
                return false;
            }

            await mediator.Send(new ExecuteBotCommand(messageIdentification, referencedMessageIdentification, new BotCommand(command)),
                cancellationToken
            );

            return true;
        }

        private async Task<bool> TryHandleImageAsync(MessageIdentification messageIdentification, MessageCreateEventArgs message,
            CancellationToken cancellationToken)
        {
            var imageAttachments = message.Message.Attachments.Where(a => IsImage(a)).Where(a => a.Height >= 299 && a.Width >= 299).ToList();
            if (!imageAttachments.Any())
            {
                return false;
            }

            // attachment url pattern: channelId/messageId/attachmentId
            foreach (var i in imageAttachments)
            {
                var attachment = new ImageMessageAttachment(i.Id, i.FileName, new Uri(i.Url));
                await mediator.Publish(new ImageMessageCreated(new ImageMessage(messageIdentification, attachment, message.Message.CreationTimestamp)),
                    cancellationToken
                );
            }

            return true;
        }

        private async Task<bool> TryHandleTextAsync(MessageIdentification messageIdentification, MessageCreateEventArgs message,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(message.Message.Content))
            {
                return false;
            }

            await mediator.Publish(
                new TextMessageCreated(new TextMessage(messageIdentification, message.Message.Content, message.Message.CreationTimestamp)),
                cancellationToken
            );

            return true;
        }

        private bool IsImage(DiscordAttachment discordAttachment)
        {
            // TODO: move to specific service
            return discordAttachment.FileName.EndsWith(".png") || discordAttachment.FileName.EndsWith(".jpg") || discordAttachment.FileName.EndsWith(".jpeg");
        }
    }
}