using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.SugmaBalls;

public class SugmaBallsCommand(IChatClient chatClient) : BotCommandFeature(chatClient)
{
    public override string? HelpMessage => null;

    protected override async Task<bool> TryHandleCommand(
        MessageIdentification commandMessageIdentification,
        string command,
        MessageIdentification? referenced,
        CancellationToken ct)
    {
        if (command != "sugma balls")
        {
            return false;
        }

        var destination = new MessageDestination(
            commandMessageIdentification.GuildId,
            commandMessageIdentification.ChannelId,
            commandMessageIdentification.MessageId
        );

        await chatClient.SendMessage(
            destination,
            chatClient.Utils.Emoji(":face_with_raised_eyebrow:")
        );

        return true;
    }
}