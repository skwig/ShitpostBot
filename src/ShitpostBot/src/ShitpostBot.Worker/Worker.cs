using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Worker;

public class Worker(
    ILogger<Worker> logger,
    IChatClient chatClient,
    IServiceScopeFactory scopeFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Worker started at: {time}", DateTimeOffset.Now);

        chatClient.MessageCreated += async args =>
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var listeners = scope.ServiceProvider.GetServices<IChatMessageCreatedListener>();
            foreach (var listener in listeners)
            {
                try
                {
                    await listener.HandleMessageCreatedAsync(args);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "MessageCreated listener failed");
                }
            }
        };

        chatClient.MessageDeleted += async args =>
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var listeners = scope.ServiceProvider.GetServices<IChatMessageDeletedListener>();
            foreach (var listener in listeners)
            {
                try
                {
                    await listener.HandleMessageDeletedAsync(args);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "MessageDeleted listener failed");
                }
            }
        };

        chatClient.MessageUpdated += async args =>
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var listeners = scope.ServiceProvider.GetServices<IChatMessageUpdatedListener>();
            foreach (var listener in listeners)
            {
                try
                {
                    await listener.HandleMessageUpdatedAsync(args);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "MessageUpdated listener failed");
                }
            }
        };

        await chatClient.ConnectAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Worker ended at: {time}", DateTimeOffset.Now);
        return Task.CompletedTask;
    }
}