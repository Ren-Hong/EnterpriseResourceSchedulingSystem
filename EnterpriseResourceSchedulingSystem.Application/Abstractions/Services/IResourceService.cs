using EnterpriseResourceSchedulingSystem.Application.Dtos;

namespace EnterpriseResourceSchedulingSystem.Application.Abstractions.Services;

public interface IResourceService
{
    Task<ResourcePage> GetPageAsync(string? keyword, int pageNumber, CancellationToken cancellationToken);

    Task<ResourceListItem?> FindByIdAsync(Guid resourceId, CancellationToken cancellationToken);

    Task<ResourceSaveResult> CreateAsync(string name, string? description, int maxUsers, string actor, CancellationToken cancellationToken);

    Task<ResourceUpdateResult> UpdateAsync(Guid resourceId, string name, string? description, int maxUsers, string actor, CancellationToken cancellationToken);

    Task<ResourceDeleteResult> DeleteAsync(Guid resourceId, CancellationToken cancellationToken);
}
