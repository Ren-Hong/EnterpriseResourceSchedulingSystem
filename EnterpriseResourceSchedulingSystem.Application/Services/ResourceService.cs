using EnterpriseResourceSchedulingSystem.Application.Abstractions.Repositories;
using EnterpriseResourceSchedulingSystem.Application.Abstractions.Persistence;
using EnterpriseResourceSchedulingSystem.Application.Abstractions.Services;
using EnterpriseResourceSchedulingSystem.Application.Dtos;

namespace EnterpriseResourceSchedulingSystem.Application.Services;

public sealed class ResourceService(IResourceRepository resources, IUnitOfWork unitOfWork) : IResourceService
{
    private const int PageSize = 10;

    public async Task<ResourcePage> GetPageAsync(string? keyword, int pageNumber, CancellationToken cancellationToken)
    {
        keyword = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();
        var totalCount = await resources.CountAsync(keyword, cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
        var actualPage = Math.Clamp(pageNumber, 1, totalPages);
        var items = totalCount == 0
            ? Array.Empty<ResourceListItem>()
            : await resources.GetPageAsync(keyword, actualPage, PageSize, cancellationToken);
        return new ResourcePage(items, actualPage, PageSize, totalCount, totalPages);
    }

    public Task<ResourceListItem?> FindByIdAsync(Guid resourceId, CancellationToken cancellationToken)
        => resources.FindByIdAsync(resourceId, cancellationToken);

    public Task<ResourceSaveResult> CreateAsync(string name, string? description, int maxUsers, string actor, CancellationToken cancellationToken)
        => unitOfWork.ExecuteInTransactionAsync(ct => resources.CreateAsync(
            NormalizeName(name), NormalizeDescription(description), NormalizeMaxUsers(maxUsers), NormalizeActor(actor), ct), cancellationToken);

    public Task<ResourceUpdateResult> UpdateAsync(Guid resourceId, string name, string? description, int maxUsers, string actor, CancellationToken cancellationToken)
        => unitOfWork.ExecuteInTransactionAsync(ct => resources.UpdateAsync(
            resourceId, NormalizeName(name), NormalizeDescription(description), NormalizeMaxUsers(maxUsers), NormalizeActor(actor), ct), cancellationToken);

    public Task<ResourceDeleteResult> DeleteAsync(Guid resourceId, CancellationToken cancellationToken)
        => resources.DeleteAsync(resourceId, cancellationToken);

    private static string NormalizeName(string? value)
    {
        var name = value?.Trim() ?? string.Empty;
        if (name.Length is 0 or > 100 || name.Any(char.IsControl))
            throw new ArgumentException("設備／資源名稱不可空白、不得超過 100 字，也不可包含控制字元。", nameof(value));
        return name;
    }

    private static string? NormalizeDescription(string? value)
    {
        var description = value?.Trim();
        if (description?.Length > 500 || description?.Any(c => char.IsControl(c) && c is not ('\r' or '\n' or '\t')) == true)
            throw new ArgumentException("設備／資源說明不得超過 500 字或包含不支援的控制字元。", nameof(value));
        return string.IsNullOrEmpty(description) ? null : description;
    }

    private static int NormalizeMaxUsers(int value)
    {
        if (value <= 0) throw new ArgumentException("可使用人數上限必須大於 0。", nameof(value));
        return value;
    }

    private static string NormalizeActor(string? value)
    {
        var actor = value?.Trim();
        return string.IsNullOrEmpty(actor) ? "SYSTEM" : actor.Length <= 50 ? actor : actor[..50];
    }
}
