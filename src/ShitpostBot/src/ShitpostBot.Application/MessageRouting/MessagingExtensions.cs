using Microsoft.Extensions.DependencyInjection;

namespace ShitpostBot.Application.MessageRouting;

public static class MessagingExtensions
{
    public static IServiceCollection AddMessageFeature<T>(this IServiceCollection services)
        where T : class, IMessageFeature
            => services.AddScoped<IMessageFeature, T>();
}
