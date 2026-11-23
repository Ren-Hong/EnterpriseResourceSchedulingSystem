using EnterpriseResourceSchedulingSystem.Application.Dtos;

namespace EnterpriseResourceSchedulingSystem.Application.Abstractions.Repositories;

public interface IResourceFeeRepository
{
    Task<IReadOnlyList<ResourceFeeListItem>> GetByResourceAsync(Guid resourceId, Guid? resourceConfigId, CancellationToken cancellationToken);
    Task<ResourceFeeSaveResult> CreateAsync(Guid resourceId, Guid resourceConfigId, Guid identityTypeId, decimal fee, string actor, CancellationToken cancellationToken);
    Task<ResourceFeeSaveResult> UpdateAsync(Guid resourceId, Guid resourceConfigId, Guid resourceFeeId, Guid identityTypeId, decimal fee, string actor, CancellationToken cancellationToken);
    Task<ResourceFeeDeleteResult> DeleteAsync(Guid resourceId, Guid resourceConfigId, Guid resourceFeeId, CancellationToken cancellationToken);
}
