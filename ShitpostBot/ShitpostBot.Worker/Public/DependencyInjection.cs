using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;
using ShitpostBot.Infrastructure;
using ShitpostBot.Worker.Core;

[assembly: InternalsVisibleTo("ShitpostBot.Tests")]

namespace ShitpostBot.Worker.Public;

public static class DependencyInjection
{
    public static IServiceCollection AddShitpostBotWorker(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection.AddShitpostBotInfrastructure(configuration);

        serviceCollection.AddMediatR(serviceConfiguration =>
        {
            serviceConfiguration.RegisterServicesFromAssemblyContaining<Worker>();
        });
            
        serviceCollection.AddRefitClient<IImageFeatureExtractorApi>(new RefitSettings(new NewtonsoftJsonContentSerializer())).ConfigureHttpClient((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<ImageFeatureExtractorApiOptions>>();
            client.BaseAddress = new Uri(options.Value.Uri);
        });
            
        serviceCollection.AddScoped<IChatMessageCreatedListener, ChatMessageCreatedListener>();
        serviceCollection.AddScoped<IChatMessageDeletedListener, ChatMessageDeletedListener>();
            
        serviceCollection.AddAllImplementationsScoped<IBotCommandHandler>(typeof(DependencyInjection).Assembly);

        serviceCollection.Configure<ImageFeatureExtractorApiOptions>(configuration.GetSection("ImageFeatureExtractorApi"));
            
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