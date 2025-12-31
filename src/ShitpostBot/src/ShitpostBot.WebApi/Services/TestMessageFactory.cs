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

    public ulong GenerateAttachmentId()
    {
        return GenerateId();
    }

    private ulong GenerateId()
    {
        return (ulong)_random.NextInt64(100000000, 999999999);
    }
}