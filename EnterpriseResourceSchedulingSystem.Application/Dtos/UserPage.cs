namespace EnterpriseResourceSchedulingSystem.Application.Dtos;

public sealed record UserPage(
    IReadOnlyList<UserListItem> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);
