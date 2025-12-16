using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using System.Text.Json;
using ShitpostBot.Application.Services;
using System.Linq;
using System.Reflection;
using ShitpostBot.Application.Features.BotCommands;

namespace ShitpostBot.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddShitpostBotApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        
        services.AddOptions<ImageFeatureExtractorApiOptions>()
            .Bind(configuration.GetSection("ImageFeatureExtractorApi"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddRefitClient<IImageFeatureExtractorApi>()
            .ConfigureHttpClient((sp, client) =>
            {
                var options =
                    sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ImageFeatureExtractorApiOptions>>()
                        .Value;
                client.BaseAddress = new Uri(options.Uri);
            });
        
        services.AddAllImplementationsScoped<IBotCommandHandler>(typeof(DependencyInjection).Assembly);
        
        return services;
    }

    private static void AddAllImplementationsScoped<TType>(
        this IServiceCollection services, 
        Assembly assembly)
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
