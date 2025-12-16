using System.Reflection;
using System.Runtime.CompilerServices;
using ShitpostBot.Application;
using ShitpostBot.Infrastructure;
using ShitpostBot.Application.Features.BotCommands;
using ShitpostBot.Worker.Core;

[assembly: InternalsVisibleTo("ShitpostBot.Tests.Unit")]

namespace ShitpostBot.Worker.Public;

public static class DependencyInjection
{
    public static IServiceCollection AddShitpostBotWorker(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection.AddShitpostBotApplication(configuration);
        serviceCollection.AddShitpostBotInfrastructure(configuration);

        serviceCollection.AddScoped<IChatMessageCreatedListener, ChatMessageCreatedListener>();
        serviceCollection.AddScoped<IChatMessageDeletedListener, ChatMessageDeletedListener>();

        serviceCollection.AddAllImplementationsScoped<IBotCommandHandler>(typeof(DependencyInjection).Assembly);

        return serviceCollection;
    }

    private static void AddAllImplementationsScoped<TType>(this IServiceCollection serviceCollection, Assembly assembly)
    {
        var concretions = assembly
            .GetTypes()
            .Where(type => typeof(IBotCommandHandler).IsAssignableFrom(type))
            .Where(type => !type.GetTypeInfo().IsAbstract && !type.GetTypeInfo().IsInterface)
            .ToList();

        foreach (var type in concretions)
        {
            serviceCollection.AddScoped(typeof(TType), type);
        }
    }
}