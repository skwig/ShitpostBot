using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using System.Text.Json;
using System.Linq;
using System.Reflection;
using ShitpostBot.Application.Core;
using ShitpostBot.Application.Features.BotCommands;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddShitpostBotApplication(IConfiguration configuration)
        {
            services.AddSingleton<IChatMessageCreatedListener, ChatMessageCreatedListener>();
            services.AddSingleton<IChatMessageDeletedListener, ChatMessageDeletedListener>();
            services.AddSingleton<IChatMessageUpdatedListener, ChatMessageUpdatedListener>();

            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

            services.AddAllImplementationsScoped<IBotCommandHandler>(typeof(DependencyInjection).Assembly);

            return services;
        }

        private void AddAllImplementationsScoped<TType>(Assembly assembly)
        {
            var concretions = assembly
                .GetTypes()
                .Where(type => typeof(TType).IsAssignableFrom(type))
                .Where(type => !type.GetTypeInfo().IsAbstract && !type.GetTypeInfo().IsInterface)
                .ToList();

            foreach (var type in concretions)
            {
                services.AddScoped(typeof(TType), type);
            }
        }
    }
}