using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ShitpostBot.Infrastructure;
using ShitpostBot.Worker.Core;

namespace ShitpostBot.Worker.Features.NineteenEightyFour;

public class NineteenEightyFourBotCommandHandler(IChatClient chatClient) : IBotCommandHandler
{
    public string? GetHelpMessage() => $"`1984` - literally";

    public async Task<bool> TryHandle(MessageIdentification commandMessageIdentification, MessageIdentification? referencedMessageIdentification,
        BotCommand command)
    {
        var r = new Regex(@"1984( \d*)*");

        var match = r.Match(command.Command);
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

        var lines = (await File.ReadAllLinesAsync("1984.txt")).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();

        requestedLineNumber ??= new Random().Next(lines.Count);

        var messageDestination = new MessageDestination(
            commandMessageIdentification.GuildId,
            commandMessageIdentification.ChannelId,
            commandMessageIdentification.MessageId
        );
            
        await chatClient.SendMessage(
            messageDestination,
            $"`{requestedLineNumber}/{lines.Count}`\n" + 
            $"{lines[requestedLineNumber.Value]}"
        );
        
        return true;
    }
}