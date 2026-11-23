using EnterpriseResourceSchedulingSystem.Application.Dtos;

namespace EnterpriseResourceSchedulingSystem.Application.Abstractions.Repositories;

public interface IResourceRepository
{
    Task<IReadOnlyList<ScheduleResourceOption>> GetResourceOptionsAsync(CancellationToken cancellationToken);
    Task<int> CountAsync(string? keyword, CancellationToken cancellationToken);
    Task<IReadOnlyList<ResourceListItem>> GetPageAsync(string? keyword, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<ResourceListItem?> FindByIdAsync(Guid resourceId, CancellationToken cancellationToken);
    Task<ResourceSaveResult> CreateAsync(Guid resourceId, string name, string? description, int maxUsers, string actor, CancellationToken cancellationToken);
    Task<ResourceUpdateResult> UpdateAsync(Guid resourceId, string name, string actor, CancellationToken cancellationToken);
    Task<ResourceDeleteResult> DeleteAsync(Guid resourceId, CancellationToken cancellationToken);
}

