using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Worker;

public class Worker(ILogger<Worker> logger, IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Worker started at: {time}", DateTimeOffset.Now);
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = serviceScopeFactory.CreateScope();
            var chatClient = scope.ServiceProvider.GetRequiredService<IChatClient>();
                
            var messageCreatedListeners = scope.ServiceProvider.GetServices<IChatMessageCreatedListener>();
            foreach (var listener in messageCreatedListeners)
            {
                chatClient.MessageCreated += listener.HandleMessageCreatedAsync;
            }
                
            var messageDeletedListeners = scope.ServiceProvider.GetServices<IChatMessageDeletedListener>();
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