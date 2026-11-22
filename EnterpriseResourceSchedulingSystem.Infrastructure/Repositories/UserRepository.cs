using Dapper;
using EnterpriseResourceSchedulingSystem.Application.Abstractions.Repositories;
using EnterpriseResourceSchedulingSystem.Application.Dtos;
using Microsoft.Data.SqlClient;

namespace EnterpriseResourceSchedulingSystem.Infrastructure.Repositories;

public sealed class UserRepository(string connectionString) : IUserRepository
{
    private const string FromAndFilter = """
        FROM dbo.ERSSUSERTBL AS u
        LEFT JOIN dbo.ERSSIDNTBL AS i ON i.UIDIDNTYPE = u.UIDIDNTYPE
        WHERE (@Keyword IS NULL OR u.NVCHDISPLAYNAME LIKE N'%' + @Keyword + N'%')
          AND (@IdentityTypeId IS NULL OR u.UIDIDNTYPE = @IdentityTypeId)
        """;

    public async Task<int> CountAsync(string? keyword, Guid? identityTypeId, CancellationToken cancellationToken)
    {
        var sql = "SELECT COUNT(*) " + FromAndFilter;
        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(sql, new { Keyword = keyword, IdentityTypeId = identityTypeId }, cancellationToken: cancellationToken);
        return await connection.ExecuteScalarAsync<int>(command);
    }

    public async Task<IReadOnlyList<UserListItem>> GetPageAsync(
        string? keyword,
        Guid? identityTypeId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        const string select = """
            SELECT u.UIDUSERID AS UserId, u.NVCHDISPLAYNAME AS DisplayName,
                   u.UIDIDNTYPE AS IdentityTypeId, i.NVCHIDENTITYNAME AS IdentityTypeName,
                   u.VCHCREATEBY AS CreatedBy, u.DTMCREATEDAT AS CreatedAt
            """;
        var sql = select + " " + FromAndFilter + " ORDER BY u.DTMCREATEDAT DESC, u.UIDUSERID "
            + "OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";
        var parameters = new
        {
            Keyword = keyword,
            IdentityTypeId = identityTypeId,
            Offset = (long)(pageNumber - 1) * pageSize,
            PageSize = pageSize
        };

        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        return (await connection.QueryAsync<UserListItem>(command)).AsList();
    }

    public async Task<IReadOnlyList<IdentityTypeOption>> GetIdentityTypesAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT UIDIDNTYPE AS IdentityTypeId, NVCHIDENTITYNAME AS IdentityName
            FROM dbo.ERSSIDNTBL
            ORDER BY NVCHIDENTITYNAME;
            """;

        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        return (await connection.QueryAsync<IdentityTypeOption>(command)).AsList();
    }

    public async Task<UserListItem?> FindByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT u.UIDUSERID AS UserId, u.NVCHDISPLAYNAME AS DisplayName,
                   u.UIDIDNTYPE AS IdentityTypeId, i.NVCHIDENTITYNAME AS IdentityTypeName,
                   u.VCHCREATEBY AS CreatedBy, u.DTMCREATEDAT AS CreatedAt
            FROM dbo.ERSSUSERTBL AS u
            LEFT JOIN dbo.ERSSIDNTBL AS i ON i.UIDIDNTYPE = u.UIDIDNTYPE
            WHERE u.UIDUSERID = @UserId;
            """;

        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<UserListItem>(command);
    }

    public async Task CreateAsync(string displayName, Guid identityTypeId, string actor, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.ERSSUSERTBL (UIDIDNTYPE, NVCHDISPLAYNAME, VCHCREATEBY)
            VALUES (@IdentityTypeId, @DisplayName, @Actor);
            """;

        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(sql, new { IdentityTypeId = identityTypeId, DisplayName = displayName, Actor = actor }, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }

    public async Task<bool> UpdateAsync(Guid userId, string displayName, Guid identityTypeId, string actor, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.ERSSUSERTBL
            SET UIDIDNTYPE = @IdentityTypeId,
                NVCHDISPLAYNAME = @DisplayName,
                VCHUPDATEBY = @Actor,
                DTMUPDATEDAT = SYSUTCDATETIME()
            WHERE UIDUSERID = @UserId;
            """;

        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(sql, new { UserId = userId, IdentityTypeId = identityTypeId, DisplayName = displayName, Actor = actor }, cancellationToken: cancellationToken);
        return await connection.ExecuteAsync(command) == 1;
    }

    public async Task<bool> DeleteAsync(Guid userId, CancellationToken cancellationToken)
    {
        const string sql = "DELETE FROM dbo.ERSSUSERTBL WHERE UIDUSERID = @UserId;";
        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken);

        try
        {
            return await connection.ExecuteAsync(command) == 1;
        }
        catch (SqlException exception) when (exception.Number == 547)
        {
            return false;
        }
    }
}
