namespace EnterpriseResourceSchedulingSystem.Application.Dtos;

public sealed record UserListItem(
    Guid UserId,
    string? DisplayName,
    Guid? IdentityTypeId,
    string? IdentityTypeName,
    string CreatedBy,
    DateTime CreatedAt);
