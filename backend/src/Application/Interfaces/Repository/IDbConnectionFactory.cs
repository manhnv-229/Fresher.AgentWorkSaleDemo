using System.Data.Common;

namespace Demo.Application.Interfaces.Repository;

public interface IDbConnectionFactory
{
    Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken);
}
