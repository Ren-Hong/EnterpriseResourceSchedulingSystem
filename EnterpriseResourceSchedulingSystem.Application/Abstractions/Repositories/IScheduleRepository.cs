using EnterpriseResourceSchedulingSystem.Application.Dtos;

namespace EnterpriseResourceSchedulingSystem.Application.Abstractions.Repositories;

public interface IScheduleRepository
{
    Task<IReadOnlyList<ScheduleSlot>> GetSlotsAsync(DateTime fromUtc, DateTime toUtc, Guid[] resources, Guid[] users, bool includeCancelled, CancellationToken ct);
    Task<IReadOnlyList<ScheduleUserOption>> GetPermittedUsersAsync(Guid slotId, CancellationToken ct);
    Task<IReadOnlyList<ScheduleBooking>> GetBookingsAsync(Guid slotId, CancellationToken ct);
    Task<IReadOnlyList<ScheduleVersion>> GetVersionsAsync(Guid resourceId, DateOnly date, CancellationToken ct);
    Task<bool> LockResourceAsync(Guid resourceId, CancellationToken ct);
    Task<bool> HasOverlapAsync(Guid resourceId, DateTime startUtc, DateTime endUtc, CancellationToken ct);
    Task<Guid> CreateSlotAsync(CreateScheduleSlot request, Guid configId, int capacity, DateTime startUtc, DateTime endUtc, string actor, CancellationToken ct);
    Task<bool> IncreaseCapacityAsync(Guid slotId, int capacity, string actor, CancellationToken ct);
    Task<bool> BookAsync(Guid slotId, Guid userId, string actor, CancellationToken ct);
    Task<bool> CancelBookingAsync(Guid slotId, Guid bookingId, CancellationToken ct);
    Task<bool> CancelSlotAsync(Guid slotId, string actor, CancellationToken ct);
}
