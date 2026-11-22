namespace EnterpriseResourceSchedulingSystem.Application.Dtos;

public sealed record ResourcePage(
    IReadOnlyList<ResourceListItem> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);
