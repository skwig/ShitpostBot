using System.Runtime.CompilerServices;
using ShitpostBot.Infrastructure.Services;
using ShitpostBot.Worker.Core;

[assembly: InternalsVisibleTo("ShitpostBot.Tests.Unit")]

namespace ShitpostBot.Worker.Public;

public static class DependencyInjection
{
    public static IServiceCollection AddShitpostBotWorker(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IChatMessageCreatedListener, ChatMessageCreatedListener>();
        serviceCollection.AddSingleton<IChatMessageDeletedListener, ChatMessageDeletedListener>();
        serviceCollection.AddSingleton<IChatMessageUpdatedListener, ChatMessageUpdatedListener>();

        serviceCollection.AddHostedService<Worker>();

        return serviceCollection;
    }
}
