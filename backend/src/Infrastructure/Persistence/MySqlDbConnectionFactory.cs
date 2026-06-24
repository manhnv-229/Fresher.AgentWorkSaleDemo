using System.Data.Common;

using Demo.Application.Interfaces.Repository;

using MySqlConnector;

namespace Demo.Infrastructure.Persistence;

public sealed class MySqlDbConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public async Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
