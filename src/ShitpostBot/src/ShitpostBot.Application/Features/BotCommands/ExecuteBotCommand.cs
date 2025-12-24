using MediatR;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.BotCommands;

/// <param name="Command">Is notnull if this command is an edit of a previous command</param>
public record ExecuteBotCommand(
    MessageIdentification Identification,
    MessageIdentification? ReferencedMessageIdentification,
    BotCommand Command,
    BotCommandEdit? Edit = null) : IRequest<Unit>;

public class ExecuteBotCommandHandler(
    ILogger<ExecuteBotCommandHandler> logger,
    IChatClient chatClient,
    IEnumerable<IBotCommandHandler> commandHandlers)
    : IRequestHandler<ExecuteBotCommand, Unit>
{
    public async Task<Unit> Handle(ExecuteBotCommand request, CancellationToken cancellationToken)
    {
        var (messageIdentification, referencedMessageIdentification, command, edit) = request;

        try
        {
            var handled = false;
            foreach (var botCommandHandler in commandHandlers)
            {
                var thisBotCommandHandled = await botCommandHandler.TryHandle(
                    messageIdentification,
                    referencedMessageIdentification,
                    command,
                    edit);

                if (thisBotCommandHandled)
                {
                    if (handled)
                    {
                        logger.LogError("Multiple command handlers handled '{Command}'", command);
                    }

                    handled = thisBotCommandHandled;
                }
            }

            if (!handled)
            {
                await chatClient.SendMessage(
                    new MessageDestination(
                        messageIdentification.GuildId,
                        messageIdentification.ChannelId,
                        messageIdentification.MessageId),
                    $"I don't know how to '{command.Command}'"
                );
            }
        }
        catch (Exception e)
        {
            await chatClient.SendMessage(
                new MessageDestination(
                    messageIdentification.GuildId,
                    messageIdentification.ChannelId,
                    messageIdentification.MessageId),
                e.ToString()
            );
        }

        return Unit.Value;
    }
}