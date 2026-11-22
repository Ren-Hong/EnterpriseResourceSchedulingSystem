using EnterpriseResourceSchedulingSystem.Application.Dtos;

namespace EnterpriseResourceSchedulingSystem.Application.Abstractions.Services;

public interface IIdentityTypeService
{
    Task<IdentityTypePage> GetPageAsync(string? keyword, int pageNumber, CancellationToken cancellationToken);

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
