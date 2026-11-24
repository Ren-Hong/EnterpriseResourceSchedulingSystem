using Dapper;
using EnterpriseResourceSchedulingSystem.Application.Abstractions.Persistence;
using EnterpriseResourceSchedulingSystem.Application.Abstractions.Repositories;
using EnterpriseResourceSchedulingSystem.Application.Dtos;
using EnterpriseResourceSchedulingSystem.Domain.Resources;

namespace EnterpriseResourceSchedulingSystem.Infrastructure.Repositories;

public sealed class ResourceConfigRepository(IUnitOfWork unitOfWork) : IResourceConfigRepository
{
    public async Task<IReadOnlyList<ResourceVersionListItem>> GetVersionsAsync(Guid resourceId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT c.UIDRESCFGID AS ResourceConfigId, c.NUMRESCFGNO AS ResourceConfigNo,
                   r.NVCHRESNAME AS ResourceName, c.NVCHRESDESC AS Description, c.INTMAXUSER AS MaxUsers,
                   CONVERT(date, c.DTMEFFECTSTART) AS EffectiveStart, CONVERT(date, c.DTMEFFECTEND) AS EffectiveEnd,
                   c.VCHCREATEBY AS CreatedBy, c.DTMCREATEDAT AS CreatedAt
            FROM dbo.ERSSRESCFGTBL AS c
            JOIN dbo.ERSSRESTBL AS r ON r.UIDRESID = c.UIDRESID
            WHERE c.UIDRESID = @ResourceId
            ORDER BY c.NUMRESCFGNO DESC;
            """;
        var command = new CommandDefinition(sql, new { ResourceId = resourceId }, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        return (await unitOfWork.Connection.QueryAsync<ResourceVersionListItem>(command)).AsList();
    }

    public async Task<IReadOnlyList<ScheduleVersion>> GetEffectiveVersionsAsync(Guid resourceId, DateOnly date, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT UIDRESCFGID AS ResourceConfigId, NUMRESCFGNO AS Version, INTMAXUSER AS MaxUsers,
                   NVCHRESDESC AS Description, DTMEFFECTSTART AS EffectiveStart, DTMEFFECTEND AS EffectiveEnd
            FROM dbo.ERSSRESCFGTBL
            WHERE UIDRESID = @ResourceId AND @Date BETWEEN DTMEFFECTSTART AND DTMEFFECTEND
            ORDER BY NUMRESCFGNO DESC;
            """;
        var parameters = new { ResourceId = resourceId, Date = date };
        var command = new CommandDefinition(sql, parameters, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        return (await unitOfWork.Connection.QueryAsync<ScheduleVersion>(command)).AsList();
    }

    public async Task CreateInitialVersionAsync(Guid resourceId, string? description, int maxUsers, ResourceEffectivePeriod period, string actor, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.ERSSRESCFGTBL
                (UIDRESID, NUMRESCFGNO, NVCHRESDESC, INTMAXUSER, DTMEFFECTSTART, DTMEFFECTEND, VCHCREATEBY)
            VALUES
                (@ResourceId, 1, @Description, @MaxUsers,
                 @EffectiveStart, @EffectiveEnd, @Actor);
            """;
        var command = new CommandDefinition(sql, new { ResourceId = resourceId, Description = description, MaxUsers = maxUsers, EffectiveStart = period.Start, EffectiveEnd = period.End, Actor = actor }, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        await unitOfWork.Connection.ExecuteAsync(command);
    }

    public async Task<ResourceVersionCreateResult> CreateVersionAsync(Guid resourceId, string? description, int maxUsers, ResourceEffectivePeriod period, string actor, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.ERSSRESCFGTBL
                (UIDRESID, NUMRESCFGNO, NVCHRESDESC, INTMAXUSER, DTMEFFECTSTART, DTMEFFECTEND, VCHCREATEBY)
            SELECT r.UIDRESID,
                   COALESCE((
                       SELECT MAX(c.NUMRESCFGNO)
                       FROM dbo.ERSSRESCFGTBL AS c WITH (UPDLOCK, HOLDLOCK)
                       WHERE c.UIDRESID = @ResourceId
                   ), 0) + 1,
                   @Description, @MaxUsers, @EffectiveStart, @EffectiveEnd, @Actor
            FROM dbo.ERSSRESTBL AS r
            WHERE r.UIDRESID = @ResourceId;
            """;
        var parameters = new { ResourceId = resourceId, Description = description, MaxUsers = maxUsers, EffectiveStart = period.Start, EffectiveEnd = period.End, Actor = actor };
        var command = new CommandDefinition(sql, parameters, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        var affected = await unitOfWork.Connection.ExecuteAsync(command);
        return affected == 1 ? ResourceVersionCreateResult.Created : ResourceVersionCreateResult.NotFound;
    }

    public async Task<bool> UpdateVersionAsync(Guid resourceId, Guid resourceConfigId, string? description, int maxUsers, ResourceEffectivePeriod period, string actor, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.ERSSRESCFGTBL
            SET NVCHRESDESC = @Description, INTMAXUSER = @MaxUsers,
                DTMEFFECTSTART = @EffectiveStart, DTMEFFECTEND = @EffectiveEnd,
                VCHUPDATEBY = @Actor, DTMUPDATEDAT = SYSUTCDATETIME()
            WHERE UIDRESID = @ResourceId AND UIDRESCFGID = @ResourceConfigId;
            """;
        var parameters = new { ResourceId = resourceId, ResourceConfigId = resourceConfigId, Description = description, MaxUsers = maxUsers, EffectiveStart = period.Start, EffectiveEnd = period.End, Actor = actor };
        var command = new CommandDefinition(sql, parameters, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        return await unitOfWork.Connection.ExecuteAsync(command) == 1;
    }
}
