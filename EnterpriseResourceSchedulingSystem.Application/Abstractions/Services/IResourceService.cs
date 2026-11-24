using EnterpriseResourceSchedulingSystem.Application.Dtos;

namespace EnterpriseResourceSchedulingSystem.Application.Abstractions.Services;

/// <summary>提供設備、資源及其設定版本的管理操作。</summary>
public interface IResourceService
{
    /// <summary>取得排程可選的設備與資源，並提供版本的人數上限。</summary>
    Task<IReadOnlyList<ScheduleResourceOption>> GetResourceOptionsAsync(CancellationToken cancellationToken);

    /// <summary>依名稱關鍵字分頁查詢設備與資源。</summary>
    Task<ResourcePage> GetPageAsync(string? keyword, int pageNumber, CancellationToken cancellationToken);

    /// <summary>依識別碼取得設備主檔及最高版號的設定，查無設備時回傳 null。</summary>
    Task<ResourceListItem?> FindByIdAsync(Guid resourceId, CancellationToken cancellationToken);

    /// <summary>依設備識別碼取得設定版本清單，按版號由新到舊排列。</summary>
    Task<IReadOnlyList<ResourceVersionListItem>> GetVersionsAsync(Guid resourceId, CancellationToken cancellationToken);

    /// <summary>建立設備主檔及初始設定版本，回傳建立結果。</summary>
    Task<ResourceSaveResult> CreateAsync(string name, string? description, int maxUsers, string actor, CancellationToken cancellationToken);

    /// <summary>更新設備主檔名稱，回傳更新結果。</summary>
    Task<ResourceUpdateResult> UpdateAsync(Guid resourceId, string name, string actor, CancellationToken cancellationToken);

    /// <summary>依指定起訖日新增設定版本，保留既有版本的有效期限。</summary>
    Task<ResourceVersionCreateResult> CreateVersionAsync(Guid resourceId, string? description, int maxUsers, DateOnly effectiveStart, DateOnly effectiveEnd, string actor, CancellationToken cancellationToken);

    /// <summary>刪除設備主檔，回傳刪除成功、不存在或仍被引用等結果。</summary>
    Task<ResourceDeleteResult> DeleteAsync(Guid resourceId, CancellationToken cancellationToken);
}

