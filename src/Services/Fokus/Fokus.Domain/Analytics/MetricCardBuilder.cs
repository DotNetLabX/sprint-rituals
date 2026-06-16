namespace Fokus.Domain.Analytics;

/// <summary>
/// Shared kernel for delta direction and polarity computation, extracted from 7 duplicate private helpers
/// across ScopeChangeService, CarryOverService, BugRatioService, CycleTimeService (3c-B) and
/// SprintSummaryAnalyzer, QaMetricsAnalyzer (3c-A).
///
/// Two distinct polarity paths:
///   String-polarity path (ComputeDelta / Build): used by metric-card builders in all calculators.
///     "neutral" string input always short-circuits to "neutral" polarity regardless of delta sign.
///   Bool-polarity path (Direction / Polarity): used by flat delta bundles in BugRatio + Leaderboard.
///     No "neutral-always" option — polarity is always derived from sign and positiveUp flag.
///
/// Do NOT route a caller that wants unconditional "neutral" through the bool path — it would compute
/// positive/negative from sign and silently change wire output.
/// </summary>
public static class MetricCardBuilder
{
    // --- String-polarity path ---

    /// <summary>
    /// Computes direction and polarity from a delta value and a polarity string.
    /// When delta is null, both outputs are null.
    /// When polarity is "neutral", output polarity is always "neutral" regardless of delta sign.
    /// Recognized polarity strings: "positive-up", "positive-down", anything else → "neutral".
    /// </summary>
    public static (string? Direction, string? Polarity) ComputeDelta(decimal? delta, string polarity)
    {
        if (!delta.HasValue)
            return (null, null);

        var direction = delta.Value > 0 ? "up" : delta.Value < 0 ? "down" : "flat";
        var deltaPolarity = polarity switch
        {
            "positive-up"   => delta.Value > 0 ? "positive" : delta.Value < 0 ? "negative" : "neutral",
            "positive-down" => delta.Value < 0 ? "positive" : delta.Value > 0 ? "negative" : "neutral",
            _               => "neutral"
        };

        return (direction, deltaPolarity);
    }

    /// <summary>
    /// Convenience builder for the 4 card-producing 3c-B calculators.
    /// Rounds value and delta to 1 dp, fills direction/polarity via ComputeDelta.
    /// </summary>
    public static MetricCardData Build(
        string name,
        decimal value,
        string displayValue,
        decimal? delta,
        string polarity)
    {
        var (direction, deltaPolarity) = ComputeDelta(delta, polarity);
        return new MetricCardData(
            name,
            Math.Round(value, 1),
            displayValue,
            delta.HasValue ? Math.Round(delta.Value, 1) : null,
            direction,
            deltaPolarity);
    }

    // --- Bool-polarity path ---

    /// <summary>
    /// Derives direction from a decimal delta. Used by flat delta bundles (BugRatio, Leaderboard).
    /// </summary>
    public static string Direction(decimal delta) =>
        delta > 0 ? "up" : delta < 0 ? "down" : "flat";

    /// <summary>
    /// Derives direction from an int delta. Overload for int callers (BugRatio, Leaderboard).
    /// </summary>
    public static string Direction(int delta) =>
        delta > 0 ? "up" : delta < 0 ? "down" : "flat";

    /// <summary>
    /// Derives polarity from a decimal delta and a positiveUp flag. Used by flat delta bundles.
    /// positiveUp=true  → up=positive / down=negative / flat=neutral
    /// positiveUp=false → down=positive / up=negative / flat=neutral
    /// This path never returns unconditional "neutral" — do NOT use for fields that need "neutral" always.
    /// </summary>
    public static string Polarity(decimal delta, bool positiveUp) =>
        positiveUp
            ? (delta > 0 ? "positive" : delta < 0 ? "negative" : "neutral")
            : (delta < 0 ? "positive" : delta > 0 ? "negative" : "neutral");

    /// <summary>
    /// Derives polarity from an int delta and a positiveUp flag. Overload for int callers.
    /// </summary>
    public static string Polarity(int delta, bool positiveUp) =>
        positiveUp
            ? (delta > 0 ? "positive" : delta < 0 ? "negative" : "neutral")
            : (delta < 0 ? "positive" : delta > 0 ? "negative" : "neutral");
}
