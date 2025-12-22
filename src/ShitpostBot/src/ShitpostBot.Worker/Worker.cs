using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Worker;

public class Worker(
    ILogger<Worker> logger,
    IChatClient chatClient,
    IEnumerable<IChatMessageCreatedListener> messageCreatedListeners,
    IEnumerable<IChatMessageDeletedListener> messageDeletedListeners) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Worker started at: {time}", DateTimeOffset.Now);
        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var listener in messageCreatedListeners)
            {
                chatClient.MessageCreated += listener.HandleMessageCreatedAsync;
            }

            foreach (var handler in messageDeletedListeners)
            {
                chatClient.MessageDeleted += handler.HandleMessageDeletedAsync;
            }

            await chatClient.ConnectAsync();

            await Task.Delay(-1, stoppingToken);
        }

        logger.LogInformation("Worker ended at: {time}", DateTimeOffset.Now);
    }
}