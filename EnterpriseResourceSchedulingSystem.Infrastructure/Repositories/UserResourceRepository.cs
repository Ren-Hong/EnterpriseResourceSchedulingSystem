using Dapper;
using EnterpriseResourceSchedulingSystem.Application.Abstractions.Persistence;
using EnterpriseResourceSchedulingSystem.Application.Abstractions.Repositories;
using EnterpriseResourceSchedulingSystem.Application.Dtos;
using Microsoft.Data.SqlClient;

namespace EnterpriseResourceSchedulingSystem.Infrastructure.Repositories;

public sealed class UserResourceRepository(IUnitOfWork unitOfWork) : IUserResourceRepository
{
    public async Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.ERSSUSERTBL WHERE UIDUSERID = @UserId) THEN 1 ELSE 0 END;";
        var command = new CommandDefinition(sql, new { UserId = userId }, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        return await unitOfWork.Connection.ExecuteScalarAsync<bool>(command);
    }

    public async Task<UserResourcePermissionList> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        // 已有該設備權限的SQL
        const string assignedSql = """ 
            SELECT r.UIDRESID AS ResourceId, r.NVCHRESNAME AS ResourceName,
                   ur.VCHCREATEBY AS CreatedBy, ur.DTMCREATEDAT AS CreatedAt
            FROM dbo.ERSSUSERRES AS ur
            INNER JOIN dbo.ERSSRESTBL AS r ON r.UIDRESID = ur.UIDRESID
            WHERE ur.UIDUSERID = @UserId
            ORDER BY r.NVCHRESNAME;
            """;

        // 還沒有該權限設備的SQL
        const string availableSql = """
            SELECT r.UIDRESID AS ResourceId, r.NVCHRESNAME AS ResourceName
            FROM dbo.ERSSRESTBL AS r
            WHERE NOT EXISTS
            (
                SELECT 1 FROM dbo.ERSSUSERRES AS ur
                WHERE ur.UIDUSERID = @UserId AND ur.UIDRESID = r.UIDRESID
            )
            ORDER BY r.NVCHRESNAME;
            """;

        var assignedCommand = new CommandDefinition(assignedSql, new { UserId = userId }, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        var availableCommand = new CommandDefinition(availableSql, new { UserId = userId }, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        var assigned = (await unitOfWork.Connection.QueryAsync<UserResourcePermissionItem>(assignedCommand)).AsList();
        var available = (await unitOfWork.Connection.QueryAsync<ResourceOption>(availableCommand)).AsList();
        return new UserResourcePermissionList(assigned, available);
    }

    public async Task<AssignResourceResult> AssignAsync(Guid userId, Guid resourceId, string actor, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.ERSSUSERRES (UIDUSERID, UIDRESID, VCHCREATEBY)
            VALUES (@UserId, @ResourceId, @Actor);
            """;
        var command = new CommandDefinition(sql, new { UserId = userId, ResourceId = resourceId, Actor = actor }, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        try
        {
            await unitOfWork.Connection.ExecuteAsync(command);
            return AssignResourceResult.Assigned;
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            return AssignResourceResult.AlreadyAssigned;
        }
        catch (SqlException exception) when (exception.Number == 547)
        {
            return AssignResourceResult.NotFound;
        }
    }

    public async Task<bool> RevokeAsync(Guid userId, Guid resourceId, CancellationToken cancellationToken)
    {
        const string sql = "DELETE FROM dbo.ERSSUSERRES WHERE UIDUSERID = @UserId AND UIDRESID = @ResourceId;";
        var command = new CommandDefinition(sql, new { UserId = userId, ResourceId = resourceId }, transaction: unitOfWork.Transaction, cancellationToken: cancellationToken);
        return await unitOfWork.Connection.ExecuteAsync(command) == 1;
    }
}
