using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using System.Text.Json;
using System.Linq;
using System.Reflection;
using ShitpostBot.Application.Features.About;
using ShitpostBot.Application.Features.BotCommands;
using ShitpostBot.Application.Features.Help;
using ShitpostBot.Application.MessageRouting;

namespace ShitpostBot.Application;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddShitpostBotApplication(IConfiguration configuration)
        {
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

            services.AddAllImplementationsScoped<IBotCommandHandler>(typeof(DependencyInjection).Assembly);

            services.AddSingleton<MessageRouter>();
            services.AddMessageFeature<AboutFeature>();
            services.AddMessageFeature<HelpFeature>();

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