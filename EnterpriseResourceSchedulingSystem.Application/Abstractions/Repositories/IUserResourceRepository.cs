using EnterpriseResourceSchedulingSystem.Application.Dtos;

namespace EnterpriseResourceSchedulingSystem.Application.Abstractions.Repositories;

public interface IUserResourceRepository
{
    Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken);

    Task<UserResourcePermissionList> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken);

    Task<AssignResourceResult> AssignAsync(Guid userId, Guid resourceId, string actor, CancellationToken cancellationToken);

    Task<bool> RevokeAsync(Guid userId, Guid resourceId, CancellationToken cancellationToken);
}
