namespace EnterpriseResourceSchedulingSystem.Application.Dtos;

public sealed record SaveResourceRequest(
    string ResourceName,
    string? Description,
    int MaxUsers,
    DateOnly EffectiveStart = default,
    DateOnly EffectiveEnd = default);
