using EnterpriseResourceSchedulingSystem.Application.Dtos;

namespace EnterpriseResourceSchedulingSystem.Application.Abstractions.Services;

public interface IResourceFeeService
{
    Task<IReadOnlyList<ResourceFeeListItem>?> GetByResourceAsync(Guid resourceId, CancellationToken cancellationToken);

    Task<ResourceFeeSaveResult> CreateAsync(Guid resourceId, SaveResourceFeeRequest request, string actor, CancellationToken cancellationToken);

    Task<ResourceFeeSaveResult> UpdateAsync(Guid resourceId, Guid resourceFeeId, SaveResourceFeeRequest request, string actor, CancellationToken cancellationToken);

    Task<ResourceFeeDeleteResult> DeleteAsync(Guid resourceId, Guid resourceFeeId, CancellationToken cancellationToken);
}
