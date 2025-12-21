using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using MediatR;
using ShitpostBot.Application.Features.BotCommands.Redacted;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.BotCommands.Sus;

internal class SusMessageHandler(IChatClient chatClient) :
    INotificationHandler<TextMessageCreated>
{
    public async Task Handle(TextMessageCreated notification, CancellationToken cancellationToken)
    {
        if (notification.TextMessage.Content == null)
        {
            return;
        }

        var whitespaceRemovedMessageContent = Regex.Replace(notification.TextMessage.Content ?? "", @"\s+", "");
        var unaccentedMessageContent = RemoveDiacritics(whitespaceRemovedMessageContent);
        if (unaccentedMessageContent.Contains("sus", StringComparison.InvariantCultureIgnoreCase))
        {
            await chatClient.React(notification.TextMessage.Identification, ":sus:");
        }
    }

    private static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        text = text.Normalize(NormalizationForm.FormD);
        var chars = text.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray();
        return new string(chars).Normalize(NormalizationForm.FormC);
    }
}