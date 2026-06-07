namespace Fokus.Domain;

public class CycleTimeConfig : ValueObject
{
    public string? StartStage { get; set; } = null;
    public string? EndStage { get; set; } = null;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return StartStage;
        yield return EndStage;
    }
}
