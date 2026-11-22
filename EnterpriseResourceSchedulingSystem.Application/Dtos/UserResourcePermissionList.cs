namespace EnterpriseResourceSchedulingSystem.Application.Dtos;

public sealed record UserResourcePermissionList(
    IReadOnlyList<UserResourcePermissionItem> AssignedResources,
    IReadOnlyList<ResourceOption> AvailableResources);
