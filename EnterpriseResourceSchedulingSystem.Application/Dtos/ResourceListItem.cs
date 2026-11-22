namespace EnterpriseResourceSchedulingSystem.Application.Dtos;

public sealed record ResourceListItem(
    Guid ResourceId,
    string ResourceName,
    string? Description,
    int MaxUsers,
    string CreatedBy,
    DateTime CreatedAt);
