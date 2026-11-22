namespace EnterpriseResourceSchedulingSystem.Application.Dtos;

public sealed record IdentityTypePage(
    IReadOnlyList<IdentityTypeListItem> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);
