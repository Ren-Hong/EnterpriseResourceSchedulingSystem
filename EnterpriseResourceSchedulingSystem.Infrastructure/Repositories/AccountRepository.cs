using Dapper;
using EnterpriseResourceSchedulingSystem.Application.Abstractions.Persistence;
using EnterpriseResourceSchedulingSystem.Application.Abstractions.Repositories;
using EnterpriseResourceSchedulingSystem.Domain.Accounts;
using Microsoft.Data.SqlClient;

namespace EnterpriseResourceSchedulingSystem.Infrastructure.Repositories;

public sealed class AccountRepository(IUnitOfWork unitOfWork) : IAccountRepository
{
    public async Task<Account?> FindByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT a.UIDACCTID AS AccountId, a.NVCHUSERNAME AS Username,
                   a.VCHPASSWORDHASH AS PasswordHash, a.NVCHLEVEL AS [Level]
            FROM dbo.ERSSACCTBL AS a
            WHERE a.NVCHUSERNAME = @Username;
            """;
        var command = new CommandDefinition(sql, new { Username = username }, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        return await unitOfWork.Connection.QuerySingleOrDefaultAsync<Account>(command);
    }

    public async Task<AccountPresentation?> FindPresentationAsync(Guid accountId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT NVCHLEVEL AS [Level], NULLIF(TRIM(NVCHDISPLAYNAME), N'') AS DisplayName
            FROM dbo.ERSSACCTBL
            WHERE UIDACCTID = @AccountId;
            """;
        var command = new CommandDefinition(sql, new { AccountId = accountId }, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        return await unitOfWork.Connection.QuerySingleOrDefaultAsync<AccountPresentation>(command);
    }

    public async Task<bool> RecordSuccessfulLoginAsync(Account account, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.ERSSACCTBL
            SET DTMLASTLOGINAT = SYSUTCDATETIME()
            WHERE UIDACCTID = @AccountId
              AND VCHPASSWORDHASH = @PasswordHash;
            """;
        var parameters = new
        {
            account.AccountId,
            PasswordHash = new DbString { Value = account.PasswordHash, IsAnsi = true, Length = 255 }
        };
        var command = new CommandDefinition(sql, parameters, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        return await unitOfWork.Connection.ExecuteAsync(command) == 1;
    }
}
