using Dapper;
using EnterpriseResourceSchedulingSystem.Application.Abstractions.Persistence;
using EnterpriseResourceSchedulingSystem.Application.Abstractions.Repositories;
using EnterpriseResourceSchedulingSystem.Application.Dtos;
using Microsoft.Data.SqlClient;

namespace EnterpriseResourceSchedulingSystem.Infrastructure.Repositories;

public sealed class UserRepository(IUnitOfWork unitOfWork) : IUserRepository
{
    private const string FromAndFilter = """
        FROM dbo.ERSSUSERTBL AS u
        LEFT JOIN dbo.ERSSIDNTBL AS i ON i.UIDIDNTYPE = u.UIDIDNTYPE
        WHERE (@Keyword IS NULL OR u.NVCHDISPLAYNAME LIKE N'%' + @Keyword + N'%')
          AND (@IdentityTypeId IS NULL OR u.UIDIDNTYPE = @IdentityTypeId)
        """;

    public async Task<IReadOnlyList<ScheduleUserOption>> GetAllForSchedulingAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT u.UIDUSERID AS UserId,
                   COALESCE(NULLIF(LTRIM(RTRIM(u.NVCHDISPLAYNAME)), N''), N'未設定姓名') AS DisplayName,
                   u.UIDIDNTYPE AS IdentityTypeId,
                   i.NVCHIDENTITYNAME AS IdentityTypeName
            FROM dbo.ERSSUSERTBL AS u
            LEFT JOIN dbo.ERSSIDNTBL AS i ON i.UIDIDNTYPE = u.UIDIDNTYPE
            ORDER BY COALESCE(NULLIF(LTRIM(RTRIM(u.NVCHDISPLAYNAME)), N''), N'未設定姓名'), u.UIDUSERID;
            """;
        var command = new CommandDefinition(sql, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        return (await unitOfWork.Connection.QueryAsync<ScheduleUserOption>(command)).AsList();
    }

    public async Task<int> CountAsync(string? keyword, Guid? identityTypeId, CancellationToken cancellationToken)
    {
        var sql = "SELECT COUNT(*) " + FromAndFilter;
        var command = new CommandDefinition(sql, new { Keyword = keyword, IdentityTypeId = identityTypeId }, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        return await unitOfWork.Connection.ExecuteScalarAsync<int>(command);
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
        var command = new CommandDefinition(sql, parameters, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        return (await unitOfWork.Connection.QueryAsync<UserListItem>(command)).AsList();
    }

    public async Task<IReadOnlyList<IdentityTypeOption>> GetIdentityTypesAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT UIDIDNTYPE AS IdentityTypeId, NVCHIDENTITYNAME AS IdentityName
            FROM dbo.ERSSIDNTBL
            ORDER BY NVCHIDENTITYNAME;
            """;
        var command = new CommandDefinition(sql, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        return (await unitOfWork.Connection.QueryAsync<IdentityTypeOption>(command)).AsList();
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
        var command = new CommandDefinition(sql, new { UserId = userId }, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        return await unitOfWork.Connection.QuerySingleOrDefaultAsync<UserListItem>(command);
    }

    public async Task CreateAsync(string displayName, Guid identityTypeId, string actor, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.ERSSUSERTBL (UIDIDNTYPE, NVCHDISPLAYNAME, VCHCREATEBY)
            VALUES (@IdentityTypeId, @DisplayName, @Actor);
            """;
        var command = new CommandDefinition(sql, new { IdentityTypeId = identityTypeId, DisplayName = displayName, Actor = actor }, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        await unitOfWork.Connection.ExecuteAsync(command);
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
        var command = new CommandDefinition(sql, new { UserId = userId, IdentityTypeId = identityTypeId, DisplayName = displayName, Actor = actor }, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        return await unitOfWork.Connection.ExecuteAsync(command) == 1;
    }

    public async Task<bool> DeleteAsync(Guid userId, CancellationToken cancellationToken)
    {
        const string sql = "DELETE FROM dbo.ERSSUSERTBL WHERE UIDUSERID = @UserId;";
        var command = new CommandDefinition(sql, new { UserId = userId }, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);

        try
        {
            return await unitOfWork.Connection.ExecuteAsync(command) == 1;
        }
        catch (SqlException exception) when (exception.Number == 547)
        {
            return false;
        }
    }
}
