using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.MessageRouting;

public static class MessagingExtensions
{
    public static MessageFeatureRegistration<T> AddMessageFeature<T>(
        this IServiceCollection services
    )
        where T : class, IMessageFeature
    {
        services.TryAddScoped<T>();
        services.AddScoped<IMessageFeature>(sp => sp.GetRequiredService<T>());
        return new MessageFeatureRegistration<T>(services);
    }

    public static MessageFeatureRegistration<T> WithAlias<T>(
        this MessageFeatureRegistration<T> registration,
        string alias,
        string canonical,
        bool forwardArguments = false
    )
        where T : BotCommandFeature
    {
        registration.Services.AddScoped<IMessageFeature>(sp => new CommandAlias(
            sp.GetRequiredService<IChatClient>(),
            sp.GetRequiredService<T>(),
            alias,
            canonical,
            forwardArguments
        ));
        return registration;
    }
}
