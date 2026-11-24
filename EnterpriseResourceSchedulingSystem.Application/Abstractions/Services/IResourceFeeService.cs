using EnterpriseResourceSchedulingSystem.Application.Dtos;

namespace EnterpriseResourceSchedulingSystem.Application.Abstractions.Services;

/// <summary>提供設備設定版本的身分別費率管理操作。</summary>
public interface IResourceFeeService
{
    /// <summary>查詢設備的身分別費率，可指定設定版本，查無設備時回傳 null。</summary>
    Task<IReadOnlyList<ResourceFeeListItem>?> GetByResourceAsync(Guid resourceId, Guid? resourceConfigId, CancellationToken cancellationToken);

    /// <summary>為設備設定版本新增指定身分別的費率。</summary>
    Task<ResourceFeeSaveResult> CreateAsync(Guid resourceId, Guid resourceConfigId, SaveResourceFeeRequest request, string actor, CancellationToken cancellationToken);

    /// <summary>修改指定版本的身分別費率，回傳更新結果。</summary>
    Task<ResourceFeeSaveResult> UpdateAsync(Guid resourceId, Guid resourceConfigId, Guid resourceFeeId, SaveResourceFeeRequest request, string actor, CancellationToken cancellationToken);

    /// <summary>刪除指定版本的一筆身分別費率，回傳刪除結果。</summary>
    Task<ResourceFeeDeleteResult> DeleteAsync(Guid resourceId, Guid resourceConfigId, Guid resourceFeeId, CancellationToken cancellationToken);
}
