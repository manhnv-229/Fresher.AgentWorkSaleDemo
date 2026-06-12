using Demo.Application.UseCases.Common;

namespace Demo.Infrastructure.Persistence;

public sealed class UnitOfWork(DemoDbContext dbContext) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
