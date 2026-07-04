using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.Sus;

public class SusFeature(IChatClient chatClient) : IMessageFeature
{
    public async Task<bool> TryHandleCreate(IncomingMessage created, CancellationToken ct)
    {
        if (created.Content == null)
        {
            return false;
        }

        var whitespaceRemoved = Regex.Replace(created.Content, @"\s+", "");
        var unaccented = RemoveDiacritics(whitespaceRemoved);

        if (!unaccented.Contains("sus", StringComparison.InvariantCultureIgnoreCase))
        {
            return false;
        }

        await chatClient.React(created.Id, ":sus:");
        return true;
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