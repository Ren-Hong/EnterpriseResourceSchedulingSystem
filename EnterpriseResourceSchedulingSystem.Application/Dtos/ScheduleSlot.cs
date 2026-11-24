namespace EnterpriseResourceSchedulingSystem.Application.Dtos;

public sealed record ScheduleSlot(Guid SlotId, Guid ResourceId, string ResourceName, int? Version,
    DateTime Start, DateTime End, int Capacity, int Booked, string Status, string? Note,
    DateTime? EffectiveStart, DateTime? EffectiveEnd, int? MaxUsers, Guid? ResourceConfigId)
{
    public IReadOnlyList<ScheduleSlotBooking> Bookings { get; init; } = [];
}

public sealed record ScheduleSlotBooking(string DisplayName, string? IdentityTypeName, decimal Fee);

public sealed record ScheduleVersion(Guid ResourceConfigId, int Version, int MaxUsers, string? Description,
    DateTime EffectiveStart, DateTime EffectiveEnd);

public sealed record CreateScheduleSlot(Guid ResourceId, Guid? ResourceConfigId, DateOnly Date,
    TimeOnly Start, TimeOnly End, int? Capacity, string? Note);

public sealed record ScheduleBooking(Guid BookingId, string DisplayName, string? IdentityTypeName);

public sealed record ScheduleBookingChange(string DisplayName, string Action, DateTimeOffset OccurredAt, string? Actor);
