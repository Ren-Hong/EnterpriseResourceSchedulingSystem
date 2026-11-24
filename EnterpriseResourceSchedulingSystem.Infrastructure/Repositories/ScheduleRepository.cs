using Dapper;
using EnterpriseResourceSchedulingSystem.Application.Abstractions.Persistence;
using EnterpriseResourceSchedulingSystem.Application.Abstractions.Repositories;
using EnterpriseResourceSchedulingSystem.Application.Dtos;

using EnterpriseResourceSchedulingSystem.Domain.Scheduling;
using Slot = EnterpriseResourceSchedulingSystem.Domain.Scheduling.ScheduleSlot;
using ScheduleSlot = EnterpriseResourceSchedulingSystem.Application.Dtos.ScheduleSlot;

namespace EnterpriseResourceSchedulingSystem.Infrastructure.Repositories;

public sealed class ScheduleRepository(IUnitOfWork unitOfWork) : IScheduleRepository
{
    public async Task<IReadOnlyList<ScheduleSlot>> GetSlotsAsync(DateTime fromUtc, DateTime toUtc, Guid[] resources, Guid[] users, bool includeCancelled, bool includeUnbooked, CancellationToken ct)
    {
        if (resources.Length == 0 || users.Length == 0) return Array.Empty<ScheduleSlot>();

        const string sql = """
            SELECT s.UIDSLOTID AS SlotId, s.UIDRESID AS ResourceId, r.NVCHRESNAME AS ResourceName,
                   c.NUMRESCFGNO AS Version, s.DTMSTARTAT AS Start, s.DTMENDAT AS [End],
                   s.INTCAPACITY AS Capacity, bookings.Booked, s.NVCHSTATUS AS Status, s.NVCHNOTE AS Note,
                   c.DTMEFFECTSTART AS EffectiveStart, c.DTMEFFECTEND AS EffectiveEnd, c.INTMAXUSER AS MaxUsers, c.UIDRESCFGID AS ResourceConfigId
            FROM dbo.ERSSSLOTTBL AS s
            JOIN dbo.ERSSRESTBL AS r ON r.UIDRESID = s.UIDRESID
            LEFT JOIN dbo.ERSSRESCFGTBL AS c ON c.UIDRESCFGID = s.UIDRESCFGID
            CROSS APPLY (
                SELECT COUNT(*) AS Booked
                FROM dbo.ERSSSCHTBL AS b
                WHERE b.UIDSLOTID = s.UIDSLOTID AND b.NVCHSTATUS = N'BOOKED'
            ) AS bookings
            WHERE s.DTMSTARTAT < @ToUtc AND s.DTMENDAT > @FromUtc
              AND (@IncludeCancelled = 1 OR s.NVCHSTATUS <> N'CANCELLED')
              AND s.UIDRESID IN @Resources
              AND ((@IncludeUnbooked = 1 AND bookings.Booked = 0) OR EXISTS (
                  SELECT 1 FROM dbo.ERSSSCHTBL AS b
                  WHERE b.UIDSLOTID = s.UIDSLOTID AND b.UIDUSERID IN @Users
                    AND (b.NVCHSTATUS = N'BOOKED' OR s.NVCHSTATUS = N'CANCELLED')
              ))
            ORDER BY s.DTMSTARTAT, s.UIDSLOTID;
            """;
        return (await unitOfWork.Connection.QueryAsync<ScheduleSlot>(Command(sql, new
        {
            FromUtc = fromUtc, ToUtc = toUtc, Resources = resources, Users = users,
            IncludeCancelled = includeCancelled, IncludeUnbooked = includeUnbooked
        }, ct))).AsList();
    }

    public async Task<IReadOnlyList<ScheduleUserOption>> GetPermittedUsersAsync(Guid slotId, CancellationToken ct)
    {
        const string sql = """
            SELECT u.UIDUSERID AS UserId,
                   COALESCE(NULLIF(LTRIM(RTRIM(u.NVCHDISPLAYNAME)), N''), N'未設定姓名') AS DisplayName,
                   u.UIDIDNTYPE AS IdentityTypeId, i.NVCHIDENTITYNAME AS IdentityTypeName
            FROM dbo.ERSSSLOTTBL AS s
            JOIN dbo.ERSSUSERRES AS access ON access.UIDRESID = s.UIDRESID
            JOIN dbo.ERSSUSERTBL AS u ON u.UIDUSERID = access.UIDUSERID
            LEFT JOIN dbo.ERSSIDNTBL AS i ON i.UIDIDNTYPE = u.UIDIDNTYPE
            WHERE s.UIDSLOTID = @SlotId
              AND access.DTMEFFECTSTART <= s.DTMSTARTAT
              AND (access.DTMEFFECTEND IS NULL OR access.DTMEFFECTEND >= s.DTMENDAT)
              AND NOT EXISTS (
                  SELECT 1 FROM dbo.ERSSSCHTBL AS b
                  WHERE b.UIDSLOTID = s.UIDSLOTID AND b.UIDUSERID = u.UIDUSERID AND b.NVCHSTATUS = N'BOOKED'
              )
            ORDER BY DisplayName, u.UIDUSERID;
            """;
        return (await unitOfWork.Connection.QueryAsync<ScheduleUserOption>(Command(sql, new { SlotId = slotId }, ct))).AsList();
    }

