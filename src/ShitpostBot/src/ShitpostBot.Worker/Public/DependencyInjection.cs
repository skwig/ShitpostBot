namespace ShitpostBot.Worker.Public;

public static class DependencyInjection
{
    public static IServiceCollection AddShitpostBotWorker(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddHostedService<Worker>();

        return serviceCollection;
    }
}