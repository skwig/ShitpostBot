using Microsoft.Extensions.DependencyInjection;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.MessageRouting;

public class MessageRouter(IServiceScopeFactory scopeFactory)
{
    public async Task RouteCreate(IncomingMessage msg)
    {
        using var scope = scopeFactory.CreateScope();
        var features = scope.ServiceProvider.GetRequiredService<IEnumerable<IMessageFeature>>();

        foreach (var feature in features)
        {
            if (await feature.TryHandleCreate(msg, default))
            {
                return;
            }
        }
    }

    public async Task RouteUpdate(IncomingMessage old, IncomingMessage updated)
    {
        using var scope = scopeFactory.CreateScope();
        var features = scope.ServiceProvider.GetRequiredService<IEnumerable<IMessageFeature>>();

        foreach (var feature in features)
        {
            if (await feature.TryHandleUpdate(old, updated, default))
            {
                return;
            }
        }
    }

    public async Task RouteDelete(MessageIdentification deleted)
    {
        using var scope = scopeFactory.CreateScope();
        var features = scope.ServiceProvider.GetRequiredService<IEnumerable<IMessageFeature>>();

        foreach (var feature in features)
        {
            if (await feature.TryHandleDelete(deleted, default))
            {
                return;
            }
        }
    }
}