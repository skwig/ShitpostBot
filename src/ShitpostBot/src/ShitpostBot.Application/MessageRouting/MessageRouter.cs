using Microsoft.Extensions.DependencyInjection;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.MessageRouting;

public class MessageRouter(IServiceScopeFactory scopeFactory)
{
    public async Task RouteCreate(IncomingMessage msg, CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        var features = scope.ServiceProvider.GetRequiredService<IEnumerable<IMessageFeature>>();

        foreach (var feature in features)
        {
            if (await feature.TryHandleCreate(msg, cancellationToken))
            {
                return;
            }
        }
    }

    public async Task RouteUpdate(IncomingMessage old, IncomingMessage updated, CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        var features = scope.ServiceProvider.GetRequiredService<IEnumerable<IMessageFeature>>();

        foreach (var feature in features)
        {
            if (await feature.TryHandleUpdate(old, updated, cancellationToken))
            {
                return;
            }
        }
    }

    public async Task RouteDelete(MessageIdentification deleted, CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        var features = scope.ServiceProvider.GetRequiredService<IEnumerable<IMessageFeature>>();

        foreach (var feature in features)
        {
            if (await feature.TryHandleDelete(deleted, cancellationToken))
            {
                return;
            }
        }
    }
}