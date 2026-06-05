namespace Fokus.Domain;

public class HealthThresholdConfig : ValueObject
{
    public decimal CompletionGreen { get; set; } = 80;
    public decimal CompletionAmber { get; set; } = 60;
    public decimal DisruptionGreen { get; set; } = 10;
    public decimal DisruptionAmber { get; set; } = 25;
    public decimal CarryOverGreen { get; set; } = 10;
    public decimal CarryOverAmber { get; set; } = 25;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return CompletionGreen;
        yield return CompletionAmber;
        yield return DisruptionGreen;
        yield return DisruptionAmber;
        yield return CarryOverGreen;
        yield return CarryOverAmber;
    }
}
