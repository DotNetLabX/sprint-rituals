namespace Fokus.Domain;

public class SyncConfig : ValueObject
{
    public int BackSprintCount { get; set; } = 20;
    public int PlanningWindowDays { get; set; } = 2;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return BackSprintCount;
        yield return PlanningWindowDays;
    }
}
