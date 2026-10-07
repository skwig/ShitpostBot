using Microsoft.Extensions.DependencyInjection;

namespace ShitpostBot.Application.MessageRouting;

public sealed class MessageFeatureRegistration<T>
    where T : class, IMessageFeature
{
    internal MessageFeatureRegistration(IServiceCollection services)
    {
        Services = services;
    }

    internal IServiceCollection Services { get; }
}
