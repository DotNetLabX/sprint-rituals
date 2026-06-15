namespace Reflekt.Domain.RetroSessions;

public sealed class ColumnColor
{
    public static readonly IReadOnlyCollection<string> AllowedColors =
        ["green", "red", "amber", "blue", "purple", "teal", "pink", "slate"];

    public string Value { get; }

    private ColumnColor(string value) => Value = value;

    public static ColumnColor Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var trimmed = value.Trim().ToLowerInvariant();
        if (!AllowedColors.Contains(trimmed))
            throw new ArgumentException($"'{value}' is not a valid column color. Allowed: {string.Join(", ", AllowedColors)}.");
        return new ColumnColor(trimmed);
    }

    public static bool IsValid(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        return AllowedColors.Contains(value.Trim().ToLowerInvariant());
    }

    public override string ToString() => Value;
    public override bool Equals(object? obj) => obj is ColumnColor other && Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
}
