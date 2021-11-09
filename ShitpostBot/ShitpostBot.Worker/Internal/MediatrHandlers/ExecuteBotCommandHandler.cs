using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Worker
{
    public record ExecuteBotCommand
        (MessageIdentification Identification, MessageIdentification? ReferencedMessageIdentification, BotCommand Command) : IRequest;

    public class ExecuteBotCommandHandler : IRequestHandler<ExecuteBotCommand>
    {
        private readonly ILogger<ExecuteBotCommandHandler> logger;
        private readonly IChatClient chatClient;
        private readonly IEnumerable<IBotCommandHandler> botCommandHandlers;

        public ExecuteBotCommandHandler(ILogger<ExecuteBotCommandHandler> logger, IChatClient chatClient, IEnumerable<IBotCommandHandler> botCommandHandlers)
        {
            this.logger = logger;
            this.chatClient = chatClient;
            this.botCommandHandlers = botCommandHandlers;
        }

        public async Task<Unit> Handle(ExecuteBotCommand request, CancellationToken cancellationToken)
        {
            var (messageIdentification, referencedMessageIdentification, command) = request;

            try
            {
                var handled = false;
                foreach (var botCommandHandler in botCommandHandlers)
                {
                    var thisBotCommandHandled = await botCommandHandler.TryHandle(messageIdentification, referencedMessageIdentification, command);

                    if (thisBotCommandHandled)
                    {
                        if (handled)
                        {
                            logger.LogError("Multiple command handlers handled '{command}'", command);
                        }

                        handled = thisBotCommandHandled;
                    }
                }

                if (!handled)
                {
                    await chatClient.SendMessage(
                        new MessageDestination(messageIdentification.GuildId, messageIdentification.ChannelId, messageIdentification.MessageId),
                        $"I don't know how to '{command.Command}'"
                    );
                }
            }
            catch (Exception e)
            {
                await chatClient.SendMessage(
                    new MessageDestination(messageIdentification.GuildId, messageIdentification.ChannelId, messageIdentification.MessageId),
                    e.ToString()
                );
            }

            return Unit.Value;
        }
    }
}