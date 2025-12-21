using Microsoft.EntityFrameworkCore;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Infrastructure;

internal abstract class Reader<TEntity>(IDbContextFactory<ShitpostBotDbContext> contextFactory) : IReader<TEntity>
    where TEntity : class
{
    protected IDbContextFactory<ShitpostBotDbContext> ContextFactory { get; } = contextFactory;

    public IQueryable<TEntity> All() => ContextFactory.CreateDbContext().Set<TEntity>().AsNoTracking().AsQueryable();
}