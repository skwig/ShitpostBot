using System;
using System.Linq;

namespace ShitpostBot.Infrastructure
{
    public interface IReader<TEntity> where TEntity : class
    {
        public IQueryable<TEntity> All { get; }
    }
}