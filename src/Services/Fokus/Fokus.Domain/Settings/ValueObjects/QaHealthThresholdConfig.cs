namespace Fokus.Domain;

public class QaHealthThresholdConfig : ValueObject
{
    public decimal CoverageGreen { get; set; } = 80;
    public decimal CoverageAmber { get; set; } = 50;
    public decimal ExecutionGreen { get; set; } = 80;
    public decimal ExecutionAmber { get; set; } = 50;
    public decimal PassRateGreen { get; set; } = 90;
    public decimal PassRateAmber { get; set; } = 70;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return CoverageGreen;
        yield return CoverageAmber;
        yield return ExecutionGreen;
        yield return ExecutionAmber;
        yield return PassRateGreen;
        yield return PassRateAmber;
    }
}
