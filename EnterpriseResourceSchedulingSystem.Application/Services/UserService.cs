using EnterpriseResourceSchedulingSystem.Application.Abstractions.Repositories;
using EnterpriseResourceSchedulingSystem.Application.Abstractions.Services;
using EnterpriseResourceSchedulingSystem.Application.Dtos;

namespace EnterpriseResourceSchedulingSystem.Application.Services;

public sealed class UserService(IUserRepository users) : IUserService
{
    public const int DefaultPageSize = 10;

    public async Task<UserPage> GetPageAsync(
        string? keyword,
        Guid? identityTypeId,
        int pageNumber,
        CancellationToken cancellationToken)
    {
        keyword = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();
        var totalCount = await users.CountAsync(keyword, identityTypeId, cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)DefaultPageSize));
        var actualPage = Math.Clamp(pageNumber, 1, totalPages);
        var items = totalCount == 0
            ? Array.Empty<UserListItem>()
            : await users.GetPageAsync(keyword, identityTypeId, actualPage, DefaultPageSize, cancellationToken);

        return new UserPage(items, actualPage, DefaultPageSize, totalCount, totalPages);
    }

    public Task<IReadOnlyList<IdentityTypeOption>> GetIdentityTypesAsync(CancellationToken cancellationToken)
        => users.GetIdentityTypesAsync(cancellationToken);

    public Task<UserListItem?> FindByIdAsync(Guid userId, CancellationToken cancellationToken)
        => users.FindByIdAsync(userId, cancellationToken);

    public async Task CreateAsync(string displayName, Guid identityTypeId, string actor, CancellationToken cancellationToken)
    {
        var normalizedName = NormalizeName(displayName);
        await EnsureIdentityTypeExistsAsync(identityTypeId, cancellationToken);
        await users.CreateAsync(normalizedName, identityTypeId, actor, cancellationToken);
    }

    public async Task<bool> UpdateAsync(Guid userId, string displayName, Guid identityTypeId, string actor, CancellationToken cancellationToken)
    {
        var normalizedName = NormalizeName(displayName);
        await EnsureIdentityTypeExistsAsync(identityTypeId, cancellationToken);
        return await users.UpdateAsync(userId, normalizedName, identityTypeId, actor, cancellationToken);
    }

    public Task<bool> DeleteAsync(Guid userId, CancellationToken cancellationToken)
        => users.DeleteAsync(userId, cancellationToken);

    private static string NormalizeName(string displayName)
    {
        var normalizedName = displayName.Trim();
        if (normalizedName.Length is 0 or > 100)
        {
            throw new ArgumentException("姓名不可空白，且不得超過 100 個字。", nameof(displayName));
        }

        return normalizedName;
    }

    private async Task EnsureIdentityTypeExistsAsync(Guid identityTypeId, CancellationToken cancellationToken)
    {
        var identityTypes = await users.GetIdentityTypesAsync(cancellationToken);
        if (!identityTypes.Any(identity => identity.IdentityTypeId == identityTypeId))
        {
            throw new ArgumentException("所選身分別不存在，請重新載入後再試。", nameof(identityTypeId));
        }
    }
}
