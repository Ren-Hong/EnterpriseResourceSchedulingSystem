using EnterpriseResourceSchedulingSystem.Application.Dtos;

namespace EnterpriseResourceSchedulingSystem.Application.Abstractions.Services;

public interface IAccountAuthenticationService
{
    Task<AuthenticatedAccount?> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken);
}
