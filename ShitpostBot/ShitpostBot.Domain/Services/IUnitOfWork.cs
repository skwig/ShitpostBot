using System;
using System.Threading;
using System.Threading.Tasks;

namespace ShitpostBot.Domain;

public interface IUnitOfWork : IDisposable
{
    IImagePostsRepository ImagePostsRepository { get; }
    ILinkPostsRepository LinkPostsRepository { get; }
    IWhitelistedPostsRepository WhitelistedPostsRepository { get; }
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}