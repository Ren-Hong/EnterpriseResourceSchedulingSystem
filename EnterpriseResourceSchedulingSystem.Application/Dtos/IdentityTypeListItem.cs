namespace EnterpriseResourceSchedulingSystem.Application.Dtos;

public sealed record IdentityTypeListItem(
    Guid IdentityTypeId,
    string IdentityName,
    string? Description,
    string CreatedBy,
    DateTime CreatedAt);
