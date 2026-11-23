using EnterpriseResourceSchedulingSystem.Application.Abstractions.Persistence;
using EnterpriseResourceSchedulingSystem.Application.Abstractions.Repositories;
using EnterpriseResourceSchedulingSystem.Application.Abstractions.Services;
using EnterpriseResourceSchedulingSystem.Application.Dtos;

namespace EnterpriseResourceSchedulingSystem.Application.Services;

public sealed class ResourceFeeService(
    IResourceRepository resources,
    IResourceFeeRepository resourceFees,
    IUnitOfWork unitOfWork) : IResourceFeeService
{
    private const decimal MaximumFee = 9_999_999_999.99m;

    public async Task<IReadOnlyList<ResourceFeeListItem>?> GetByResourceAsync(Guid resourceId, Guid? resourceConfigId, CancellationToken cancellationToken)
    {
        if (await resources.FindByIdAsync(resourceId, cancellationToken) is null) return null;
        return await resourceFees.GetByResourceAsync(resourceId, resourceConfigId, cancellationToken);
    }

    public Task<ResourceFeeSaveResult> CreateAsync(Guid resourceId, Guid resourceConfigId, SaveResourceFeeRequest request, string actor, CancellationToken cancellationToken)
        => unitOfWork.ExecuteInTransactionAsync(ct => resourceFees.CreateAsync(
            resourceId, resourceConfigId, request.IdentityTypeId, ValidateFee(request.Fee), NormalizeActor(actor), ct), cancellationToken);

    public Task<ResourceFeeSaveResult> UpdateAsync(Guid resourceId, Guid resourceConfigId, Guid resourceFeeId, SaveResourceFeeRequest request, string actor, CancellationToken cancellationToken)
        => unitOfWork.ExecuteInTransactionAsync(ct => resourceFees.UpdateAsync(
            resourceId, resourceConfigId, resourceFeeId, request.IdentityTypeId, ValidateFee(request.Fee), NormalizeActor(actor), ct), cancellationToken);

    public Task<ResourceFeeDeleteResult> DeleteAsync(Guid resourceId, Guid resourceConfigId, Guid resourceFeeId, CancellationToken cancellationToken)
        => resourceFees.DeleteAsync(resourceId, resourceConfigId, resourceFeeId, cancellationToken);

    private static decimal ValidateFee(decimal value)
    {
        if (value is < 0 or > MaximumFee || decimal.Round(value, 2) != value)
            throw new ArgumentException("費率須為 0 至 9,999,999,999.99，最多兩位小數。", nameof(value));
        return value;
    }

    private static string NormalizeActor(string? value)
    {
        var actor = value?.Trim();
        return string.IsNullOrEmpty(actor) ? "SYSTEM" : actor.Length <= 50 ? actor : actor[..50];
    }
}
