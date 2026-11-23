using Dapper;
using EnterpriseResourceSchedulingSystem.Application.Abstractions.Persistence;
using EnterpriseResourceSchedulingSystem.Application.Abstractions.Repositories;
using EnterpriseResourceSchedulingSystem.Application.Dtos;
using Microsoft.Data.SqlClient;

namespace EnterpriseResourceSchedulingSystem.Infrastructure.Repositories;

public sealed class ResourceFeeRepository(IUnitOfWork unitOfWork) : IResourceFeeRepository
{
    public async Task<IReadOnlyList<ResourceFeeListItem>> GetByResourceAsync(Guid resourceId, Guid? resourceConfigId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT f.UIDRESFEEID AS ResourceFeeId, f.UIDRESCFGID AS ResourceConfigId,
                   c.NUMRESCFGNO AS ResourceConfigNo, f.UIDIDNTYPE AS IdentityTypeId,
                   i.NVCHIDENTITYNAME AS IdentityName, f.NUMFEE AS Fee,
                   c.DTMEFFECTSTART AS ConfigEffectiveStart, c.DTMEFFECTEND AS ConfigEffectiveEnd,
                   f.VCHCREATEBY AS CreatedBy, f.DTMCREATEDAT AS CreatedAt
            FROM dbo.ERSSRESFEETBL AS f
            INNER JOIN dbo.ERSSRESCFGTBL AS c ON c.UIDRESCFGID = f.UIDRESCFGID
            INNER JOIN dbo.ERSSIDNTBL AS i ON i.UIDIDNTYPE = f.UIDIDNTYPE
            WHERE c.UIDRESID = @ResourceId AND (@ResourceConfigId IS NULL OR c.UIDRESCFGID = @ResourceConfigId)
            ORDER BY c.NUMRESCFGNO DESC, i.NVCHIDENTITYNAME;
            """;
        var connection = await unitOfWork.GetOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { ResourceId = resourceId, ResourceConfigId = resourceConfigId }, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        return (await connection.QueryAsync<ResourceFeeListItem>(command)).AsList();
    }

    public async Task<ResourceFeeSaveResult> CreateAsync(Guid resourceId, Guid resourceConfigId, Guid identityTypeId, decimal fee, string actor, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.ERSSRESFEETBL (UIDRESCFGID, UIDIDNTYPE, NUMFEE, VCHCREATEBY)
            SELECT c.UIDRESCFGID, @IdentityTypeId, @Fee, @Actor
            FROM dbo.ERSSRESCFGTBL c
            WHERE c.UIDRESID=@ResourceId AND c.UIDRESCFGID=@ResourceConfigId
              AND c.NUMRESCFGNO=(SELECT MAX(latest.NUMRESCFGNO) FROM dbo.ERSSRESCFGTBL latest WHERE latest.UIDRESID=@ResourceId)
              AND EXISTS (SELECT 1 FROM dbo.ERSSIDNTBL WHERE UIDIDNTYPE=@IdentityTypeId);
            SELECT CAST(@@ROWCOUNT AS INT);
            """;
        var connection = await unitOfWork.GetOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { ResourceId = resourceId, ResourceConfigId = resourceConfigId, IdentityTypeId = identityTypeId, Fee = fee, Actor = actor }, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        try
        {
            var affected = await connection.ExecuteScalarAsync<int>(command);
            if (affected == 1) return ResourceFeeSaveResult.Saved;
            return await IdentityTypeExistsAsync(identityTypeId, cancellationToken) ? ResourceFeeSaveResult.ReferenceNotFound : ResourceFeeSaveResult.IdentityTypeNotFound;
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627) { return ResourceFeeSaveResult.DuplicateIdentity; }
        catch (SqlException exception) when (exception.Number == 547) { return ResourceFeeSaveResult.ReferenceNotFound; }
    }

    public async Task<ResourceFeeSaveResult> UpdateAsync(Guid resourceId, Guid resourceConfigId, Guid resourceFeeId, Guid identityTypeId, decimal fee, string actor, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE f SET UIDIDNTYPE=@IdentityTypeId, NUMFEE=@Fee, VCHUPDATEBY=@Actor, DTMUPDATEDAT=SYSUTCDATETIME()
            FROM dbo.ERSSRESFEETBL f JOIN dbo.ERSSRESCFGTBL c ON c.UIDRESCFGID=f.UIDRESCFGID
            WHERE c.UIDRESID=@ResourceId AND c.UIDRESCFGID=@ResourceConfigId AND f.UIDRESFEEID=@ResourceFeeId
              AND c.NUMRESCFGNO=(SELECT MAX(latest.NUMRESCFGNO) FROM dbo.ERSSRESCFGTBL latest WHERE latest.UIDRESID=@ResourceId)
              AND EXISTS (SELECT 1 FROM dbo.ERSSIDNTBL WHERE UIDIDNTYPE=@IdentityTypeId);
            """;
        var connection = await unitOfWork.GetOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { ResourceId = resourceId, ResourceConfigId = resourceConfigId, ResourceFeeId = resourceFeeId, IdentityTypeId = identityTypeId, Fee = fee, Actor = actor }, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        try
        {
            if (await connection.ExecuteAsync(command) == 1) return ResourceFeeSaveResult.Saved;
            return await IdentityTypeExistsAsync(identityTypeId, cancellationToken) ? ResourceFeeSaveResult.FeeNotFound : ResourceFeeSaveResult.IdentityTypeNotFound;
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627) { return ResourceFeeSaveResult.DuplicateIdentity; }
        catch (SqlException exception) when (exception.Number == 547) { return ResourceFeeSaveResult.ReferenceNotFound; }
    }

    public async Task<ResourceFeeDeleteResult> DeleteAsync(Guid resourceId, Guid resourceConfigId, Guid resourceFeeId, CancellationToken cancellationToken)
    {
        const string sql = """
            DELETE f FROM dbo.ERSSRESFEETBL f JOIN dbo.ERSSRESCFGTBL c ON c.UIDRESCFGID=f.UIDRESCFGID
            WHERE c.UIDRESID=@ResourceId AND c.UIDRESCFGID=@ResourceConfigId AND f.UIDRESFEEID=@ResourceFeeId
              AND c.NUMRESCFGNO=(SELECT MAX(latest.NUMRESCFGNO) FROM dbo.ERSSRESCFGTBL latest WHERE latest.UIDRESID=@ResourceId);
            """;
        var connection = await unitOfWork.GetOpenConnectionAsync(cancellationToken);
        return await connection.ExecuteAsync(new CommandDefinition(sql, new { ResourceId = resourceId, ResourceConfigId = resourceConfigId, ResourceFeeId = resourceFeeId }, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken)) == 1 ? ResourceFeeDeleteResult.Deleted : ResourceFeeDeleteResult.NotFound;
    }

    private async Task<bool> IdentityTypeExistsAsync(Guid identityTypeId, CancellationToken cancellationToken)
    {
        var connection = await unitOfWork.GetOpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition("SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.ERSSIDNTBL WHERE UIDIDNTYPE=@IdentityTypeId) THEN 1 ELSE 0 END;", new { IdentityTypeId = identityTypeId }, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken));
    }
}
