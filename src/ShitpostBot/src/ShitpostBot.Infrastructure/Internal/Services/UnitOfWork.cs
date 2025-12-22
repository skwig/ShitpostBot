using ShitpostBot.Domain;

namespace ShitpostBot.Infrastructure;

internal class UnitOfWork(ShitpostBotDbContext context) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}