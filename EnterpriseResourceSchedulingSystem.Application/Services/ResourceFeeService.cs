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

    public async Task<IReadOnlyList<ResourceFeeListItem>?> GetByResourceAsync(Guid resourceId, CancellationToken cancellationToken)
    {
        if (await resources.FindByIdAsync(resourceId, cancellationToken) is null) return null;
        return await resourceFees.GetByResourceAsync(resourceId, cancellationToken);
    }

    public Task<ResourceFeeSaveResult> CreateAsync(Guid resourceId, SaveResourceFeeRequest request, string actor, CancellationToken cancellationToken)
    {
        var fee = ValidateFee(request.Fee);
        var start = Date(request.EffectiveStart, "生效開始日");
        var end = Date(request.EffectiveEnd, "生效結束日");
        ValidatePeriod(start, end);
        var createdBy = NormalizeActor(actor);

        return unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            if (await resourceFees.HasOverlappingPeriodAsync(resourceId, request.IdentityTypeId, start, end, null, ct))
                return ResourceFeeSaveResult.OverlappingPeriod;
            return await resourceFees.CreateAsync(resourceId, request.IdentityTypeId, fee, start, end, createdBy, ct);
        }, cancellationToken);
    }

    public Task<ResourceFeeSaveResult> UpdateAsync(Guid resourceId, Guid resourceFeeId, SaveResourceFeeRequest request, string actor, CancellationToken cancellationToken)
    {
        var fee = ValidateFee(request.Fee);
        var start = Date(request.EffectiveStart, "生效開始日");
        var end = Date(request.EffectiveEnd, "生效結束日");
        ValidatePeriod(start, end);
        var updatedBy = NormalizeActor(actor);

        return unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            if (await resourceFees.HasOverlappingPeriodAsync(resourceId, request.IdentityTypeId, start, end, resourceFeeId, ct))
                return ResourceFeeSaveResult.OverlappingPeriod;
            return await resourceFees.UpdateAsync(resourceId, resourceFeeId, request.IdentityTypeId, fee, start, end, updatedBy, ct);
        }, cancellationToken);
    }

    public Task<ResourceFeeDeleteResult> DeleteAsync(Guid resourceId, Guid resourceFeeId, CancellationToken cancellationToken)
        => resourceFees.DeleteAsync(resourceId, resourceFeeId, cancellationToken);

    private static decimal ValidateFee(decimal value)
    {
        if (value is < 0 or > MaximumFee || decimal.Round(value, 2) != value)
            throw new ArgumentException("費率須為 0 至 9,999,999,999.99，最多兩位小數。", nameof(value));
        return value;
    }

    private static DateTime Date(DateOnly value, string label)
    {
        if (value == default)
            throw new ArgumentException($"{label}不正確。", nameof(value));
        return value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
    }

    private static void ValidatePeriod(DateTime start, DateTime end)
    {
        if (end < start)
            throw new ArgumentException("生效結束日不可早於生效開始日。", nameof(end));
    }

    private static string NormalizeActor(string? value)
    {
        var actor = value?.Trim();
        return string.IsNullOrEmpty(actor) ? "SYSTEM" : actor.Length <= 50 ? actor : actor[..50];
    }
}
