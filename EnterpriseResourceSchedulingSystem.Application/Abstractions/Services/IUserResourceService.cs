using EnterpriseResourceSchedulingSystem.Application.Dtos;

namespace EnterpriseResourceSchedulingSystem.Application.Abstractions.Services;

public interface IUserResourceService
{
    Task<UserResourcePermissionList?> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken);

    Task<AssignResourceResult> AssignAsync(Guid userId, Guid resourceId, string actor, CancellationToken cancellationToken);

    Task<bool> RevokeAsync(Guid userId, Guid resourceId, CancellationToken cancellationToken);
}
