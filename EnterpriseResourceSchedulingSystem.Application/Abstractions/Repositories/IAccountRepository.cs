using EnterpriseResourceSchedulingSystem.Domain.Entities;

namespace EnterpriseResourceSchedulingSystem.Application.Abstractions.Repositories;

public interface IAccountRepository
{
    Task<Account?> FindByUsernameAsync(string username, CancellationToken cancellationToken);

    Task<string?> FindActiveLevelAsync(Guid userId, CancellationToken cancellationToken);

    Task<bool> RecordSuccessfulLoginAsync(Account account, CancellationToken cancellationToken);
}
