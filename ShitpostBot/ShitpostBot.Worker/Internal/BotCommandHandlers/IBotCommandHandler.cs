using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Worker
{
    public interface IBotCommandHandler
    {
        public string GetHelpMessage();
        public int GetHelpOrder() => 0;

        public Task<bool> TryHandle(MessageIdentification commandMessageIdentification, MessageIdentification? referencedMessageIdentification, BotCommand command);
    }

    public record BotCommand(string Command);
}