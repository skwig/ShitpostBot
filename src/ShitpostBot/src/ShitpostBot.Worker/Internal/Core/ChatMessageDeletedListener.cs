using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.EventArgs;
using MediatR;
using Microsoft.Extensions.Logging;
using ShitpostBot.Application.Features.BotCommands.Redacted;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Worker.Core;

public class ChatMessageDeletedListener(ILogger<ChatMessageDeletedListener> logger, IMediator mediator) : IChatMessageDeletedListener
{
    public async Task HandleMessageDeletedAsync(MessageDeleteEventArgs message)
    {
        // using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource();
        var cancellationToken = CancellationToken.None;

        if (message.Message?.Author == null)
        {
            return;
        }

        var isPosterBot = message.Message.Author.IsBot;
        if (isPosterBot)
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