    public async Task<IReadOnlyList<ScheduleBooking>> GetBookingsAsync(Guid slotId, CancellationToken ct)
    {
        const string sql = """
            SELECT b.UIDSCHID AS BookingId,
                   COALESCE(NULLIF(LTRIM(RTRIM(u.NVCHDISPLAYNAME)), N''), N'未設定姓名') AS DisplayName,
                   i.NVCHIDENTITYNAME AS IdentityTypeName
            FROM dbo.ERSSSCHTBL AS b
            JOIN dbo.ERSSUSERTBL AS u ON u.UIDUSERID = b.UIDUSERID
            LEFT JOIN dbo.ERSSIDNTBL AS i ON i.UIDIDNTYPE = b.UIDIDNTYPE
            WHERE b.UIDSLOTID = @SlotId AND b.NVCHSTATUS = N'BOOKED'
            ORDER BY u.NVCHDISPLAYNAME, u.UIDUSERID;
            """;
        return (await unitOfWork.Connection.QueryAsync<ScheduleBooking>(Command(sql, new { SlotId = slotId }, ct))).AsList();
    }

    public async Task<IReadOnlyList<ScheduleBookingChange>> GetBookingChangesAsync(Guid slotId, CancellationToken ct)
    {
        const string sql = """
            SELECT COALESCE(NULLIF(LTRIM(RTRIM(u.NVCHDISPLAYNAME)), N''), N'未設定姓名') AS DisplayName,
                   e.Action, TODATETIMEOFFSET(e.OccurredAt, '+00:00') AS OccurredAt,
                   CASE WHEN e.Actor IS NULL THEN NULL
                        WHEN e.Actor = N'SYSTEM' AND a.NVCHUSERNAME IS NULL THEN N'系統'
                        ELSE COALESCE(NULLIF(TRIM(a.NVCHDISPLAYNAME), N''), N'未設定姓名')
                   END AS Actor
            FROM dbo.ERSSSCHTBL AS b
            JOIN dbo.ERSSUSERTBL AS u ON u.UIDUSERID = b.UIDUSERID
            CROSS APPLY (VALUES
                (N'BOOKED', b.DTMBOOKEDAT, b.VCHCREATEBY),
                (N'CANCELLED', b.DTMCANCELAT, b.VCHCANCELBY)
            ) AS e (Action, OccurredAt, Actor)
            LEFT JOIN dbo.ERSSACCTBL AS a ON a.NVCHUSERNAME = e.Actor COLLATE Latin1_General_100_CI_AS_SC
            WHERE b.UIDSLOTID = @SlotId AND e.OccurredAt IS NOT NULL
            ORDER BY e.OccurredAt DESC, e.Action, b.UIDSCHID;
            """;
        return (await unitOfWork.Connection.QueryAsync<ScheduleBookingChange>(Command(sql, new { SlotId = slotId }, ct))).AsList();
    }

    public Task<bool> LockResourceForSlotCreationAsync(Guid resourceId, CancellationToken ct)
        => unitOfWork.Connection.ExecuteScalarAsync<bool>(Command("""
            SELECT CAST(1 AS bit) FROM dbo.ERSSRESTBL WITH (UPDLOCK, HOLDLOCK) WHERE UIDRESID = @ResourceId;
            """, new { ResourceId = resourceId }, ct));

    public Task<bool> HasOverlapAsync(Guid resourceId, DateTime startUtc, DateTime endUtc, CancellationToken ct)
        => unitOfWork.Connection.ExecuteScalarAsync<bool>(Command("""
            SELECT TOP (1) CAST(1 AS bit) --有查到東西就回傳 1 DAPPER會視作 TRUE
            FROM dbo.ERSSSLOTTBL
            WHERE UIDRESID = @ResourceId AND DTMSTARTAT < @EndUtc AND DTMENDAT > @StartUtc
              AND NVCHSTATUS <> N'CANCELLED';
            """, new { ResourceId = resourceId, StartUtc = startUtc, EndUtc = endUtc }, ct));

