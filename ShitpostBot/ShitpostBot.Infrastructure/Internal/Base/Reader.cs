using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace ShitpostBot.Infrastructure
{
    internal abstract class Reader<TEntity> : IReader<TEntity> where TEntity : class
    {
        protected IDbContextFactory<ShitpostBotDbContext> ContextFactory { get; }

        public Reader(IDbContextFactory<ShitpostBotDbContext> contextFactory)
        {
            ContextFactory = contextFactory;
        }

        public IQueryable<TEntity> All() => ContextFactory.CreateDbContext().Set<TEntity>().AsNoTracking().AsQueryable();
    }
}