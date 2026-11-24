using System.Diagnostics.CodeAnalysis;

namespace EnterpriseResourceSchedulingSystem.Domain.Scheduling;

public sealed record SlotTimeRange
{
    public DateOnly Date { get; }
    public TimeOnly Start { get; }
    public TimeOnly End { get; }

    private SlotTimeRange(DateOnly date, TimeOnly start, TimeOnly end)
    {
        Date = date;
        Start = start;
        End = end;
    }

    public static bool TryCreate(DateOnly date, TimeOnly start, TimeOnly end, [NotNullWhen(true)] out SlotTimeRange? range)
    {
        range = null;
        if (date.Year < 1900 || end <= start ||
            start.Ticks % TimeSpan.TicksPerMinute != 0 || end.Ticks % TimeSpan.TicksPerMinute != 0)
            return false;

        range = new SlotTimeRange(date, start, end);
        return true;
    }
}
