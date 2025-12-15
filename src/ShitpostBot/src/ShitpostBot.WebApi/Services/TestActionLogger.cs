using System.Collections.Concurrent;
using System.Diagnostics;

namespace ShitpostBot.WebApi.Services;

public class TestActionLogger : ITestActionLogger
{
    private readonly ConcurrentDictionary<ulong, List<TestAction>> _actions = new();
    private readonly ILogger<TestActionLogger> _logger;

    public TestActionLogger(ILogger<TestActionLogger> logger)
    {
        _logger = logger;
    }

    public Task LogActionAsync(ulong messageId, TestAction action)
    {
        _actions.AddOrUpdate(
            messageId,
            _ => new List<TestAction> { action },
            (_, existing) => 
            {
                lock (existing)
                {
                    existing.Add(action);
                }
                return existing;
            }
        );
        
        _logger.LogDebug("Logged action for message {MessageId}: {Type}", messageId, action.Type);
        return Task.CompletedTask;
    }

    public async Task<List<TestAction>> WaitForActionsAsync(
        ulong messageId, 
        int expectedCount, 
        TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();
        
        _logger.LogInformation("Waiting for {ExpectedCount} actions on message {MessageId} (timeout: {Timeout}ms)", 
            expectedCount, messageId, timeout.TotalMilliseconds);
        
        while (stopwatch.Elapsed < timeout)
        {
            if (_actions.TryGetValue(messageId, out var actions))
            {
                lock (actions)
                {
                    if (actions.Count >= expectedCount)
                    {
                        _logger.LogInformation("Found {Count} actions for message {MessageId} after {Elapsed}ms", 
                            actions.Count, messageId, stopwatch.ElapsedMilliseconds);
                        return actions.ToList();
                    }
                }
            }
            
            await Task.Delay(100); // Poll every 100ms
        }
        
        // Timeout - return whatever we have
        var final = _actions.TryGetValue(messageId, out var finalActions) 
            ? finalActions.ToList() 
            : new List<TestAction>();
        
        _logger.LogWarning("Timeout waiting for actions on message {MessageId}. Expected {Expected}, got {Actual}", 
            messageId, expectedCount, final.Count);
        
        return final;
    }
}