using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Worker
{
    internal class SusMessageHandler :
        INotificationHandler<TextMessageCreated>
    {
        private readonly IChatClient chatClient;

        public SusMessageHandler(IChatClient chatClient)
        {
            this.chatClient = chatClient;
        }

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
        
        public static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            text = text.Normalize(NormalizationForm.FormD);
            var chars = text.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray();
            return new string(chars).Normalize(NormalizationForm.FormC);
        }
    }
}