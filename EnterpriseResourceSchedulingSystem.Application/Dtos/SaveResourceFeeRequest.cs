namespace EnterpriseResourceSchedulingSystem.Application.Dtos;

public sealed record SaveResourceFeeRequest(
    Guid IdentityTypeId,
    decimal Fee,
    DateOnly EffectiveStart,
    DateOnly EffectiveEnd);
