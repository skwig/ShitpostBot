using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShitpostBot.Domain
{
    public interface IRepository<TEntity> where TEntity : class
    {
        Task CreateAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task CreateAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

        Task RemoveAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task RemoveAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    }
}