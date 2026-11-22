using EnterpriseResourceSchedulingSystem.Application.Abstractions.Repositories;
using EnterpriseResourceSchedulingSystem.Application.Abstractions.Services;
using EnterpriseResourceSchedulingSystem.Application.Dtos;

namespace EnterpriseResourceSchedulingSystem.Application.Services;

public sealed class IdentityTypeService(IIdentityTypeRepository identityTypes) : IIdentityTypeService
{
    private const int PageSize = 10;

    public async Task<IdentityTypePage> GetPageAsync(string? keyword, int pageNumber, CancellationToken cancellationToken)
    {
        keyword = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();
        var totalCount = await identityTypes.CountAsync(keyword, cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
        var actualPage = Math.Clamp(pageNumber, 1, totalPages);
        var items = totalCount == 0
            ? Array.Empty<IdentityTypeListItem>()
            : await identityTypes.GetPageAsync(keyword, actualPage, PageSize, cancellationToken);

        return new IdentityTypePage(items, actualPage, PageSize, totalCount, totalPages);
    }

    public Task<IdentityTypeListItem?> FindByIdAsync(Guid identityTypeId, CancellationToken cancellationToken)
        => identityTypes.FindByIdAsync(identityTypeId, cancellationToken);

    public Task<bool> CreateAsync(string identityName, string? description, string actor, CancellationToken cancellationToken)
        => identityTypes.CreateAsync(NormalizeName(identityName), NormalizeDescription(description), NormalizeActor(actor), cancellationToken);

    public Task<IdentityTypeUpdateResult> UpdateAsync(
        Guid identityTypeId,
        string identityName,
        string? description,
        string actor,
        CancellationToken cancellationToken)
        => identityTypes.UpdateAsync(identityTypeId, NormalizeName(identityName), NormalizeDescription(description), NormalizeActor(actor), cancellationToken);

    public Task<IdentityTypeDeleteResult> DeleteAsync(Guid identityTypeId, CancellationToken cancellationToken)
        => identityTypes.DeleteAsync(identityTypeId, cancellationToken);

    private static string NormalizeName(string? identityName)
    {
        var value = identityName?.Trim() ?? string.Empty;
        if (value.Length is 0 or > 50 || value.Any(char.IsControl))
            throw new ArgumentException("身分別名稱不可空白、不得超過 50 字，也不可包含控制字元。", nameof(identityName));
        return value;
    }

    private static string? NormalizeDescription(string? description)
    {
        var value = description?.Trim();
        if (value?.Length > 200 || value?.Any(c => char.IsControl(c) && c is not ('\r' or '\n' or '\t')) == true)
            throw new ArgumentException("說明不得超過 200 字或包含不支援的控制字元。", nameof(description));
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static string NormalizeActor(string? actor)
    {
        var value = actor?.Trim();
        if (string.IsNullOrEmpty(value)) return "SYSTEM";
        return value.Length <= 50 ? value : value[..50];
    }
}
