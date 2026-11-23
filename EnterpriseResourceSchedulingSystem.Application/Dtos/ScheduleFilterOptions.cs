namespace EnterpriseResourceSchedulingSystem.Application.Dtos;

public sealed record ScheduleFilterOptions(
    IReadOnlyList<ScheduleResourceOption> Resources,
    IReadOnlyList<ScheduleUserOption> Users);
