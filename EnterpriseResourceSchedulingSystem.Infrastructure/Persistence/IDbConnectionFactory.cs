using System.Data.Common;

namespace EnterpriseResourceSchedulingSystem.Infrastructure.Persistence;

public interface IDbConnectionFactory
{
    DbConnection CreateErssConnection();
}
