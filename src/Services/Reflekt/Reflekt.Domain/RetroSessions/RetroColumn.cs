using Blocks.Domain.ValueObjects;

namespace Reflekt.Domain.RetroSessions;

public sealed class RetroColumn : ValueObject
{
    public string Name { get; private set; } = default!;
    public ColumnColor Color { get; private set; } = default!;

    // Parameterless ctor required by EF Core ComplexProperty materialization
    private RetroColumn() { }

    private RetroColumn(string name, ColumnColor color)
    {
        Name = name;
        Color = color;
    }

    /// <summary>Creates a validated column. Validates name (non-empty, ≤30 chars, trimmed) and color palette.</summary>
    public static RetroColumn Create(string name, string color)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed))
            throw new ArgumentException("Column name cannot be empty or whitespace.", nameof(name));
        if (trimmed.Length > 30)
            throw new ArgumentException("Column name cannot exceed 30 characters.", nameof(name));

        return new RetroColumn(trimmed, ColumnColor.Create(color));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Name;
        yield return Color;
    }
}
