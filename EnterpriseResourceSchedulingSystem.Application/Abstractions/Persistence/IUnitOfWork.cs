using System.Data.Common;

namespace EnterpriseResourceSchedulingSystem.Application.Abstractions.Persistence;

/// <summary>
/// Executes a complete application use case in one database transaction.
/// Repositories called by the operation share the same scoped database session.
/// </summary>
public interface IUnitOfWork
{
    DbConnection Connection { get; }

    DbTransaction? Transaction { get; }

    Task<DbConnection> GetOpenConnectionAsync(CancellationToken cancellationToken);

    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken);

    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken);
}
