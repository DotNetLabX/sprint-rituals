# Create Value Object

## Choose Base Type

| Base | When | Example |
|------|------|---------|
| `StringValueObject` | Single string value (email, name, URL) | `EmailAddress`, `{ValueObjectName}` |
| `SingleValueObject<T>` | Single struct value (int, decimal, Guid) | `Money`, `Quantity` |
| `ValueObject` | Multiple properties | `Address`, `DateRange` |

## Pattern: StringValueObject

**Reference:** `src/BuildingBlocks/Blocks.Domain/ValueObjects/StringValueObject.cs`

```csharp
public class {Name} : StringValueObject
{
    internal {Name}(string value) { Value = value; }

    public static {Name} Create(string value)
    {
        Guard.ThrowIfNullOrWhiteSpace(value);
        // Additional validation...
        return new {Name}(value);
    }
}
```

## Pattern: SingleValueObject<T>

```csharp
public class {Name} : SingleValueObject<decimal>
{
    internal {Name}(decimal value) { Value = value; }

    public static {Name} Create(decimal value)
    {
        if (value < 0) throw new DomainException("{Name} cannot be negative");
        return new {Name}(value);
    }
}
```

## Pattern: ValueObject (Multi-Property)

**Reference:** `src/BuildingBlocks/Blocks.Domain/ValueObjects/ValueObject.cs`

```csharp
public class {Name} : ValueObject
{
    public string Street { get; }
    public string City { get; }

    internal {Name}(string street, string city) => (Street, City) = (street, city);

    public static {Name} Create(string street, string city)
    {
        Guard.ThrowIfNullOrWhiteSpace(street);
        Guard.ThrowIfNullOrWhiteSpace(city);
        return new {Name}(street, city);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
    }
}
```

## Location

`{Service}.Domain/{AggregateName}/ValueObjects/{Name}.cs`

## Implicit Operators (Optional)

For ergonomic conversion between VO and its underlying type:

```csharp
public static implicit operator {Name}(string value) => Create(value);
public static implicit operator string({Name} vo) => vo.Value;
```

Optional — only add when the conversion is unambiguous.

## EF Core Mapping

Map value objects via `OwnsOne` in the entity configuration:

```csharp
builder.OwnsOne(e => e.Email, b =>
{
    b.Property(v => v.Value).HasColumnName("Email").HasMaxLength(MaxLength.C256);
    b.HasIndex(v => v.Value).IsUnique(); // if uniqueness required
});
```

## Per-Bounded-Context

Each service defines its own VOs even if the shape is identical. This is DDD-correct — each bounded context owns its types. Only share base classes (`StringValueObject`, etc.) through BuildingBlocks.
