namespace Blocks.Domain.ValueObjects;

public abstract class StringValueObject : ValueObject
{
    public string Value { get; protected set; } = string.Empty;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static bool operator ==(StringValueObject? left, StringValueObject? right)
        => left?.Equals(right) ?? right is null;

    public static bool operator !=(StringValueObject? left, StringValueObject? right)
        => !(left == right);

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType()) return false;
        return Value == ((StringValueObject)obj).Value;
    }

    public override int GetHashCode() => HashCode.Combine(GetType(), Value);
}
