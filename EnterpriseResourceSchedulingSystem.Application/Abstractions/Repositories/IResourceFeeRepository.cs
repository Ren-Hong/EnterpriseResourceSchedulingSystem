using EnterpriseResourceSchedulingSystem.Application.Dtos;

namespace EnterpriseResourceSchedulingSystem.Application.Abstractions.Repositories;

public interface IResourceFeeRepository
{
    Task<IReadOnlyList<ResourceFeeListItem>> GetByResourceAsync(Guid resourceId, CancellationToken cancellationToken);

    Task<bool> HasOverlappingPeriodAsync(
        Guid resourceId,
        Guid identityTypeId,
        DateTime effectiveStart,
        DateTime effectiveEnd,
        Guid? excludeFeeId,
        CancellationToken cancellationToken);

    Task<ResourceFeeSaveResult> CreateAsync(Guid resourceId, Guid identityTypeId, decimal fee, DateTime effectiveStart, DateTime effectiveEnd, string actor, CancellationToken cancellationToken);

    Task<ResourceFeeSaveResult> UpdateAsync(Guid resourceId, Guid resourceFeeId, Guid identityTypeId, decimal fee, DateTime effectiveStart, DateTime effectiveEnd, string actor, CancellationToken cancellationToken);

    Task<ResourceFeeDeleteResult> DeleteAsync(Guid resourceId, Guid resourceFeeId, CancellationToken cancellationToken);
}
