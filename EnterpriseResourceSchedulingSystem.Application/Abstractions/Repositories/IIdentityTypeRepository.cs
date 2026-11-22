using EnterpriseResourceSchedulingSystem.Application.Dtos;

namespace EnterpriseResourceSchedulingSystem.Application.Abstractions.Repositories;

public interface IIdentityTypeRepository
{
    Task<int> CountAsync(string? keyword, CancellationToken cancellationToken);

    Task<IReadOnlyList<IdentityTypeListItem>> GetPageAsync(
        string? keyword,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<IdentityTypeListItem?> FindByIdAsync(Guid identityTypeId, CancellationToken cancellationToken);

    Task<bool> CreateAsync(string identityName, string? description, string actor, CancellationToken cancellationToken);

    Task<IdentityTypeUpdateResult> UpdateAsync(
        Guid identityTypeId,
        string identityName,
        string? description,
        string actor,
        CancellationToken cancellationToken);

    Task<IdentityTypeDeleteResult> DeleteAsync(Guid identityTypeId, CancellationToken cancellationToken);
}
