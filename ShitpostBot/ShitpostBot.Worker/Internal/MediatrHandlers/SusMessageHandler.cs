﻿using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Worker;

internal class SusMessageHandler(IChatClient client) :
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
            await client.React(notification.TextMessage.Identification, ":sus:");
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