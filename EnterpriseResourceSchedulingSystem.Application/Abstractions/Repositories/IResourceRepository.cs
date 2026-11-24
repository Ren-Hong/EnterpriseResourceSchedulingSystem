using EnterpriseResourceSchedulingSystem.Application.Dtos;

namespace EnterpriseResourceSchedulingSystem.Application.Abstractions.Repositories;

/// <summary>讀寫設備與資源主檔，並查詢清單所需的設定資料。</summary>
public interface IResourceRepository
{
    /// <summary>取得排程可選的設備與資源，並提供版本的人數上限。</summary>
    Task<IReadOnlyList<ScheduleResourceOption>> GetResourceOptionsAsync(CancellationToken cancellationToken);

    /// <summary>計算符合名稱關鍵字的設備與資源總數。</summary>
    Task<int> CountAsync(string? keyword, CancellationToken cancellationToken);

    /// <summary>依名稱關鍵字分頁查詢設備與資源。</summary>
    Task<IReadOnlyList<ResourceListItem>> GetPageAsync(string? keyword, int pageNumber, int pageSize, CancellationToken cancellationToken);

    /// <summary>依識別碼取得設備主檔及最高版號的設定，查無設備時回傳 null。</summary>
    Task<ResourceListItem?> FindByIdAsync(Guid resourceId, CancellationToken cancellationToken);

    /// <summary>以指定識別碼寫入設備主檔，回傳建立結果。</summary>
    Task<ResourceSaveResult> CreateAsync(Guid resourceId, string name, string? description, int maxUsers, string actor, CancellationToken cancellationToken);

    /// <summary>更新設備主檔名稱，回傳更新結果。</summary>
    Task<ResourceUpdateResult> UpdateAsync(Guid resourceId, string name, string actor, CancellationToken cancellationToken);

    /// <summary>刪除設備主檔，回傳刪除成功、不存在或仍被引用等結果。</summary>
    Task<ResourceDeleteResult> DeleteAsync(Guid resourceId, CancellationToken cancellationToken);
}

