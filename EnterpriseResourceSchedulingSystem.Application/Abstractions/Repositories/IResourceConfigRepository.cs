using EnterpriseResourceSchedulingSystem.Application.Dtos;

namespace EnterpriseResourceSchedulingSystem.Application.Abstractions.Repositories;

public interface IResourceConfigRepository
{
    Task<IReadOnlyList<ResourceVersionListItem>> GetVersionsAsync(Guid resourceId, CancellationToken cancellationToken);
    Task CreateInitialVersionAsync(Guid resourceId, string name, string? description, int maxUsers, string actor, CancellationToken cancellationToken);
    Task<ResourceVersionCreateResult> CreateVersionAsync(Guid resourceId, string? description, int maxUsers, DateOnly effectiveStart, DateOnly effectiveEnd, string actor, CancellationToken cancellationToken);
}
