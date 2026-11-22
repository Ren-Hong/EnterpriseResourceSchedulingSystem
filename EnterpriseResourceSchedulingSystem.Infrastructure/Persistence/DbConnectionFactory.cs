using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace EnterpriseResourceSchedulingSystem.Infrastructure.Persistence;

public sealed class DbConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public DbConnection CreateErssConnection()
        => new SqlConnection(connectionString);
}
