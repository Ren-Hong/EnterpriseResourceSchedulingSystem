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
            SELECT UIDUSERID AS UserId, NVCHUSERNAME AS Username, VCHPASSWORDHASH AS PasswordHash,
                   BITISACTIVE AS IsActive, NVCHLEVEL AS [Level]
            FROM dbo.ERSSUSERTBL
            WHERE NVCHUSERNAME = @Username;
            """;

        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(sql, new { Username = username }, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Account>(command);
    }

    public async Task<string?> FindActiveLevelAsync(Guid userId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT NVCHLEVEL
            FROM dbo.ERSSUSERTBL
            WHERE UIDUSERID = @UserId AND BITISACTIVE = 1;
            """;

        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<string>(command);
    }

    public async Task<bool> RecordSuccessfulLoginAsync(Account account, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.ERSSUSERTBL
            SET DTMLASTLOGINAT = SYSUTCDATETIME()
            WHERE UIDUSERID = @UserId AND BITISACTIVE = 1 AND VCHPASSWORDHASH = @PasswordHash;
            """;

        await using var connection = new SqlConnection(connectionString);
        var parameters = new
        {
            account.UserId,
            PasswordHash = new DbString { Value = account.PasswordHash, IsAnsi = true, Length = 255 }
        };
        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        return await connection.ExecuteAsync(command) == 1;
    }
}
