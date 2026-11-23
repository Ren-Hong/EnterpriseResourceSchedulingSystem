namespace EnterpriseResourceSchedulingSystem.Application.Dtos;

public sealed record ScheduleResourceOption(
    Guid ResourceId,
    string ResourceName,
    int MaxUsers);
