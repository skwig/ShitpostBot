namespace ShitpostBot.WebApi.Services;

public interface ITestEventPublisher
{
    Task PublishAsync<T>(string eventType, T data);
    IAsyncEnumerable<TestEvent> SubscribeAsync(CancellationToken cancellationToken);
}

public record TestEvent(string Type, string DataJson);
