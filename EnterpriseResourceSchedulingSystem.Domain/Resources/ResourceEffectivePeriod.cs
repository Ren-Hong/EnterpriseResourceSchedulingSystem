namespace EnterpriseResourceSchedulingSystem.Domain.Resources;

public sealed record ResourceEffectivePeriod
{
    public DateOnly Start { get; }
    public DateOnly End { get; }

    public ResourceEffectivePeriod(DateOnly start, DateOnly end)
    {
        if (start == default || end == default)
            throw new ArgumentException("請填寫版本生效起日與迄日。", nameof(start));
        if (end < start)
            throw new ArgumentException("新版本生效迄日不可早於生效起日。", nameof(end));

        Start = start;
        End = end;
    }
}
