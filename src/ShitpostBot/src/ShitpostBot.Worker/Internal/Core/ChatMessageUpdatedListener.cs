using DSharpPlus.EventArgs;
using MediatR;
using ShitpostBot.Infrastructure;
using ShitpostBot.Application.Features.BotCommands;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Worker.Core;

public class ChatMessageUpdatedListener(
    ILogger<ChatMessageUpdatedListener> logger,
    IChatClient chatClient,
    IMediator mediator)
    : IChatMessageUpdatedListener
{
    public async Task HandleMessageUpdatedAsync(MessageUpdateEventArgs message)
    {
        var cancellationToken = CancellationToken.None;

        // Ignore bot messages
        if (message.Author.IsBot)
        {
            return;
        }

        var messageIdentification = new MessageIdentification(
            message.Guild.Id,
            message.Channel.Id,
            message.Author.Id,
            message.Message.Id);

        logger.LogDebug("Updated: '{MessageId}' '{MessageContent}'", 
            message.Message.Id, message.Message.Content);

        // Check if edited message is a bot command
        var startsWithThisBotTag =
            message.Message.Content.StartsWith(chatClient.Utils.Mention(message.Guild.CurrentMember.Id, true))
            || message.Message.Content.StartsWith(chatClient.Utils.Mention(message.Guild.CurrentMember.Id, false));

        if (!startsWithThisBotTag)
        {
            return; // Not a bot command, ignore edit
        }

        // Extract command (same logic as ChatMessageCreatedListener)
        var command = string.Join(' ',
            message.Message.Content.Split(" ", StringSplitOptions.RemoveEmptyEntries).Skip(1));

        if (string.IsNullOrWhiteSpace(command))
        {
            return; // Empty command, ignore
        }

        // Try to find the bot's response to this message
        var botResponseMessageId = await chatClient.FindReplyToMessage(messageIdentification);

        // Get referenced message if any
        var referencedMessageIdentification = message.Message.Reference != null
            ? new MessageIdentification(
                message.Message.Reference.Guild.Id,
                message.Message.Reference.Channel.Id,
                message.Message.Reference.Message.Author.Id,
                message.Message.Reference.Message.Id
            )
            : null;

        // Execute command with IsEdit=true and optional BotResponseMessageId
        await mediator.Send(
            new ExecuteBotCommand(
                messageIdentification,
                referencedMessageIdentification,
                new BotCommand(command),
                IsEdit: true,
                BotResponseMessageId: botResponseMessageId
            ),
            cancellationToken
        );
    }
}
