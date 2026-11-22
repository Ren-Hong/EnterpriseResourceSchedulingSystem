using Dapper;
using EnterpriseResourceSchedulingSystem.Application.Abstractions.Persistence;
using EnterpriseResourceSchedulingSystem.Application.Abstractions.Repositories;
using EnterpriseResourceSchedulingSystem.Application.Dtos;
using Microsoft.Data.SqlClient;

namespace EnterpriseResourceSchedulingSystem.Infrastructure.Repositories;

public sealed class ResourceFeeRepository(IUnitOfWork unitOfWork) : IResourceFeeRepository
{
    public async Task<IReadOnlyList<ResourceFeeListItem>> GetByResourceAsync(Guid resourceId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT f.UIDRESFEEID AS ResourceFeeId, f.UIDRESCFGID AS ResourceConfigId,
                   c.NUMRESCFGNO AS ResourceConfigNo, f.UIDIDNTYPE AS IdentityTypeId,
                   i.NVCHIDENTITYNAME AS IdentityName, f.NUMFEE AS Fee,
                   f.DTMEFFECTSTART AS EffectiveStart, f.DTMEFFECTEND AS EffectiveEnd,
                   f.VCHCREATEBY AS CreatedBy, f.DTMCREATEDAT AS CreatedAt
            FROM dbo.ERSSRESFEETBL AS f
            INNER JOIN dbo.ERSSRESCFGTBL AS c ON c.UIDRESCFGID = f.UIDRESCFGID
            INNER JOIN dbo.ERSSIDNTBL AS i ON i.UIDIDNTYPE = f.UIDIDNTYPE
            WHERE c.UIDRESID = @ResourceId
            ORDER BY i.NVCHIDENTITYNAME, f.DTMEFFECTSTART DESC;
            """;
        var connection = await unitOfWork.GetOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { ResourceId = resourceId }, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        return (await connection.QueryAsync<ResourceFeeListItem>(command)).AsList();
    }

    public async Task<bool> HasOverlappingPeriodAsync(
        Guid resourceId,
        Guid identityTypeId,
        DateTime effectiveStart,
        DateTime effectiveEnd,
        Guid? excludeFeeId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT CASE WHEN EXISTS
            (
                SELECT 1
                FROM dbo.ERSSRESFEETBL WITH (UPDLOCK, HOLDLOCK)
                WHERE UIDRESCFGID = COALESCE
                  ((SELECT UIDRESCFGID FROM dbo.ERSSRESFEETBL WHERE UIDRESFEEID = @ExcludeFeeId),
                   (SELECT TOP (1) UIDRESCFGID FROM dbo.ERSSRESCFGTBL
                    WHERE UIDRESID = @ResourceId ORDER BY NUMRESCFGNO DESC))
                  AND UIDIDNTYPE = @IdentityTypeId
                  AND (@ExcludeFeeId IS NULL OR UIDRESFEEID <> @ExcludeFeeId)
                  AND DTMEFFECTSTART <= @EffectiveEnd
                  AND DTMEFFECTEND >= @EffectiveStart
            ) THEN 1 ELSE 0 END;
            """;
        var parameters = new { ResourceId = resourceId, IdentityTypeId = identityTypeId, EffectiveStart = effectiveStart, EffectiveEnd = effectiveEnd, ExcludeFeeId = excludeFeeId };
        var connection = await unitOfWork.GetOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, parameters, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(command);
    }

    public async Task<ResourceFeeSaveResult> CreateAsync(
        Guid resourceId, Guid identityTypeId, decimal fee, DateTime effectiveStart, DateTime effectiveEnd, string actor, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.ERSSRESFEETBL (UIDRESCFGID, UIDIDNTYPE, NUMFEE, DTMEFFECTSTART, DTMEFFECTEND, VCHCREATEBY)
            SELECT config.UIDRESCFGID, @IdentityTypeId, @Fee, @EffectiveStart, @EffectiveEnd, @Actor
            FROM dbo.ERSSRESCFGTBL AS config
            WHERE config.UIDRESID = @ResourceId
              AND config.NUMRESCFGNO = (SELECT MAX(currentConfig.NUMRESCFGNO) FROM dbo.ERSSRESCFGTBL AS currentConfig WHERE currentConfig.UIDRESID = @ResourceId);
            """;
        var parameters = new { ResourceId = resourceId, IdentityTypeId = identityTypeId, Fee = fee, EffectiveStart = effectiveStart, EffectiveEnd = effectiveEnd, Actor = actor };
        var connection = await unitOfWork.GetOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, parameters, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        try
        {
            await connection.ExecuteAsync(command);
            return ResourceFeeSaveResult.Saved;
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            return ResourceFeeSaveResult.DuplicateStart;
        }
        catch (SqlException exception) when (exception.Number == 547)
        {
            return ResourceFeeSaveResult.ReferenceNotFound;
        }
    }

    public async Task<ResourceFeeSaveResult> UpdateAsync(
        Guid resourceId, Guid resourceFeeId, Guid identityTypeId, decimal fee, DateTime effectiveStart, DateTime effectiveEnd, string actor, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE f
            SET UIDIDNTYPE = @IdentityTypeId,
                NUMFEE = @Fee,
                DTMEFFECTSTART = @EffectiveStart,
                DTMEFFECTEND = @EffectiveEnd,
                VCHUPDATEBY = @Actor,
                DTMUPDATEDAT = SYSUTCDATETIME()
            FROM dbo.ERSSRESFEETBL AS f
            INNER JOIN dbo.ERSSRESCFGTBL AS c ON c.UIDRESCFGID = f.UIDRESCFGID
            WHERE f.UIDRESFEEID = @ResourceFeeId
              AND c.UIDRESCFGID = f.UIDRESCFGID
              AND c.UIDRESID = @ResourceId;
            """;
        var parameters = new { ResourceId = resourceId, ResourceFeeId = resourceFeeId, IdentityTypeId = identityTypeId, Fee = fee, EffectiveStart = effectiveStart, EffectiveEnd = effectiveEnd, Actor = actor };
        var connection = await unitOfWork.GetOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, parameters, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        try
        {
            return await connection.ExecuteAsync(command) == 1 ? ResourceFeeSaveResult.Saved : ResourceFeeSaveResult.FeeNotFound;
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            return ResourceFeeSaveResult.DuplicateStart;
        }
        catch (SqlException exception) when (exception.Number == 547)
        {
            return ResourceFeeSaveResult.ReferenceNotFound;
        }
    }

    public async Task<ResourceFeeDeleteResult> DeleteAsync(Guid resourceId, Guid resourceFeeId, CancellationToken cancellationToken)
    {
        const string sql = """
            DELETE f
            FROM dbo.ERSSRESFEETBL AS f
            INNER JOIN dbo.ERSSRESCFGTBL AS c ON c.UIDRESCFGID = f.UIDRESCFGID
            WHERE c.UIDRESID = @ResourceId AND f.UIDRESFEEID = @ResourceFeeId;
            """;
        var connection = await unitOfWork.GetOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { ResourceId = resourceId, ResourceFeeId = resourceFeeId }, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        return await connection.ExecuteAsync(command) == 1 ? ResourceFeeDeleteResult.Deleted : ResourceFeeDeleteResult.NotFound;
    }
}
