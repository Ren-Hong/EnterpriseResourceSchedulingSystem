namespace EnterpriseResourceSchedulingSystem.Application.Dtos;

public sealed record ResourceVersionListItem(
    Guid ResourceConfigId,
    int ResourceConfigNo,
    string ResourceName,
    string? Description,
    int MaxUsers,
    DateTime EffectiveStart,
    DateTime? EffectiveEnd,
    string CreatedBy,
    DateTime CreatedAt);
