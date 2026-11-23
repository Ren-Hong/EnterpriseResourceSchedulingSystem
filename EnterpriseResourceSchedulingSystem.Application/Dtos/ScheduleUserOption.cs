namespace EnterpriseResourceSchedulingSystem.Application.Dtos;

public sealed record ScheduleUserOption(
    Guid UserId,
    string DisplayName,
    Guid? IdentityTypeId,
    string? IdentityTypeName);
