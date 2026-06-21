namespace Poker.Domain.Planning;

/// <summary>
/// Fixed Fibonacci estimation scale (Rule 5). "?" is the abstain value — it can never
/// be the final estimate and never triggers an automatic skip (Rules 6, 9).
/// </summary>
public static class FibonacciScale
{
    public const string Abstain = "?";

    public static readonly IReadOnlySet<string> Values =
        new HashSet<string>(StringComparer.Ordinal) { "1", "2", "3", "5", "8", "13", "21", Abstain };

    /// <summary>Returns true when <paramref name="value"/> is a valid numeric estimate (not "?").</summary>
    public static bool IsNumeric(string value) => Values.Contains(value) && value != Abstain;
}
