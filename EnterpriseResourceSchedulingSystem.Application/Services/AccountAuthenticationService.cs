using System.Text;
using EnterpriseResourceSchedulingSystem.Application.Abstractions.Repositories;
using EnterpriseResourceSchedulingSystem.Application.Abstractions.Security;
using EnterpriseResourceSchedulingSystem.Application.Abstractions.Services;
using EnterpriseResourceSchedulingSystem.Application.Dtos;

namespace EnterpriseResourceSchedulingSystem.Application.Services;

public sealed class AccountAuthenticationService(
    IAccountRepository accounts,
    IPasswordVerifier passwordVerifier) : IAccountAuthenticationService
{
    public async Task<AuthenticatedAccount?> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken)
    {
        var normalizedUsername = username.Trim();
        if (normalizedUsername.Length == 0 ||
            Encoding.UTF8.GetByteCount(password) > 72)
        {
            return null;
        }

        var account = await accounts.FindByUsernameAsync(normalizedUsername, cancellationToken);
        if (account is null || !passwordVerifier.Verify(password, account.PasswordHash))
        {
            return null;
        }

        if (!await accounts.RecordSuccessfulLoginAsync(account, cancellationToken))
        {
            return null;
        }

        return new AuthenticatedAccount(account.AccountId, account.Username, account.Level);
    }
}
