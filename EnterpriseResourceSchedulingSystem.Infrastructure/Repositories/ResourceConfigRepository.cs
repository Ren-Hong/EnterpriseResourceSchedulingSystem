using Dapper;
using EnterpriseResourceSchedulingSystem.Application.Abstractions.Persistence;
using EnterpriseResourceSchedulingSystem.Application.Abstractions.Repositories;
using EnterpriseResourceSchedulingSystem.Application.Dtos;

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
            INNER JOIN dbo.ERSSRESTBL AS r ON r.UIDRESID = c.UIDRESID
            WHERE c.UIDRESID = @ResourceId
            ORDER BY c.NUMRESCFGNO DESC;
            """;
        var command = new CommandDefinition(sql, new { ResourceId = resourceId }, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        return (await unitOfWork.Connection.QueryAsync<ResourceVersionListItem>(command)).AsList();
    }

    public async Task CreateInitialVersionAsync(Guid resourceId, string name, string? description, int maxUsers, string actor, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.ERSSRESCFGTBL
                (UIDRESID, NUMRESCFGNO, NVCHRESNAME, NVCHRESDESC, INTMAXUSER, DTMEFFECTSTART, DTMEFFECTEND, VCHCREATEBY)
            VALUES
                (@ResourceId, 1, @Name, @Description, @MaxUsers,
                 CONVERT(date, DATEADD(HOUR, 8, SYSUTCDATETIME())), CONVERT(date, '99991231', 112), @Actor);
            """;
        var command = new CommandDefinition(sql, new { ResourceId = resourceId, Name = name, Description = description, MaxUsers = maxUsers, Actor = actor }, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        await unitOfWork.Connection.ExecuteAsync(command);
    }

    public async Task<ResourceVersionCreateResult> CreateVersionAsync(Guid resourceId, string? description, int maxUsers, DateOnly effectiveStart, DateOnly effectiveEnd, string actor, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.ERSSRESCFGTBL
                (UIDRESID, NUMRESCFGNO, NVCHRESNAME, NVCHRESDESC, INTMAXUSER, DTMEFFECTSTART, DTMEFFECTEND, VCHCREATEBY)
            SELECT r.UIDRESID,
                   COALESCE((
                       SELECT MAX(c.NUMRESCFGNO)
                       FROM dbo.ERSSRESCFGTBL AS c WITH (UPDLOCK, HOLDLOCK)
                       WHERE c.UIDRESID = @ResourceId
                   ), 0) + 1,
                   r.NVCHRESNAME, @Description, @MaxUsers, @EffectiveStart, @EffectiveEnd, @Actor
            FROM dbo.ERSSRESTBL AS r
            WHERE r.UIDRESID = @ResourceId;
            """;
        var parameters = new { ResourceId = resourceId, Description = description, MaxUsers = maxUsers, EffectiveStart = effectiveStart, EffectiveEnd = effectiveEnd, Actor = actor };
        var command = new CommandDefinition(sql, parameters, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        var affected = await unitOfWork.Connection.ExecuteAsync(command);
        return affected == 1 ? ResourceVersionCreateResult.Created : ResourceVersionCreateResult.NotFound;
    }
}
