using System.Threading;
using System.Threading.Tasks;
using ShitpostBot.Domain;

namespace ShitpostBot.Infrastructure;

internal class UnitOfWork : IUnitOfWork
{
    private readonly ShitpostBotDbContext _context;

    public IImagePostsRepository ImagePostsRepository { get; }
    public ILinkPostsRepository LinkPostsRepository { get; }
    public IWhitelistedPostsRepository WhitelistedPostsRepository { get; }

    public UnitOfWork(
        ShitpostBotDbContext context,
        IImagePostsRepository imagePostsRepository,
        ILinkPostsRepository linkPostsRepository,
        IWhitelistedPostsRepository whitelistedPostsRepository)
    {
        _context = context;
        ImagePostsRepository = imagePostsRepository;
        LinkPostsRepository = linkPostsRepository;
        WhitelistedPostsRepository = whitelistedPostsRepository;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}