namespace EnterpriseResourceSchedulingSystem.Application.Dtos;

public sealed record ResourceFeeListItem(
    Guid ResourceFeeId,
    Guid ResourceConfigId,
    int ResourceConfigNo,
    Guid IdentityTypeId,
    string IdentityName,
    decimal Fee,
    DateTime ConfigEffectiveStart,
    DateTime ConfigEffectiveEnd,
    string CreatedBy,
    DateTime CreatedAt);
