namespace Fokus.Domain;

public class HealthWeightConfig : ValueObject
{
    public int Completion { get; set; } = 40;
    public int Disruption { get; set; } = 30;
    public int CarryOver { get; set; } = 30;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Completion;
        yield return Disruption;
        yield return CarryOver;
    }
}
