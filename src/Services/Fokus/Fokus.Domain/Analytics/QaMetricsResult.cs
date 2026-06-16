namespace Fokus.Domain.Analytics;

// --- QA metrics domain result records ---

public sealed record QaMetricsData(
    bool HasQaData,
    QaMetricCardData CoverageRate,
    QaMetricCardData ExecutionRate,
    QaMetricCardData PassRate,
    int BugsFound,
    decimal QualitySubScore,
    QaBreakdownData QualityBreakdown,
    int UntestedCount,
    int FailingCount);

public sealed record QaMetricCardData(
    string Name, decimal Value, string DisplayValue,
    decimal? Delta, string? DeltaDirection, string? DeltaPolarity,
    IReadOnlyList<QaSparklinePoint> Sparkline)
{
    public string? Rag { get; init; }
}

public sealed record QaSparklinePoint(string SprintName, decimal Value);

/// <summary>
/// Quality breakdown — shared domain type used by both QaMetricsAnalyzer and SprintSummaryAnalyzer.
/// Defined once here; SprintSummaryResult.cs references this type without redefining it.
/// </summary>
public sealed record QaBreakdownData(
    decimal CoverageScore,
    int CoverageWeight,
    decimal PassRateScore,
    int PassRateWeight);
