using System.Data.Common;

namespace EnterpriseResourceSchedulingSystem.Application.Abstractions.Persistence;

/// <summary>
/// 在單一資料庫交易中執行完整的應用程式操作。
/// 操作呼叫的資料存取介面共用同一個資料庫連線與交易。
/// </summary>
public interface IUnitOfWork
{
    /// <summary>取得目前工作單元共用的資料庫連線。</summary>
    DbConnection Connection { get; }

    /// <summary>取得目前交易，未啟用交易時回傳 null。</summary>
    DbTransaction? Transaction { get; }

    /// <summary>確保共用連線已開啟並回傳該連線。</summary>
    Task<DbConnection> GetOpenConnectionAsync(CancellationToken cancellationToken);

    /// <summary>在交易中執行並回傳操作結果。若已有交易，則加入該交易。</summary>
    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken);

    /// <summary>在交易中執行不回傳值的操作。若已有交易，則加入該交易。</summary>
    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken);
}
