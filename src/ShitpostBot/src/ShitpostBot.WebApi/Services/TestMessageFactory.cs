using ShitpostBot.Infrastructure;

namespace ShitpostBot.WebApi.Services;

public class TestMessageFactory
{
    private ulong _nextMessageId = 1000000;
    private readonly Random _random = new();

    public MessageIdentification GenerateMessageIdentification(
        ulong? guildId = null,
        ulong? channelId = null,
        ulong? userId = null,
        ulong? messageId = null)
    {
        return new MessageIdentification(
            guildId ?? GenerateId(),
            channelId ?? GenerateId(),
            userId ?? GenerateId(),
            messageId ?? Interlocked.Increment(ref _nextMessageId)
        );
    }

    public ImageMessage CreateImageMessage(
        string imageUrl,
        ulong? guildId = null,
        ulong? channelId = null,
        ulong? userId = null,
        ulong? messageId = null,
        DateTimeOffset? timestamp = null)
    {
        var identification = GenerateMessageIdentification(guildId, channelId, userId, messageId);
        var uri = new Uri(imageUrl);
        var attachmentId = GenerateId();
        var fileName = Path.GetFileName(uri.LocalPath);

        var attachment = new ImageMessageAttachment(attachmentId, fileName, uri, null);

        return new ImageMessage(
            identification,
            attachment,
            timestamp ?? DateTimeOffset.UtcNow
        );
    }

    public LinkMessage CreateLinkMessage(
        string linkUrl,
        ulong? guildId = null,
        ulong? channelId = null,
        ulong? userId = null,
        ulong? messageId = null,
        DateTimeOffset? timestamp = null)
    {
        var identification = GenerateMessageIdentification(guildId, channelId, userId, messageId);
        var uri = new Uri(linkUrl);

        var embed = new LinkMessageEmbed(uri);

        return new LinkMessage(
            identification,
            embed,
            timestamp ?? DateTimeOffset.UtcNow
        );
    }

    private ulong GenerateId()
    {
        return (ulong)_random.NextInt64(100000000, 999999999);
    }
}