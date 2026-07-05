using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Worker;

public class Worker(
    ILogger<Worker> logger,
    IChatClient chatClient,
    IChatMessageCreatedListener created,
    IChatMessageUpdatedListener updated,
    IChatMessageDeletedListener deleted
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Worker started at: {time}", DateTimeOffset.Now);

        chatClient.MessageCreated += async args => await created.HandleMessageCreatedAsync(args);
        chatClient.MessageUpdated += async args => await updated.HandleMessageUpdatedAsync(args);
        chatClient.MessageDeleted += async args => await deleted.HandleMessageDeletedAsync(args);

        await chatClient.ConnectAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Worker ended at: {time}", DateTimeOffset.Now);
        return Task.CompletedTask;
    }
}
