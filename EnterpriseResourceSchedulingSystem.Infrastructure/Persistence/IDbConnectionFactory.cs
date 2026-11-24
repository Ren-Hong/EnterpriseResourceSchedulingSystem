using System.Data.Common;

namespace EnterpriseResourceSchedulingSystem.Infrastructure.Persistence;

/// <summary>集中建立 ERSS 資料庫連線。</summary>
public interface IDbConnectionFactory
{
    /// <summary>建立一個尚未開啟的 ERSS 資料庫連線。</summary>
    DbConnection CreateErssConnection();
}
