using Dapper;
using EnterpriseResourceSchedulingSystem.Application.Abstractions.Persistence;
using EnterpriseResourceSchedulingSystem.Application.Abstractions.Repositories;
using EnterpriseResourceSchedulingSystem.Application.Dtos;
using Microsoft.Data.SqlClient;

namespace EnterpriseResourceSchedulingSystem.Infrastructure.Repositories;

public sealed class ResourceRepository(IUnitOfWork unitOfWork) : IResourceRepository
{
    public async Task<int> CountAsync(string? keyword, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM dbo.ERSSRESCFGTBL AS c
            WHERE c.DTMEFFECTEND IS NULL
              AND (@Keyword IS NULL OR c.NVCHRESNAME LIKE N'%' + @Keyword + N'%');
            """;
        var connection = await unitOfWork.GetOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { Keyword = keyword }, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        return await connection.ExecuteScalarAsync<int>(command);
    }

    public async Task<IReadOnlyList<ResourceListItem>> GetPageAsync(string? keyword, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT c.UIDRESID AS ResourceId, c.NVCHRESNAME AS ResourceName,
                   c.NVCHRESDESC AS Description, c.INTMAXUSER AS MaxUsers,
                   c.VCHCREATEBY AS CreatedBy, c.DTMCREATEDAT AS CreatedAt
            FROM dbo.ERSSRESCFGTBL AS c
            WHERE c.DTMEFFECTEND IS NULL
              AND (@Keyword IS NULL OR c.NVCHRESNAME LIKE N'%' + @Keyword + N'%')
            ORDER BY c.NVCHRESNAME, c.UIDRESID
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;
        var parameters = new { Keyword = keyword, Offset = (long)(pageNumber - 1) * pageSize, PageSize = pageSize };
        var connection = await unitOfWork.GetOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, parameters, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        return (await connection.QueryAsync<ResourceListItem>(command)).AsList();
    }

    public async Task<ResourceListItem?> FindByIdAsync(Guid resourceId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (1) UIDRESID AS ResourceId, NVCHRESNAME AS ResourceName,
                   NVCHRESDESC AS Description, INTMAXUSER AS MaxUsers,
                   VCHCREATEBY AS CreatedBy, DTMCREATEDAT AS CreatedAt
            FROM dbo.ERSSRESCFGTBL
            WHERE UIDRESID = @ResourceId
            ORDER BY DTMEFFECTSTART DESC, NUMRESCFGNO DESC;
            """;
        var connection = await unitOfWork.GetOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { ResourceId = resourceId }, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<ResourceListItem>(command);
    }

    public async Task<ResourceSaveResult> CreateAsync(string name, string? description, int maxUsers, string actor, CancellationToken cancellationToken)
    {
        const string sql = """
            DECLARE @ResourceId UNIQUEIDENTIFIER = NEWID();
            INSERT INTO dbo.ERSSRESTBL (UIDRESID, NVCHRESNAME, NVCHRESDESC, INTMAXUSER, VCHCREATEBY)
            VALUES (@ResourceId, @Name, @Description, @MaxUsers, @Actor);

            INSERT INTO dbo.ERSSRESCFGTBL
                (UIDRESID, NUMRESCFGNO, NVCHRESNAME, NVCHRESDESC, INTMAXUSER, DTMEFFECTSTART, VCHCREATEBY)
            VALUES
                (@ResourceId, 1, @Name, @Description, @MaxUsers, SYSUTCDATETIME(), @Actor);
            """;
        var connection = await unitOfWork.GetOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { Name = name, Description = description, MaxUsers = maxUsers, Actor = actor }, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        try
        {
            await connection.ExecuteAsync(command);
            return ResourceSaveResult.Saved;
        }
        catch (SqlException exception) when (IsUniqueConstraintViolation(exception))
        {
            return ResourceSaveResult.DuplicateName;
        }
    }

    public async Task<ResourceUpdateResult> UpdateAsync(Guid resourceId, string name, string? description, int maxUsers, string actor, CancellationToken cancellationToken)
    {
        const string sql = """
            DECLARE @Clock DATETIME2(0) = SYSUTCDATETIME();
            DECLARE @LastStart DATETIME2(0);
            SELECT @LastStart = MAX(DTMEFFECTSTART)
            FROM dbo.ERSSRESCFGTBL WITH (UPDLOCK, HOLDLOCK)
            WHERE UIDRESID = @ResourceId;
            DECLARE @Now DATETIME2(0) = CASE WHEN @LastStart >= @Clock THEN DATEADD(SECOND, 1, @LastStart) ELSE @Clock END;
            DECLARE @ConfigNo INT;
            SELECT @ConfigNo = ISNULL(MAX(NUMRESCFGNO), 0) + 1
            FROM dbo.ERSSRESCFGTBL WITH (UPDLOCK, HOLDLOCK)
            WHERE UIDRESID = @ResourceId;

            IF NOT EXISTS (SELECT 1 FROM dbo.ERSSRESTBL WHERE UIDRESID = @ResourceId)
            BEGIN
                SELECT CAST(0 AS INT);
                RETURN;
            END;

            UPDATE dbo.ERSSRESCFGTBL
            SET DTMEFFECTEND = @Now,
                VCHUPDATEBY = @Actor,
                DTMUPDATEDAT = @Now
            WHERE UIDRESID = @ResourceId AND DTMEFFECTEND IS NULL;

            UPDATE dbo.ERSSRESTBL
            SET NVCHRESNAME = @Name, NVCHRESDESC = @Description, INTMAXUSER = @MaxUsers,
                VCHUPDATEBY = @Actor, DTMUPDATEDAT = @Now
            WHERE UIDRESID = @ResourceId;

            INSERT INTO dbo.ERSSRESCFGTBL
                (UIDRESID, NUMRESCFGNO, NVCHRESNAME, NVCHRESDESC, INTMAXUSER, DTMEFFECTSTART, VCHCREATEBY)
            VALUES
                (@ResourceId, @ConfigNo, @Name, @Description, @MaxUsers,
                 @Now, @Actor);

            SELECT CAST(1 AS INT);
            """;
        var connection = await unitOfWork.GetOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { ResourceId = resourceId, Name = name, Description = description, MaxUsers = maxUsers, Actor = actor }, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        try
        {
            return await connection.ExecuteScalarAsync<int>(command) == 1 ? ResourceUpdateResult.Updated : ResourceUpdateResult.NotFound;
        }
        catch (SqlException exception) when (IsUniqueConstraintViolation(exception))
        {
            return ResourceUpdateResult.DuplicateName;
        }
    }

    public async Task<ResourceDeleteResult> DeleteAsync(Guid resourceId, CancellationToken cancellationToken)
    {
        const string sql = "DELETE FROM dbo.ERSSRESTBL WHERE UIDRESID = @ResourceId;";
        var connection = await unitOfWork.GetOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { ResourceId = resourceId }, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        try
        {
            return await connection.ExecuteAsync(command) == 1 ? ResourceDeleteResult.Deleted : ResourceDeleteResult.NotFound;
        }
        catch (SqlException exception) when (exception.Number == 547)
        {
            return ResourceDeleteResult.InUse;
        }
    }

    private static bool IsUniqueConstraintViolation(SqlException exception)
        => exception.Number is 2601 or 2627;
}
