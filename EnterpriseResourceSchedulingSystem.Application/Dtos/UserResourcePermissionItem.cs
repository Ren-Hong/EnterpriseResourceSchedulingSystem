namespace EnterpriseResourceSchedulingSystem.Application.Dtos;

public sealed record UserResourcePermissionItem(
    Guid ResourceId,
    string ResourceName,
    string CreatedBy,
    DateTime CreatedAt);
