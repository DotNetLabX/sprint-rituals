namespace Blocks.Domain.ValueObjects;

public abstract class SingleValueObject<T> : ValueObject where T : struct
{
    public T Value { get; protected set; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType()) return false;
        return Value.Equals(((SingleValueObject<T>)obj).Value);
    }

    public override int GetHashCode() => HashCode.Combine(GetType(), Value);

    public override string ToString() => Value.ToString() ?? string.Empty;
}
