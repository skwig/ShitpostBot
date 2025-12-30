using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.Features.BotCommands;

public interface IBotCommandHandler
{
    string? GetHelpMessage();
    int GetHelpOrder() => 0;

    /// <param name="commandMessageIdentification"></param>
    /// <param name="referencedMessageIdentification"></param>
    /// <param name="command"></param>
    /// <param name="edit">Is notnull if this command is an edit of a previous command</param>
    Task<bool> TryHandle(
        MessageIdentification commandMessageIdentification,
        MessageIdentification? referencedMessageIdentification,
        BotCommand command,
        BotCommandEdit? edit);
}