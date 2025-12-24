using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.Features.BotCommands;

public interface IBotCommandHandler
{
    public string? GetHelpMessage();
    public int GetHelpOrder() => 0;

    public Task<bool> TryHandle(
        MessageIdentification commandMessageIdentification, 
        MessageIdentification? referencedMessageIdentification, 
        BotCommand command,
        bool isEdit = false,
        ulong? botResponseMessageId = null);
}
