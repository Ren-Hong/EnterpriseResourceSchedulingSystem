using Dapper;
using EnterpriseResourceSchedulingSystem.Application.Abstractions.Persistence;
using EnterpriseResourceSchedulingSystem.Application.Abstractions.Repositories;
using EnterpriseResourceSchedulingSystem.Application.Dtos;
using Microsoft.Data.SqlClient;

namespace EnterpriseResourceSchedulingSystem.Infrastructure.Repositories;

public sealed class IdentityTypeRepository(IUnitOfWork unitOfWork) : IIdentityTypeRepository
{
    public async Task<int> CountAsync(string? keyword, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM dbo.ERSSIDNTBL
            WHERE (@Keyword IS NULL OR NVCHIDENTITYNAME LIKE N'%' + @Keyword + N'%');
            """;
        var command = new CommandDefinition(sql, new { Keyword = keyword }, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        return await unitOfWork.Connection.ExecuteScalarAsync<int>(command);
    }

    public async Task<IReadOnlyList<IdentityTypeListItem>> GetPageAsync(
        string? keyword,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT UIDIDNTYPE AS IdentityTypeId,
                   NVCHIDENTITYNAME AS IdentityName,
                   NVCHDESCRIPTION AS Description,
                   VCHCREATEBY AS CreatedBy,
                   DTMCREATEDAT AS CreatedAt
            FROM dbo.ERSSIDNTBL
            WHERE (@Keyword IS NULL OR NVCHIDENTITYNAME LIKE N'%' + @Keyword + N'%')
            ORDER BY NVCHIDENTITYNAME, UIDIDNTYPE
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;
        var parameters = new
        {
            Keyword = keyword,
            Offset = (long)(pageNumber - 1) * pageSize,
            PageSize = pageSize
        };
        var command = new CommandDefinition(sql, parameters, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        return (await unitOfWork.Connection.QueryAsync<IdentityTypeListItem>(command)).AsList();
    }

    public async Task<IdentityTypeListItem?> FindByIdAsync(Guid identityTypeId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT UIDIDNTYPE AS IdentityTypeId,
                   NVCHIDENTITYNAME AS IdentityName,
                   NVCHDESCRIPTION AS Description,
                   VCHCREATEBY AS CreatedBy,
                   DTMCREATEDAT AS CreatedAt
            FROM dbo.ERSSIDNTBL
            WHERE UIDIDNTYPE = @IdentityTypeId;
            """;
        var command = new CommandDefinition(sql, new { IdentityTypeId = identityTypeId }, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        return await unitOfWork.Connection.QuerySingleOrDefaultAsync<IdentityTypeListItem>(command);
    }

    public async Task<bool> CreateAsync(string identityName, string? description, string actor, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.ERSSIDNTBL (NVCHIDENTITYNAME, NVCHDESCRIPTION, VCHCREATEBY)
            VALUES (@IdentityName, @Description, @Actor);
            """;
        var command = new CommandDefinition(sql, new { IdentityName = identityName, Description = description, Actor = actor }, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        try
        {
            await unitOfWork.Connection.ExecuteAsync(command);
            return true;
        }
        catch (SqlException exception) when (IsUniqueConstraintViolation(exception))
        {
            return false;
        }
    }

    public async Task<IdentityTypeUpdateResult> UpdateAsync(
        Guid identityTypeId,
        string identityName,
        string? description,
        string actor,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.ERSSIDNTBL
            SET NVCHIDENTITYNAME = @IdentityName,
                NVCHDESCRIPTION = @Description,
                VCHUPDATEBY = @Actor,
                DTMUPDATEDAT = SYSUTCDATETIME()
            WHERE UIDIDNTYPE = @IdentityTypeId;
            """;
        var command = new CommandDefinition(sql, new { IdentityTypeId = identityTypeId, IdentityName = identityName, Description = description, Actor = actor }, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        try
        {
            return await unitOfWork.Connection.ExecuteAsync(command) == 1
                ? IdentityTypeUpdateResult.Updated
                : IdentityTypeUpdateResult.NotFound;
        }
        catch (SqlException exception) when (IsUniqueConstraintViolation(exception))
        {
            return IdentityTypeUpdateResult.DuplicateName;
        }
    }

    public async Task<IdentityTypeDeleteResult> DeleteAsync(Guid identityTypeId, CancellationToken cancellationToken)
    {
        const string sql = "DELETE FROM dbo.ERSSIDNTBL WHERE UIDIDNTYPE = @IdentityTypeId;";
        var command = new CommandDefinition(sql, new { IdentityTypeId = identityTypeId }, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        try
        {
            return await unitOfWork.Connection.ExecuteAsync(command) == 1
                ? IdentityTypeDeleteResult.Deleted
                : IdentityTypeDeleteResult.NotFound;
        }
        catch (SqlException exception) when (exception.Number == 547)
        {
            return IdentityTypeDeleteResult.InUse;
        }
    }

    private static bool IsUniqueConstraintViolation(SqlException exception)
        => exception.Number is 2601 or 2627;
}
