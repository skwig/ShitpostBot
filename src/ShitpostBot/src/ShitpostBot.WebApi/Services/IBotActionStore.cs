namespace ShitpostBot.WebApi.Services;

public interface IBotActionStore
{
    Task StoreActionAsync(ulong messageId, TestAction action);
    Task<IReadOnlyList<TestAction>> WaitForActionsAsync(ulong messageId, int expectedCount, TimeSpan timeout);
}

public record TestAction(string Type, string Data, DateTimeOffset Timestamp);