using EnterpriseResourceSchedulingSystem.Application.Dtos;

namespace EnterpriseResourceSchedulingSystem.Application.Abstractions.Services;

/// <summary>提供人員設備使用權限的管理操作。</summary>
public interface IUserResourceService
{
    /// <summary>取得人員已授權與可指派的設備清單，查無人員時回傳 null。</summary>
    Task<UserResourcePermissionList?> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>授予指定人員使用設備的權限，回傳指派結果。</summary>
    Task<AssignResourceResult> AssignAsync(Guid userId, Guid resourceId, string actor, CancellationToken cancellationToken);

    /// <summary>移除指定人員與設備之間的授權，回傳是否移除成功。</summary>
    Task<bool> RevokeAsync(Guid userId, Guid resourceId, CancellationToken cancellationToken);
}
