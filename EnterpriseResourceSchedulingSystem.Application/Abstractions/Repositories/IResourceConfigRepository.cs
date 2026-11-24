using EnterpriseResourceSchedulingSystem.Application.Dtos;
using EnterpriseResourceSchedulingSystem.Domain.Resources;

namespace EnterpriseResourceSchedulingSystem.Application.Abstractions.Repositories;

/// <summary>讀寫設備與資源的設定版本。</summary>
public interface IResourceConfigRepository
{
    /// <summary>依設備識別碼取得設定版本清單，按版號由新到舊排列。</summary>
    Task<IReadOnlyList<ResourceVersionListItem>> GetVersionsAsync(Guid resourceId, CancellationToken cancellationToken);

    /// <summary>取得設備在指定日期適用的設定版本，供建立時段時選用。</summary>
    Task<IReadOnlyList<ScheduleVersion>> GetEffectiveVersionsAsync(Guid resourceId, DateOnly date, CancellationToken cancellationToken);

    /// <summary>依指定生效起訖日寫入新設備的第 1 版設定。</summary>
    Task CreateInitialVersionAsync(Guid resourceId, string? description, int maxUsers, ResourceEffectivePeriod period, string actor, CancellationToken cancellationToken);

    /// <summary>依指定起訖日新增設定版本，保留既有版本的有效期限。</summary>
    Task<ResourceVersionCreateResult> CreateVersionAsync(Guid resourceId, string? description, int maxUsers, ResourceEffectivePeriod period, string actor, CancellationToken cancellationToken);

    /// <summary>更新指定設備版本的設定資料。</summary>
    Task<bool> UpdateVersionAsync(Guid resourceId, Guid resourceConfigId, string? description, int maxUsers, ResourceEffectivePeriod period, string actor, CancellationToken cancellationToken);
}
