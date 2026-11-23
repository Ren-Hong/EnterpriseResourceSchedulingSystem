using EnterpriseResourceSchedulingSystem.Application.Dtos;

namespace EnterpriseResourceSchedulingSystem.Application.Abstractions.Services;

public interface IResourceFeeService
{
    Task<IReadOnlyList<ResourceFeeListItem>?> GetByResourceAsync(Guid resourceId, Guid? resourceConfigId, CancellationToken cancellationToken);
    Task<ResourceFeeSaveResult> CreateAsync(Guid resourceId, Guid resourceConfigId, SaveResourceFeeRequest request, string actor, CancellationToken cancellationToken);
    Task<ResourceFeeSaveResult> UpdateAsync(Guid resourceId, Guid resourceConfigId, Guid resourceFeeId, SaveResourceFeeRequest request, string actor, CancellationToken cancellationToken);
    Task<ResourceFeeDeleteResult> DeleteAsync(Guid resourceId, Guid resourceConfigId, Guid resourceFeeId, CancellationToken cancellationToken);
}
