namespace ShitpostBot.Application.Features.BotCommands;

public record BotCommand(string Command);

public record BotCommandEdit(ulong BotResponseMessageId);