    public async Task<Guid> CreateSlotAsync(Guid resourceId, Guid configId, int capacity, DateTime startUtc, DateTime endUtc, string? note, string actor, CancellationToken ct)
    {
        const string sql = """
            DECLARE @Created TABLE (SlotId uniqueidentifier);
            INSERT INTO dbo.ERSSSLOTTBL
                (UIDRESID, UIDRESCFGID, DTMSTARTAT, DTMENDAT, INTCAPACITY, NVCHNOTE, VCHCREATEBY)
            OUTPUT INSERTED.UIDSLOTID INTO @Created
            VALUES (@ResourceId, @ConfigId, @StartUtc, @EndUtc, @Capacity, @Note, @Actor);

            SELECT SlotId FROM @Created;
            """;
        return await unitOfWork.Connection.QuerySingleAsync<Guid>(Command(sql, new
        {
            ResourceId = resourceId, ConfigId = configId, StartUtc = startUtc, EndUtc = endUtc,
            Capacity = capacity, Note = note?.Trim(), Actor = actor
        }, ct));
    }

    public Task<bool> LockBookingUserAsync(Guid userId, CancellationToken ct)
        => unitOfWork.Connection.ExecuteScalarAsync<bool>(Command("""
            SELECT CAST(1 AS bit)
            FROM dbo.ERSSUSERTBL WITH (UPDLOCK, HOLDLOCK)
            WHERE UIDUSERID = @UserId;
            """, new { UserId = userId }, ct));

    public async Task<(Slot Slot, DateTime UtcNow)?> GetSlotForUpdateAsync(Guid slotId, CancellationToken ct)
    {
        const string sql = """
            SELECT s.UIDSLOTID AS SlotId, s.NVCHSTATUS AS Status, s.DTMSTARTAT AS StartsAtUtc,
                   s.INTCAPACITY AS Capacity, c.INTMAXUSER AS CapacityLimit,
                   (SELECT COUNT(*) FROM dbo.ERSSSCHTBL AS b
                    WHERE b.UIDSLOTID = s.UIDSLOTID AND b.NVCHSTATUS = N'BOOKED') AS BookedCount,
                   SYSUTCDATETIME() AS UtcNow
            FROM dbo.ERSSSLOTTBL AS s WITH (UPDLOCK, HOLDLOCK)
            LEFT JOIN dbo.ERSSRESCFGTBL AS c ON c.UIDRESCFGID = s.UIDRESCFGID
            WHERE s.UIDSLOTID = @SlotId;
            """;

        var row = await unitOfWork.Connection.QuerySingleOrDefaultAsync<SlotForUpdateRow>(Command(sql, new { SlotId = slotId }, ct));
        if (row is null) return null;

        Enum.TryParse<ScheduleSlotStatus>(row.Status, ignoreCase: true, out var status);
        return (new Slot(row.SlotId, status, row.StartsAtUtc, row.Capacity, row.BookedCount, row.CapacityLimit), row.UtcNow);
    }

    public async Task<bool> IncreaseCapacityAsync(Guid slotId, int capacity, string actor, CancellationToken ct)
    {
        const string sql = """
            UPDATE s
            SET INTCAPACITY = @Capacity, VCHUPDATEBY = @Actor, DTMUPDATEDAT = SYSUTCDATETIME()
            FROM dbo.ERSSSLOTTBL AS s
            JOIN dbo.ERSSRESCFGTBL AS c ON c.UIDRESCFGID = s.UIDRESCFGID
            WHERE s.UIDSLOTID = @SlotId AND s.NVCHSTATUS = N'OPEN'
              AND s.DTMSTARTAT > SYSUTCDATETIME()
              AND @Capacity > s.INTCAPACITY  --新名額要大於目前名額
              AND @Capacity <= c.INTMAXUSER; --新名額不能超過該版本的上限
            """;
        return await unitOfWork.Connection.ExecuteAsync(Command(sql, new { SlotId = slotId, Capacity = capacity, Actor = actor }, ct)) == 1;
    }

