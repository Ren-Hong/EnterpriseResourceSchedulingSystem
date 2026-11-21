using Dapper;
using EnterpriseResourceSchedulingSystem.Application.Abstractions.Repositories;
using EnterpriseResourceSchedulingSystem.Domain.Entities;
using Microsoft.Data.SqlClient;

namespace EnterpriseResourceSchedulingSystem.Infrastructure.Repositories;

public sealed class AccountRepository(string connectionString) : IAccountRepository
{
    public async Task<Account?> FindByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT a.UIDACCTID AS AccountId, a.NVCHUSERNAME AS Username,
                   a.VCHPASSWORDHASH AS PasswordHash, a.NVCHLEVEL AS [Level]
            FROM dbo.ERSSACCTBL AS a
            WHERE a.NVCHUSERNAME = @Username;
            """;

        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(sql, new { Username = username }, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Account>(command);
    }

    public async Task<string?> FindLevelAsync(Guid accountId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT NVCHLEVEL
            FROM dbo.ERSSACCTBL
            WHERE UIDACCTID = @AccountId;
            """;

        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(sql, new { AccountId = accountId }, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<string>(command);
    }

    public async Task<bool> RecordSuccessfulLoginAsync(Account account, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.ERSSACCTBL
            SET DTMLASTLOGINAT = SYSUTCDATETIME()
            WHERE UIDACCTID = @AccountId
              AND VCHPASSWORDHASH = @PasswordHash;
            """;

        await using var connection = new SqlConnection(connectionString);
        var parameters = new
        {
            account.AccountId,
            PasswordHash = new DbString { Value = account.PasswordHash, IsAnsi = true, Length = 255 }
        };
        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        return await connection.ExecuteAsync(command) == 1;
    }
}
