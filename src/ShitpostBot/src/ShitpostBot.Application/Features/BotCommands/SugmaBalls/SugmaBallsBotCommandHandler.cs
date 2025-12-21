using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.BotCommands.SugmaBalls;

public class SugmaBallsBotCommandHandler(IChatClient chatClient) : IBotCommandHandler
{
    public string? GetHelpMessage() => null;

    public async Task<bool> TryHandle(MessageIdentification commandMessageIdentification, MessageIdentification? referencedMessageIdentification,
        BotCommand command)
    {
        if (command.Command != "sugma balls")
        {
            return false;
        }

        var messageDestination = new MessageDestination(
            commandMessageIdentification.GuildId,
            commandMessageIdentification.ChannelId,
            commandMessageIdentification.MessageId
        );

        await chatClient.SendMessage(
            messageDestination,
            chatClient.Utils.Emoji(":face_with_raised_eyebrow:")
        );

        return true;
    }
}