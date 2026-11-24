using EnterpriseResourceSchedulingSystem.Application.Dtos;

namespace EnterpriseResourceSchedulingSystem.Application.Abstractions.Repositories;

/// <summary>讀寫人員與設備之間的使用授權。</summary>
public interface IUserResourceRepository
{
    /// <summary>確認指定人員識別碼是否存在。</summary>
    Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>取得人員已授權與可指派的設備清單。</summary>
    Task<UserResourcePermissionList> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>授予指定人員使用設備的權限，回傳指派結果。</summary>
    Task<AssignResourceResult> AssignAsync(Guid userId, Guid resourceId, string actor, CancellationToken cancellationToken);

    /// <summary>移除指定人員與設備之間的授權，回傳是否移除成功。</summary>
    Task<bool> RevokeAsync(Guid userId, Guid resourceId, CancellationToken cancellationToken);
}
