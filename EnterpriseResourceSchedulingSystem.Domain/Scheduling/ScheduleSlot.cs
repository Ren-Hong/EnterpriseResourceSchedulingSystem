namespace EnterpriseResourceSchedulingSystem.Domain.Scheduling;

public sealed class ScheduleSlot(
    Guid id,
    ScheduleSlotStatus status,
    DateTime startsAtUtc,
    int capacity,
    int bookedCount,
    int? capacityLimit)
{
    public Guid Id { get; } = id;
    public ScheduleSlotStatus Status { get; private set; } = status;
    public DateTime StartsAtUtc { get; } = startsAtUtc;
    public int Capacity { get; private set; } = capacity;
    public int BookedCount { get; } = bookedCount;
    public int? CapacityLimit { get; } = capacityLimit;

    public static bool TryResolveInitialCapacity(int? requestedCapacity, int capacityLimit, out int capacity)
    {
        capacity = requestedCapacity ?? capacityLimit;
        return capacity > 0 && capacity <= capacityLimit;
    }

    public bool TryIncreaseCapacity(int newCapacity, DateTime utcNow)
    {
        if (!IsOpenAndUpcoming(utcNow) || newCapacity <= Capacity || CapacityLimit is not { } limit || newCapacity > limit)
            return false;

        Capacity = newCapacity;
        return true;
    }

    public bool CanAcceptBooking(DateTime utcNow)
        => IsOpenAndUpcoming(utcNow) && BookedCount < Capacity;

    public bool AllowsBookingCancellation()
        => Status is ScheduleSlotStatus.Open or ScheduleSlotStatus.Closed;

    public bool TryCancel()
    {
        if (!AllowsBookingCancellation()) return false;

        Status = ScheduleSlotStatus.Cancelled;
        return true;
    }

    private bool IsOpenAndUpcoming(DateTime utcNow)
        => Status == ScheduleSlotStatus.Open && StartsAtUtc > utcNow;
}
