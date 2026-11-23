using Dapper;
using EnterpriseResourceSchedulingSystem.Application.Abstractions.Persistence;
using EnterpriseResourceSchedulingSystem.Application.Abstractions.Repositories;
using EnterpriseResourceSchedulingSystem.Application.Dtos;
using Microsoft.Data.SqlClient;

namespace EnterpriseResourceSchedulingSystem.Infrastructure.Repositories;

public sealed class ResourceRepository(IUnitOfWork unitOfWork) : IResourceRepository
{
    public async Task<IReadOnlyList<ScheduleResourceOption>> GetAllForSchedulingAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT r.UIDRESID AS ResourceId, c.NVCHRESNAME AS ResourceName, c.INTMAXUSER AS MaxUsers
            FROM dbo.ERSSRESTBL AS r
            INNER JOIN dbo.ERSSRESCFGTBL AS c ON c.UIDRESID = r.UIDRESID
            WHERE c.DTMEFFECTSTART <= CONVERT(date, DATEADD(HOUR, 8, SYSUTCDATETIME())) AND c.DTMEFFECTEND >= CONVERT(date, DATEADD(HOUR, 8, SYSUTCDATETIME()))
            ORDER BY c.NVCHRESNAME, r.UIDRESID;
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
            SELECT r.UIDRESID AS ResourceId, c.NVCHRESNAME AS ResourceName,
                   c.NVCHRESDESC AS Description, c.INTMAXUSER AS MaxUsers,
                   c.VCHCREATEBY AS CreatedBy, c.DTMCREATEDAT AS CreatedAt
            FROM dbo.ERSSRESTBL AS r
            INNER JOIN dbo.ERSSRESCFGTBL AS c ON c.UIDRESID = r.UIDRESID
            WHERE c.DTMEFFECTSTART <= CONVERT(date, DATEADD(HOUR, 8, SYSUTCDATETIME())) AND c.DTMEFFECTEND >= CONVERT(date, DATEADD(HOUR, 8, SYSUTCDATETIME()))
              AND (@Keyword IS NULL OR r.NVCHRESNAME LIKE N'%' + @Keyword + N'%')
            ORDER BY c.NVCHRESNAME, r.UIDRESID
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;
        var parameters = new { Keyword = keyword, Offset = (long)(pageNumber - 1) * pageSize, PageSize = pageSize };
        var command = new CommandDefinition(sql, parameters, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        return (await unitOfWork.Connection.QueryAsync<ResourceListItem>(command)).AsList();
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
        var command = new CommandDefinition(sql, new { ResourceId = resourceId }, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        return await unitOfWork.Connection.QuerySingleOrDefaultAsync<ResourceListItem>(command);
    }

    public async Task<IReadOnlyList<ResourceVersionListItem>> GetVersionsAsync(Guid resourceId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT UIDRESCFGID AS ResourceConfigId, NUMRESCFGNO AS ResourceConfigNo,
                   NVCHRESNAME AS ResourceName, NVCHRESDESC AS Description, INTMAXUSER AS MaxUsers,
                   CONVERT(date, DTMEFFECTSTART) AS EffectiveStart, CONVERT(date, DTMEFFECTEND) AS EffectiveEnd,
                   VCHCREATEBY AS CreatedBy, DTMCREATEDAT AS CreatedAt
            FROM dbo.ERSSRESCFGTBL WHERE UIDRESID = @ResourceId
            ORDER BY NUMRESCFGNO DESC;
            """;
        var command = new CommandDefinition(sql, new { ResourceId = resourceId }, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        return (await unitOfWork.Connection.QueryAsync<ResourceVersionListItem>(command)).AsList();
    }
    public async Task<ResourceSaveResult> CreateAsync(string name, string? description, int maxUsers, string actor, CancellationToken cancellationToken)
    {
        const string sql = """
            DECLARE @ResourceId UNIQUEIDENTIFIER = NEWID();
            INSERT INTO dbo.ERSSRESTBL (UIDRESID, NVCHRESNAME, NVCHRESDESC, INTMAXUSER, VCHCREATEBY)
            VALUES (@ResourceId, @Name, @Description, @MaxUsers, @Actor);

            INSERT INTO dbo.ERSSRESCFGTBL
                (UIDRESID, NUMRESCFGNO, NVCHRESNAME, NVCHRESDESC, INTMAXUSER, DTMEFFECTSTART, DTMEFFECTEND, VCHCREATEBY)
            VALUES
                (@ResourceId, 1, @Name, @Description, @MaxUsers, CONVERT(date, DATEADD(HOUR, 8, SYSUTCDATETIME())), CONVERT(date, '99991231', 112), @Actor);
            """;
        var command = new CommandDefinition(sql, new { Name = name, Description = description, MaxUsers = maxUsers, Actor = actor }, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
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

    public async Task<ResourceUpdateResult> UpdateAsync(Guid resourceId, string name, string? description, int maxUsers, DateOnly effectiveStart, DateOnly effectiveEnd, string actor, CancellationToken cancellationToken)
    {
        const string sql = """
            SET NOCOUNT ON;
            DECLARE @StartDate DATE = @EffectiveStart;
            DECLARE @ConfigNo INT;
            DECLARE @OldConfigId UNIQUEIDENTIFIER;
            DECLARE @NewConfigId UNIQUEIDENTIFIER = NEWSEQUENTIALID();
            DECLARE @LastStart DATE;

            SELECT @LastStart = MAX(CONVERT(date, DTMEFFECTSTART))
            FROM dbo.ERSSRESCFGTBL WITH (UPDLOCK, HOLDLOCK)
            WHERE UIDRESID = @ResourceId;
            SELECT TOP (1) @OldConfigId = UIDRESCFGID
            FROM dbo.ERSSRESCFGTBL WITH (UPDLOCK, HOLDLOCK)
            WHERE UIDRESID = @ResourceId ORDER BY NUMRESCFGNO DESC;

            IF @LastStart IS NULL BEGIN SELECT CAST(0 AS INT); RETURN; END;
            IF @EffectiveEnd < @StartDate BEGIN SELECT CAST(-2 AS INT); RETURN; END;
            IF @StartDate <= @LastStart BEGIN SELECT CAST(-1 AS INT); RETURN; END;

            SELECT @ConfigNo = ISNULL(MAX(NUMRESCFGNO), 0) + 1
            FROM dbo.ERSSRESCFGTBL WITH (UPDLOCK, HOLDLOCK)
            WHERE UIDRESID = @ResourceId;

            UPDATE dbo.ERSSRESCFGTBL
            SET DTMEFFECTEND = DATEADD(DAY, -1, @StartDate), VCHUPDATEBY = @Actor, DTMUPDATEDAT = SYSUTCDATETIME()
            WHERE UIDRESID = @ResourceId AND CONVERT(date, DTMEFFECTSTART) < @StartDate AND CONVERT(date, DTMEFFECTEND) >= @StartDate;

            INSERT INTO dbo.ERSSRESCFGTBL
                (UIDRESCFGID, UIDRESID, NUMRESCFGNO, NVCHRESNAME, NVCHRESDESC, INTMAXUSER, DTMEFFECTSTART, DTMEFFECTEND, VCHCREATEBY)
            VALUES (@NewConfigId, @ResourceId, @ConfigNo, @Name, @Description, @MaxUsers, @StartDate, @EffectiveEnd, @Actor);

            INSERT INTO dbo.ERSSRESFEETBL (UIDRESCFGID, UIDIDNTYPE, NUMFEE, VCHCREATEBY)
            SELECT @NewConfigId, UIDIDNTYPE, NUMFEE, @Actor
            FROM dbo.ERSSRESFEETBL WHERE UIDRESCFGID = @OldConfigId;

            UPDATE dbo.ERSSRESTBL
            SET NVCHRESNAME = @Name, NVCHRESDESC = @Description, INTMAXUSER = @MaxUsers,
                VCHUPDATEBY = @Actor, DTMUPDATEDAT = SYSUTCDATETIME()
            WHERE UIDRESID = @ResourceId;
            SELECT CAST(1 AS INT);
            """;
        var parameters = new { ResourceId = resourceId, Name = name, Description = description, MaxUsers = maxUsers, EffectiveStart = effectiveStart, EffectiveEnd = effectiveEnd, Actor = actor };
        var command = new CommandDefinition(sql, parameters, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        try
        {
            var result = await unitOfWork.Connection.ExecuteScalarAsync<int>(command);
            return result switch { 1 => ResourceUpdateResult.Updated, -1 => ResourceUpdateResult.InvalidEffectiveStart, -2 => ResourceUpdateResult.InvalidEffectivePeriod, _ => ResourceUpdateResult.NotFound };
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

