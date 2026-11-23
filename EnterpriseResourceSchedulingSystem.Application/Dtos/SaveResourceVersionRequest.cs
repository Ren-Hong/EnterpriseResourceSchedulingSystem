namespace EnterpriseResourceSchedulingSystem.Application.Dtos;

public sealed record SaveResourceVersionRequest(
    string? Description,
    int MaxUsers,
    DateOnly EffectiveStart,
    DateOnly EffectiveEnd);
