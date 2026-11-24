namespace EnterpriseResourceSchedulingSystem.Application.Dtos;

public sealed record UpdateResourceVersionRequest(
    string? Description,
    int MaxUsers,
    DateOnly EffectiveStart,
    DateOnly EffectiveEnd);
