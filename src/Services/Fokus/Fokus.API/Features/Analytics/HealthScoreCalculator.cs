namespace Fokus.API.Features.Analytics;

/// <summary>
/// Shared health score interpolation and RAG classification helpers.
/// Extracted from SprintSummaryService so both delivery and QA metric computation can reference them.
/// </summary>
public static class HealthScoreCalculator
{
    /// <summary>
    /// Higher is better: >= green -> 100; [amber, green) -> linear 50-99; &lt; amber -> linear 0-49.
    /// </summary>
    public static decimal ScoreHigherIsBetter(decimal value, decimal green, decimal amber)
    {
        if (value >= green) return 100;
        if (value >= amber)
        {
            var range = green - amber;
            if (range == 0) return 50;
            return 50 + (value - amber) / range * 49;
        }
        // below amber: linear 0-49, where 0=0 and amber=49
        if (amber == 0) return 0;
        return Math.Max(0, value / amber * 49);
    }

    /// <summary>
    /// Lower is better: &lt;= green -> 100; (green, amber] -> linear 99-50; > amber -> linear 49-0, hitting 0 at 2x amber.
    /// </summary>
    public static decimal ScoreLowerIsBetter(decimal value, decimal green, decimal amber)
    {
        if (value <= green) return 100;
        if (value <= amber)
        {
            var range = amber - green;
            if (range == 0) return 50;
            return 99 - (value - green) / range * 49;
        }
        // above amber: linear 49-0, where amber=49 and 2*amber=0
        var cap = amber * 2;
        if (cap <= amber) return 0;
        return Math.Max(0, 49 - (value - amber) / (cap - amber) * 49);
    }

    public static string CompositeRag(decimal score) =>
        score >= 75 ? "green" : score >= 40 ? "amber" : "red";

    public static string MetricRag(decimal value, decimal green, decimal amber, bool higherIsBetter)
    {
        if (higherIsBetter)
            return value >= green ? "green" : value >= amber ? "amber" : "red";
        else
            return value <= green ? "green" : value <= amber ? "amber" : "red";
    }
}
