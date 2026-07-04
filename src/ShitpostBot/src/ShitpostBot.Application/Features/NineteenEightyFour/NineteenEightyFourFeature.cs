using System.Text.RegularExpressions;
using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.NineteenEightyFour;

public class NineteenEightyFourFeature(IChatClient chatClient) : BotCommandFeature(chatClient)
{
    public override string? HelpMessage => "`1984` - literally";

    protected override async Task<bool> TryHandleCommand(
        MessageIdentification commandMessageIdentification,
        string command,
        MessageIdentification? referenced,
        CancellationToken ct)
    {
        var r = new Regex(@"1984( \d*)*");

        var match = r.Match(command);
        if (!match.Success)
        {
            return false;
        }

        int? requestedLineNumber = null;
        if (match.Groups.TryGetValue("1", out var requestedLineText))
        {
            if (int.TryParse(requestedLineText.Value.Trim(), out var parsedInt))
            {
                requestedLineNumber = parsedInt;
            }
        }

        string[] lines;
        try
        {
            lines = await File.ReadAllLinesAsync("1984.txt", ct);
        }
        catch (FileNotFoundException)
        {
            var errDestination = new MessageDestination(
                commandMessageIdentification.GuildId,
                commandMessageIdentification.ChannelId,
                commandMessageIdentification.MessageId
            );

            await chatClient.SendMessage(errDestination, "1984.txt is not available");
            return true;
        }

        lines = lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();

        requestedLineNumber ??= new Random().Next(lines.Length);

        var destination = new MessageDestination(
            commandMessageIdentification.GuildId,
            commandMessageIdentification.ChannelId,
            commandMessageIdentification.MessageId
        );

        await chatClient.SendMessage(
            destination,
            $"`{requestedLineNumber}/{lines.Length}`\n" +
            $"{lines[requestedLineNumber.Value]}"
        );

        return true;
    }
}