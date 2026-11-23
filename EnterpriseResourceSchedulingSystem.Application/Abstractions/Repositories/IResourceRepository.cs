using EnterpriseResourceSchedulingSystem.Application.Dtos;

namespace EnterpriseResourceSchedulingSystem.Application.Abstractions.Repositories;

public interface IResourceRepository
{
    Task<IReadOnlyList<ScheduleResourceOption>> GetAllForSchedulingAsync(CancellationToken cancellationToken);
    Task<int> CountAsync(string? keyword, CancellationToken cancellationToken);
    Task<IReadOnlyList<ResourceListItem>> GetPageAsync(string? keyword, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<ResourceListItem?> FindByIdAsync(Guid resourceId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ResourceVersionListItem>> GetVersionsAsync(Guid resourceId, CancellationToken cancellationToken);
    Task<ResourceSaveResult> CreateAsync(string name, string? description, int maxUsers, string actor, CancellationToken cancellationToken);
    Task<ResourceUpdateResult> UpdateAsync(Guid resourceId, string name, string? description, int maxUsers, DateOnly effectiveStart, DateOnly? effectiveEnd, string actor, CancellationToken cancellationToken);
    Task<ResourceDeleteResult> DeleteAsync(Guid resourceId, CancellationToken cancellationToken);
}