    public async Task<bool> BookAsync(Guid slotId, Guid userId, string actor, CancellationToken ct)
    {
        var parameters = new { SlotId = slotId, UserId = userId, Actor = actor };
        const string insert = """
            INSERT INTO dbo.ERSSSCHTBL (UIDSLOTID, UIDUSERID, UIDIDNTYPE, NUMFEE, VCHCREATEBY)
            SELECT s.UIDSLOTID, u.UIDUSERID, u.UIDIDNTYPE, fee.NUMFEE, @Actor
            FROM dbo.ERSSSLOTTBL AS s
            JOIN dbo.ERSSUSERRES AS access ON access.UIDRESID = s.UIDRESID AND access.UIDUSERID = @UserId
            JOIN dbo.ERSSUSERTBL AS u ON u.UIDUSERID = access.UIDUSERID
            JOIN dbo.ERSSRESFEETBL AS fee ON fee.UIDRESCFGID = s.UIDRESCFGID AND fee.UIDIDNTYPE = u.UIDIDNTYPE
            WHERE s.UIDSLOTID = @SlotId AND s.NVCHSTATUS = N'OPEN'
              AND s.DTMSTARTAT > SYSUTCDATETIME()
              AND access.DTMEFFECTSTART <= s.DTMSTARTAT
              AND (access.DTMEFFECTEND IS NULL OR access.DTMEFFECTEND >= s.DTMENDAT)
              AND s.INTCAPACITY > (
                  SELECT COUNT(*) FROM dbo.ERSSSCHTBL AS b
                  WHERE b.UIDSLOTID = @SlotId AND b.NVCHSTATUS = N'BOOKED'
              )
              AND NOT EXISTS (
                  SELECT 1
                  FROM dbo.ERSSSCHTBL AS b
                  JOIN dbo.ERSSSLOTTBL AS booked ON booked.UIDSLOTID = b.UIDSLOTID
                  WHERE b.UIDUSERID = @UserId AND b.NVCHSTATUS = N'BOOKED'
                    AND booked.DTMSTARTAT < s.DTMENDAT AND booked.DTMENDAT > s.DTMSTARTAT
              );
            """;
        return await unitOfWork.Connection.ExecuteAsync(Command(insert, parameters, ct)) == 1;
    }
    public async Task<bool> CancelBookingAsync(Guid slotId, Guid bookingId, string actor, CancellationToken ct)
    {
        const string sql = """
            DECLARE @CancelledAt DATETIME2(0) = SYSUTCDATETIME();

            UPDATE b
            SET NVCHSTATUS = N'CANCELLED', DTMCANCELAT = @CancelledAt, VCHCANCELBY = @Actor,
                VCHUPDATEBY = @Actor, DTMUPDATEDAT = @CancelledAt
            FROM dbo.ERSSSCHTBL AS b
            JOIN dbo.ERSSSLOTTBL AS s ON s.UIDSLOTID = b.UIDSLOTID
            WHERE b.UIDSLOTID = @SlotId AND b.UIDSCHID = @BookingId
              AND b.NVCHSTATUS = N'BOOKED' AND s.NVCHSTATUS IN (N'OPEN', N'CLOSED');
            """;
        return await unitOfWork.Connection.ExecuteAsync(Command(sql, new { SlotId = slotId, BookingId = bookingId, Actor = actor }, ct)) == 1;
    }

    public async Task<bool> CancelSlotAsync(Guid slotId, string actor, CancellationToken ct)
    {
        const string sql = """
            UPDATE dbo.ERSSSLOTTBL
            SET NVCHSTATUS = N'CANCELLED', VCHUPDATEBY = @Actor, DTMUPDATEDAT = SYSUTCDATETIME()
            WHERE UIDSLOTID = @SlotId AND NVCHSTATUS IN (N'OPEN', N'CLOSED');

            IF @@ROWCOUNT = 0
            BEGIN
                SELECT CAST(0 AS bit);
                RETURN;
            END;

            DECLARE @CancelledAt DATETIME2(0) = SYSUTCDATETIME();

            UPDATE dbo.ERSSSCHTBL
            SET NVCHSTATUS = N'CANCELLED', DTMCANCELAT = @CancelledAt, VCHCANCELBY = @Actor,
                VCHUPDATEBY = @Actor, DTMUPDATEDAT = @CancelledAt
            WHERE UIDSLOTID = @SlotId AND NVCHSTATUS = N'BOOKED';

            SELECT CAST(1 AS bit);
            """;
        return await unitOfWork.Connection.ExecuteScalarAsync<bool>(Command(sql, new { SlotId = slotId, Actor = actor }, ct));
    }

    private sealed record SlotForUpdateRow(Guid SlotId, string Status, DateTime StartsAtUtc,
        int Capacity, int? CapacityLimit, int BookedCount, DateTime UtcNow);

    private CommandDefinition Command(string sql, object parameters, CancellationToken ct)
        => new(sql, parameters, transaction: unitOfWork.Transaction, cancellationToken: ct);
}
