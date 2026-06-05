namespace Fokus.Domain;

public class QualitySubScoreWeightConfig : ValueObject
{
    public int CoverageWeight { get; set; } = 50;
    public int PassRateWeight { get; set; } = 50;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return CoverageWeight;
        yield return PassRateWeight;
    }
}
