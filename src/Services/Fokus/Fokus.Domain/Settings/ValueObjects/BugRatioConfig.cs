namespace Fokus.Domain;

public class BugRatioConfig : ValueObject
{
    public int AlertThreshold { get; set; } = 50;
    public int ConsecutiveSprintCount { get; set; } = 2;
    public decimal Target { get; set; } = 30;
    public int DefaultSpPerBug { get; set; } = 3;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return AlertThreshold;
        yield return ConsecutiveSprintCount;
        yield return Target;
        yield return DefaultSpPerBug;
    }
}
