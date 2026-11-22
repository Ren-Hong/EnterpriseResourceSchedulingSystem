using EnterpriseResourceSchedulingSystem.Application.Abstractions.Repositories;
using EnterpriseResourceSchedulingSystem.Application.Abstractions.Services;
using EnterpriseResourceSchedulingSystem.Application.Dtos;

namespace EnterpriseResourceSchedulingSystem.Application.Services;

public sealed class UserResourceService(IUserResourceRepository userResources) : IUserResourceService
{
    public async Task<UserResourcePermissionList?> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (!await userResources.UserExistsAsync(userId, cancellationToken)) return null;
        return await userResources.GetPermissionsAsync(userId, cancellationToken);
    }

    public Task<AssignResourceResult> AssignAsync(Guid userId, Guid resourceId, string actor, CancellationToken cancellationToken)
    {
        if (resourceId == Guid.Empty) throw new ArgumentException("請選擇設備與資源。", nameof(resourceId));
        var normalizedActor = string.IsNullOrWhiteSpace(actor) ? "SYSTEM" : actor.Trim();
        return userResources.AssignAsync(userId, resourceId, normalizedActor, cancellationToken);
    }

    public Task<bool> RevokeAsync(Guid userId, Guid resourceId, CancellationToken cancellationToken)
        => userResources.RevokeAsync(userId, resourceId, cancellationToken);
}
