using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.Help;

public class HelpFeature(
    IEnumerable<IMessageFeature> allFeatures,
    IChatClient chatClient)
    : BotCommandFeature
{
    public override string? HelpMessage => "`help` - prints this help message";

    protected override async Task<bool> TryHandleCommand(
        MessageIdentification commandMessageIdentification,
        string command,
        MessageIdentification? referenced,
        CancellationToken ct)
    {
        if (command != "help")
        {
            return false;
        }

        var destination = new MessageDestination(
            commandMessageIdentification.GuildId,
            commandMessageIdentification.ChannelId,
            commandMessageIdentification.MessageId
        );

        var helpMessages = allFeatures
            .OfType<BotCommandFeature>()
            .Select(f => f.HelpMessage)
            .Where(m => m != null)
            .Order();

        await chatClient.SendMessage(destination, string.Join('\n', helpMessages));

        return true;
    }
}
