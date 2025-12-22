using System.Reflection;
using System.Runtime.CompilerServices;
using ShitpostBot.Application;
using ShitpostBot.Infrastructure;
using ShitpostBot.Application.Features.BotCommands;
using ShitpostBot.Infrastructure.Services;
using ShitpostBot.Worker.Core;

[assembly: InternalsVisibleTo("ShitpostBot.Tests.Unit")]

namespace ShitpostBot.Worker.Public;

public static class DependencyInjection
{
    extension(IServiceCollection serviceCollection)
    {
        public IServiceCollection AddShitpostBotWorker()
        {
            serviceCollection.AddSingleton<IChatMessageCreatedListener, ChatMessageCreatedListener>();
            serviceCollection.AddSingleton<IChatMessageDeletedListener, ChatMessageDeletedListener>();

            serviceCollection.AddAllImplementationsScoped<IBotCommandHandler>(typeof(DependencyInjection).Assembly);

            serviceCollection.AddHostedService<Worker>();

            return serviceCollection;
        }

        private void AddAllImplementationsScoped<TType>(Assembly assembly)
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
}