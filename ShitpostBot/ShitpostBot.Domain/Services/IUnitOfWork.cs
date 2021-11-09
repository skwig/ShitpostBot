using System;
using System.Threading;
using System.Threading.Tasks;

namespace ShitpostBot.Domain
{
    public interface IUnitOfWork : IDisposable
    {
        IImagePostsRepository ImagePostsRepository { get; }
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}