using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;

namespace ShitpostBot.WebApi.Services;

public class TestEventPublisher : ITestEventPublisher
{
    private readonly Channel<TestEvent> _channel;
    private readonly ILogger<TestEventPublisher> _logger;

    public TestEventPublisher(ILogger<TestEventPublisher> logger)
    {
        _logger = logger;
        _channel = Channel.CreateUnbounded<TestEvent>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });
    }

    public async Task PublishAsync<T>(string eventType, T data)
    {
        var dataJson = JsonSerializer.Serialize(data);
        var testEvent = new TestEvent(eventType, dataJson);
        
        _logger.LogDebug("Publishing event: {EventType}", eventType);
        
        await _channel.Writer.WriteAsync(testEvent);
    }

    public async IAsyncEnumerable<TestEvent> SubscribeAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.LogInformation("New SSE subscriber connected");
        
        try
        {
            await foreach (var testEvent in _channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return testEvent;
            }
        }
        finally
        {
            _logger.LogInformation("SSE subscriber disconnected");
        }
    }
}
