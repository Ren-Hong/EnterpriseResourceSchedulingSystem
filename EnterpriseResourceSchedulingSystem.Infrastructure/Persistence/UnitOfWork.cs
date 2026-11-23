using EnterpriseResourceSchedulingSystem.Application.Abstractions.Persistence;
using System.Data.Common;

namespace EnterpriseResourceSchedulingSystem.Infrastructure.Persistence;

public sealed class UnitOfWork(IDbConnectionFactory connectionFactory) : IUnitOfWork, IAsyncDisposable
{
    private readonly DbConnection _connection = connectionFactory.CreateErssConnection();
    private DbTransaction? _transaction;

    public DbConnection Connection => _connection;

    public DbTransaction? Transaction => _transaction;

    public async Task<DbConnection> GetOpenConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection.State != System.Data.ConnectionState.Open)
        {
            await _connection.OpenAsync(cancellationToken);
        }

        return _connection;
    }

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        // Nested service operations join the transaction owned by the outer operation.
        if (_transaction is not null)
        {
            return await operation(cancellationToken);
        }

        var connection = await GetOpenConnectionAsync(cancellationToken);
        _transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await operation(cancellationToken);
            await _transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            try
            {
                await _transaction.RollbackAsync(CancellationToken.None);
            }
            catch
            {
                // Preserve the original operation/commit exception.
            }

            throw;
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken)
        => ExecuteInTransactionAsync(async token =>
        {
            await operation(token);
            return true;
        }, cancellationToken);

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
        }

        await _connection.DisposeAsync();
    }
}
