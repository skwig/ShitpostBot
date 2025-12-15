namespace ShitpostBot.WebApi.Services;

public interface ITestActionLogger
{
    Task LogActionAsync(ulong messageId, TestAction action);
    Task<List<TestAction>> WaitForActionsAsync(ulong messageId, int expectedCount, TimeSpan timeout);
}

public record TestAction(string Type, string Data, DateTimeOffset Timestamp);