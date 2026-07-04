using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ShitpostBot.Application.MessageRouting;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Features.About;

public class AboutFeature(
    IChatClient chatClient,
    IOptions<RepostServiceOptions> repostServiceOptions,
    IHostEnvironment hostEnvironment)
    : BotCommandFeature
{
    private static readonly DateTimeOffset deployedOn = DateTimeOffset.UtcNow;

    public override string? HelpMessage => "`about` - prints information about the bot";

    protected override async Task<bool> TryHandleCommand(
        MessageIdentification commandMessageIdentification,
        string command,
        MessageIdentification? referenced,
        CancellationToken ct)
    {
        if (command != "about")
        {
            return false;
        }

        var destination = new MessageDestination(
            commandMessageIdentification.GuildId,
            commandMessageIdentification.ChannelId,
            commandMessageIdentification.MessageId
        );

        var utcNow = DateTimeOffset.UtcNow;
        var message = $"Uptime: {Math.Round((utcNow - deployedOn).TotalHours, 2)} hours\n" +
                      $"\n" +
                      $"I'm also open source {chatClient.Utils.Emoji(":bugman:")} https://github.com/skwig/ShitpostBot" +
                      $"\n" +
                      $"Config:\n" +
                      $"`{nameof(hostEnvironment.EnvironmentName)}: {hostEnvironment.EnvironmentName}`\n" +
                      $"`{nameof(repostServiceOptions.Value.RepostSimilarityThreshold)}: {repostServiceOptions.Value.RepostSimilarityThreshold}`\n";

        await chatClient.SendMessage(destination, message);
        return true;
    }
}
