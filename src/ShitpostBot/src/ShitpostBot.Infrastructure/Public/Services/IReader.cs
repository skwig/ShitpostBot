namespace ShitpostBot.Infrastructure.Services;

public interface IReader<TEntity> where TEntity : class
{
    public IQueryable<TEntity> All();
}