using EnterpriseResourceSchedulingSystem.Application.Dtos;

namespace EnterpriseResourceSchedulingSystem.Application.Abstractions.Repositories;

/// <summary>讀寫各設備設定版本的身分別費率。</summary>
public interface IResourceFeeRepository
{
    /// <summary>查詢設備的身分別費率，可指定設備設定版本。</summary>
    Task<IReadOnlyList<ResourceFeeListItem>> GetByResourceAsync(Guid resourceId, Guid? resourceConfigId, CancellationToken cancellationToken);

    /// <summary>為設備設定版本新增指定身分別的費率。</summary>
    Task<ResourceFeeSaveResult> CreateAsync(Guid resourceId, Guid resourceConfigId, Guid identityTypeId, decimal fee, string actor, CancellationToken cancellationToken);

    /// <summary>修改指定版本的身分別費率，回傳更新結果。</summary>
    Task<ResourceFeeSaveResult> UpdateAsync(Guid resourceId, Guid resourceConfigId, Guid resourceFeeId, Guid identityTypeId, decimal fee, string actor, CancellationToken cancellationToken);

    /// <summary>刪除指定版本的一筆身分別費率，回傳刪除結果。</summary>
    Task<ResourceFeeDeleteResult> DeleteAsync(Guid resourceId, Guid resourceConfigId, Guid resourceFeeId, CancellationToken cancellationToken);
}
