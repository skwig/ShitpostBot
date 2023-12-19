using System.Threading;
using System.Threading.Tasks;
using ShitpostBot.Domain;

namespace ShitpostBot.Infrastructure;

internal class UnitOfWork : IUnitOfWork
{
    private readonly IDbContextFactory<ShitpostBotDbContext> contextFactory;
        
    /// <summary>
    /// Shared context across all repositories
    /// </summary>
    private ShitpostBotDbContext context;

    public IImagePostsRepository ImagePostsRepository { get; private set; }
    public ILinkPostsRepository LinkPostsRepository { get; private set; }

    public UnitOfWork(IDbContextFactory<ShitpostBotDbContext> contextFactory)
    {
        this.contextFactory = contextFactory;

        RefreshContext();
    }

    private void RefreshContext()
    {
        context = contextFactory.CreateDbContext();

        ImagePostsRepository = new ImagePostsRepository(context);
        LinkPostsRepository = new LinkPostsRepository(context);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await context.SaveChangesAsync(cancellationToken);
        RefreshContext();
    }

    public void Dispose()
    {
        context.Dispose();
    }
}