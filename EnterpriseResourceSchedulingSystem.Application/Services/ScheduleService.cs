using EnterpriseResourceSchedulingSystem.Application.Abstractions.Persistence;
using EnterpriseResourceSchedulingSystem.Application.Abstractions.Repositories;
using EnterpriseResourceSchedulingSystem.Application.Dtos;

using SlotTimeRange = EnterpriseResourceSchedulingSystem.Domain.Scheduling.SlotTimeRange;
using Slot = EnterpriseResourceSchedulingSystem.Domain.Scheduling.ScheduleSlot;

namespace EnterpriseResourceSchedulingSystem.Application.Services;

public sealed class ScheduleService(IScheduleRepository schedules, IResourceConfigRepository resourceConfigs, IUnitOfWork unitOfWork)
{
    private static readonly TimeSpan TaipeiOffset = TimeSpan.FromHours(8);

    public async Task<IReadOnlyList<ScheduleSlot>> GetSlotsAsync(DateOnly from, DateOnly to, Guid[] resources, Guid[] users, bool includeCancelled, bool includeUnbooked, CancellationToken ct)
    {
        if (from.Year < 1900 || to <= from || to.DayNumber - from.DayNumber > 62)
            throw new ArgumentException("排程查詢範圍須介於 1 至 62 天。");

        var slots = await schedules.GetSlotsAsync(ToUtc(from, TimeOnly.MinValue), ToUtc(to, TimeOnly.MinValue), resources, users, includeCancelled, includeUnbooked, ct);
        return slots.Select(slot => slot with { Start = ToTaipei(slot.Start), End = ToTaipei(slot.End) }).ToArray();
    }

    public Task<IReadOnlyList<ScheduleUserOption>> GetPermittedUsersAsync(Guid slotId, CancellationToken ct)
        => schedules.GetPermittedUsersAsync(slotId, ct);

    public Task<IReadOnlyList<ScheduleBooking>> GetBookingsAsync(Guid slotId, CancellationToken ct)
        => schedules.GetBookingsAsync(slotId, ct);

    public Task<IReadOnlyList<ScheduleBookingChange>> GetBookingChangesAsync(Guid slotId, CancellationToken ct)
        => schedules.GetBookingChangesAsync(slotId, ct);

    public Task<IReadOnlyList<ScheduleVersion>> GetVersionsAsync(Guid resourceId, DateOnly date, CancellationToken ct)
        => resourceConfigs.GetEffectiveVersionsAsync(resourceId, date, ct);

    public Task<Guid> CreateSlotAsync(CreateScheduleSlot request, string actor, CancellationToken ct)
    {
        if (request.ResourceId == Guid.Empty || !SlotTimeRange.TryCreate(request.Date, request.Start, request.End, out var range))
            throw new ArgumentException("請選擇設備和日期，並確認結束時間晚於開始時間，時間以分鐘為單位。");

        if (request.Capacity < 1 || request.Note?.Length > 500)
            throw new ArgumentException("名額須大於零，備註最多 500 字。");

        var start = ToUtc(range.Date, range.Start);
        var end = ToUtc(range.Date, range.End);
        return unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            if (!await schedules.LockResourceForSlotCreationAsync(request.ResourceId, token))
                throw new KeyNotFoundException("找不到設備資源。");

            var versions = await GetVersionsAsync(request.ResourceId, range.Date, token);
            var version = request.ResourceConfigId is { } id
                ? versions.SingleOrDefault(item => item.ResourceConfigId == id)
                : versions.Count == 1 ? versions[0] : null;
            if (version is null)
                throw new ScheduleConflictException(versions.Count == 0 ? "該日期沒有適用版本。" : "請選擇該日期適用的設備版本。");
            if (!Slot.TryResolveInitialCapacity(request.Capacity, version.MaxUsers, out var capacity))
                throw new ScheduleConflictException($"名額不可超過所選版本的上限（{version.MaxUsers} 人）。");
            if (await schedules.HasOverlapAsync(request.ResourceId, start, end, token))
                throw new ScheduleConflictException("此設備在該時間已有時段。");

            return await schedules.CreateSlotAsync(request.ResourceId, version.ResourceConfigId, capacity, start, end, request.Note, actor, token);
        }, ct);
    }

    public Task<bool> IncreaseCapacityAsync(Guid slotId, int capacity, string actor, CancellationToken ct)
        => unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var state = await schedules.GetSlotForUpdateAsync(slotId, token);
            if (state is not { } current || !current.Slot.TryIncreaseCapacity(capacity, current.UtcNow))
                return false;

            return await schedules.IncreaseCapacityAsync(current.Slot.Id, current.Slot.Capacity, actor, token);
        }, ct);

    public Task<bool> BookAsync(Guid slotId, Guid userId, string actor, CancellationToken ct)
        => unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            if (!await schedules.LockBookingUserAsync(userId, token)) return false;

            var state = await schedules.GetSlotForUpdateAsync(slotId, token);
            if (state is not { } current || !current.Slot.CanAcceptBooking(current.UtcNow))
                return false;

            return await schedules.BookAsync(current.Slot.Id, userId, actor, token);
        }, ct);

    public Task<bool> CancelBookingAsync(Guid slotId, Guid bookingId, string actor, CancellationToken ct)
        => unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var state = await schedules.GetSlotForUpdateAsync(slotId, token);
            if (state is not { } current || !current.Slot.AllowsBookingCancellation())
                return false;

            return await schedules.CancelBookingAsync(current.Slot.Id, bookingId, actor, token);
        }, ct);

    public Task<bool> CancelSlotAsync(Guid slotId, string actor, CancellationToken ct)
        => unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var state = await schedules.GetSlotForUpdateAsync(slotId, token);
            if (state is not { } current || !current.Slot.TryCancel())
                return false;

            return await schedules.CancelSlotAsync(current.Slot.Id, actor, token);
        }, ct);

    private static DateTime ToUtc(DateOnly date, TimeOnly time)
        => new DateTimeOffset(date.ToDateTime(time), TaipeiOffset).UtcDateTime;

    private static DateTime ToTaipei(DateTime utc)
        => DateTime.SpecifyKind(utc, DateTimeKind.Unspecified).Add(TaipeiOffset);
}

public sealed class ScheduleConflictException(string message) : Exception(message);
