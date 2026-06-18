using Demo.Domain.Interfaces.Repository;

namespace Demo.Infrastructure.Persistence;

public sealed class UnitOfWork(DemoDbContext dbContext) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
