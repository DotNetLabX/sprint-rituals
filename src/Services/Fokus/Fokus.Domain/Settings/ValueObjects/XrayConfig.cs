namespace Fokus.Domain;

public class XrayConfig : ValueObject
{
    public bool Enabled { get; set; } = false;
    public string? ClientId { get; set; } = null;
    public string? ClientSecret { get; set; } = null;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Enabled;
        yield return ClientId;
        yield return ClientSecret;
    }
}
