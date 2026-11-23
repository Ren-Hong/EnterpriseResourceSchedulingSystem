using Dapper;
using EnterpriseResourceSchedulingSystem.Application.Abstractions.Persistence;
using EnterpriseResourceSchedulingSystem.Application.Abstractions.Repositories;
using EnterpriseResourceSchedulingSystem.Application.Dtos;
using Microsoft.Data.SqlClient;

namespace EnterpriseResourceSchedulingSystem.Infrastructure.Repositories;

public sealed class ResourceRepository(IUnitOfWork unitOfWork) : IResourceRepository
{
    public async Task<IReadOnlyList<ScheduleResourceOption>> GetResourceOptionsAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT r.UIDRESID AS ResourceId, r.NVCHRESNAME AS ResourceName, c.INTMAXUSER AS MaxUsers
            FROM dbo.ERSSRESTBL AS r
            CROSS APPLY
            (
                SELECT TOP (1) config.INTMAXUSER
                FROM dbo.ERSSRESCFGTBL AS config
                WHERE config.UIDRESID = r.UIDRESID
                ORDER BY CASE WHEN CONVERT(date, DATEADD(HOUR, 8, SYSUTCDATETIME()))
                                   BETWEEN config.DTMEFFECTSTART AND config.DTMEFFECTEND THEN 0 ELSE 1 END,
                         config.NUMRESCFGNO DESC
            ) AS c
            ORDER BY r.NVCHRESNAME, r.UIDRESID;
            """;
        var command = new CommandDefinition(sql, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        return (await unitOfWork.Connection.QueryAsync<ScheduleResourceOption>(command)).AsList();
    }

    public async Task<int> CountAsync(string? keyword, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM dbo.ERSSRESTBL AS r
            WHERE (@Keyword IS NULL OR r.NVCHRESNAME LIKE N'%' + @Keyword + N'%');
            """;
        var command = new CommandDefinition(sql, new { Keyword = keyword }, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        return await unitOfWork.Connection.ExecuteScalarAsync<int>(command);
    }

    public async Task<IReadOnlyList<ResourceListItem>> GetPageAsync(string? keyword, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT r.UIDRESID AS ResourceId, r.NVCHRESNAME AS ResourceName,
                   c.NVCHRESDESC AS Description, c.INTMAXUSER AS MaxUsers,
                   r.VCHCREATEBY AS CreatedBy, r.DTMCREATEDAT AS CreatedAt
            FROM dbo.ERSSRESTBL AS r
            CROSS APPLY
            (
                SELECT TOP (1) config.NVCHRESDESC, config.INTMAXUSER
                FROM dbo.ERSSRESCFGTBL AS config
                WHERE config.UIDRESID = r.UIDRESID
                ORDER BY CASE WHEN CONVERT(date, DATEADD(HOUR, 8, SYSUTCDATETIME()))
                                   BETWEEN config.DTMEFFECTSTART AND config.DTMEFFECTEND THEN 0 ELSE 1 END,
                         config.NUMRESCFGNO DESC
            ) AS c
            WHERE (@Keyword IS NULL OR r.NVCHRESNAME LIKE N'%' + @Keyword + N'%')
            ORDER BY r.NVCHRESNAME, r.UIDRESID
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;
        var parameters = new { Keyword = keyword, Offset = (long)(pageNumber - 1) * pageSize, PageSize = pageSize };
        var command = new CommandDefinition(sql, parameters, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        return (await unitOfWork.Connection.QueryAsync<ResourceListItem>(command)).AsList();
    }

    public async Task<ResourceListItem?> FindByIdAsync(Guid resourceId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT r.UIDRESID AS ResourceId, r.NVCHRESNAME AS ResourceName,
                   c.NVCHRESDESC AS Description, c.INTMAXUSER AS MaxUsers,
                   r.VCHCREATEBY AS CreatedBy, r.DTMCREATEDAT AS CreatedAt
            FROM dbo.ERSSRESTBL AS r
            OUTER APPLY
            (
                SELECT TOP (1) config.NVCHRESDESC, config.INTMAXUSER
                FROM dbo.ERSSRESCFGTBL AS config
                WHERE config.UIDRESID = r.UIDRESID
                ORDER BY config.NUMRESCFGNO DESC
            ) AS c
            WHERE r.UIDRESID = @ResourceId;
            """;
        var command = new CommandDefinition(sql, new { ResourceId = resourceId }, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        return await unitOfWork.Connection.QuerySingleOrDefaultAsync<ResourceListItem>(command);
    }

    public async Task<ResourceSaveResult> CreateAsync(Guid resourceId, string name, string? description, int maxUsers, string actor, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.ERSSRESTBL (UIDRESID, NVCHRESNAME, NVCHRESDESC, INTMAXUSER, VCHCREATEBY)
            VALUES (@ResourceId, @Name, @Description, @MaxUsers, @Actor);
            """;
        var command = new CommandDefinition(sql, new { ResourceId = resourceId, Name = name, Description = description, MaxUsers = maxUsers, Actor = actor }, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        try
        {
            await unitOfWork.Connection.ExecuteAsync(command);
            return ResourceSaveResult.Saved;
        }
        catch (SqlException exception) when (IsUniqueConstraintViolation(exception))
        {
            return ResourceSaveResult.DuplicateName;
        }
    }

    public async Task<ResourceUpdateResult> UpdateAsync(Guid resourceId, string name, string actor, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.ERSSRESTBL
            SET NVCHRESNAME = @Name, VCHUPDATEBY = @Actor, DTMUPDATEDAT = SYSUTCDATETIME()
            WHERE UIDRESID = @ResourceId;
            """;
        var command = new CommandDefinition(sql, new { ResourceId = resourceId, Name = name, Actor = actor }, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        try
        {
            return await unitOfWork.Connection.ExecuteAsync(command) == 1 ? ResourceUpdateResult.Updated : ResourceUpdateResult.NotFound;
        }
        catch (SqlException exception) when (IsUniqueConstraintViolation(exception))
        {
            return ResourceUpdateResult.DuplicateName;
        }
    }
    public async Task<ResourceDeleteResult> DeleteAsync(Guid resourceId, CancellationToken cancellationToken)
    {
        const string sql = "DELETE FROM dbo.ERSSRESTBL WHERE UIDRESID = @ResourceId;";
        var command = new CommandDefinition(sql, new { ResourceId = resourceId }, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        try
        {
            return await unitOfWork.Connection.ExecuteAsync(command) == 1 ? ResourceDeleteResult.Deleted : ResourceDeleteResult.NotFound;
        }
        catch (SqlException exception) when (exception.Number == 547)
        {
            return ResourceDeleteResult.InUse;
        }
    }

    private static bool IsUniqueConstraintViolation(SqlException exception)
        => exception.Number is 2601 or 2627;
}

