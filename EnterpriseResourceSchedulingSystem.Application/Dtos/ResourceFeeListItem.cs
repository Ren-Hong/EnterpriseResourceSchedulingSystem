namespace EnterpriseResourceSchedulingSystem.Application.Dtos;

public sealed record ResourceFeeListItem(
    Guid ResourceFeeId,
    Guid ResourceConfigId,
    int ResourceConfigNo,
    Guid IdentityTypeId,
    string IdentityName,
    decimal Fee,
    DateTime EffectiveStart,
    DateTime EffectiveEnd,
    string CreatedBy,
    DateTime CreatedAt);
