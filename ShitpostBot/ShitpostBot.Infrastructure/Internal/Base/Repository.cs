using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ShitpostBot.Domain;

namespace ShitpostBot.Infrastructure;

internal class Repository<TEntity>(ShitpostBotDbContext context) : IRepository<TEntity>
    where TEntity : class
{
    protected ShitpostBotDbContext Context { get; } = context;

    public Task CreateAsync(TEntity entity, CancellationToken cancellationToken)
    {
        Context.Attach(entity);

        return Task.CompletedTask;
    }

    public Task CreateAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken)
    {
        Context.AttachRange(entities);
        
        return Task.CompletedTask;
    }
    
    public Task RemoveAsync(TEntity entity, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            Context.Remove(entity);
        }, cancellationToken);
    }

    public Task RemoveAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            Context.RemoveRange(entities);
        }, cancellationToken);
    }
}