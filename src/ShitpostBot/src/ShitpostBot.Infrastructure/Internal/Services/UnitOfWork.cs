using System.Threading;
using System.Threading.Tasks;
using ShitpostBot.Domain;

namespace ShitpostBot.Infrastructure;

internal class UnitOfWork : IUnitOfWork
{
    private readonly IDbContextFactory<ShitpostBotDbContext> _contextFactory;

    /// <summary>
    /// Shared context across all repositories
    /// </summary>
    private ShitpostBotDbContext _context;

    public IImagePostsRepository ImagePostsRepository { get; private set; }
    public ILinkPostsRepository LinkPostsRepository { get; private set; }
    public IWhitelistedPostsRepository WhitelistedPostsRepository { get; private set; }

    public UnitOfWork(IDbContextFactory<ShitpostBotDbContext> contextFactory)
    {
        this._contextFactory = contextFactory;

        RefreshContext();
    }

    private void RefreshContext()
    {
        _context = _contextFactory.CreateDbContext();

        ImagePostsRepository = new ImagePostsRepository(_context);
        LinkPostsRepository = new LinkPostsRepository(_context);
        WhitelistedPostsRepository = new WhitelistedPostsRepository(_context);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
        RefreshContext();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}