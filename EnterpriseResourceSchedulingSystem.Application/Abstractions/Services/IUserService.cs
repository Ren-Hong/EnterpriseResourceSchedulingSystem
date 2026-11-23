using EnterpriseResourceSchedulingSystem.Application.Dtos;

namespace EnterpriseResourceSchedulingSystem.Application.Abstractions.Services;

public interface IUserService
{
    Task<IReadOnlyList<ScheduleUserOption>> GetAllForSchedulingAsync(CancellationToken cancellationToken);

    Task<UserPage> GetPageAsync(
        string? keyword,
        Guid? identityTypeId,
        int pageNumber,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<IdentityTypeOption>> GetIdentityTypesAsync(CancellationToken cancellationToken);

    Task<UserListItem?> FindByIdAsync(Guid userId, CancellationToken cancellationToken);

    Task CreateAsync(string displayName, Guid identityTypeId, string actor, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(Guid userId, string displayName, Guid identityTypeId, string actor, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid userId, CancellationToken cancellationToken);
}
