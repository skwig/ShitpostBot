using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ShitpostBot.Domain;

namespace ShitpostBot.Infrastructure;

internal class Repository<TEntity>(ShitpostBotDbContext context) : IRepository<TEntity>
    where TEntity : class
{
    protected ShitpostBotDbContext Context { get; } = context;

    public async Task CreateAsync(TEntity entity, CancellationToken cancellationToken)
    {
        await Context.AddAsync(entity, cancellationToken);
    }

    public async Task CreateAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken)
    {
        await Context.AddRangeAsync(entities, cancellationToken).ConfigureAwait(false);
    }

    public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        Context.Update(entity);
        
        return Task.CompletedTask;
    }
        
    public Task UpdateAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        Context.Update(entities);
        
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