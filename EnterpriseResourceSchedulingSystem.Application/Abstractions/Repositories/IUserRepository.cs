using EnterpriseResourceSchedulingSystem.Application.Dtos;

namespace EnterpriseResourceSchedulingSystem.Application.Abstractions.Repositories;

public interface IUserRepository
{
    Task<IReadOnlyList<ScheduleUserOption>> GetAllForSchedulingAsync(CancellationToken cancellationToken);

    Task<int> CountAsync(string? keyword, Guid? identityTypeId, CancellationToken cancellationToken);

    Task<IReadOnlyList<UserListItem>> GetPageAsync(
        string? keyword,
        Guid? identityTypeId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<IdentityTypeOption>> GetIdentityTypesAsync(CancellationToken cancellationToken);

    Task<UserListItem?> FindByIdAsync(Guid userId, CancellationToken cancellationToken);

    Task CreateAsync(string displayName, Guid identityTypeId, string actor, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(Guid userId, string displayName, Guid identityTypeId, string actor, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid userId, CancellationToken cancellationToken);
}
