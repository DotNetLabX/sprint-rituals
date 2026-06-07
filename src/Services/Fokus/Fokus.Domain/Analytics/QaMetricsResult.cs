namespace Fokus.Domain.Analytics;

// --- QA metrics domain result records ---

public record QaMetricsData(
    bool HasQaData,
    QaMetricCardData CoverageRate,
    QaMetricCardData ExecutionRate,
    QaMetricCardData PassRate,
    int BugsFound,
    decimal QualitySubScore,
    QaBreakdownData QualityBreakdown,
    int UntestedCount,
    int FailingCount);

public record QaMetricCardData(
    string Name, decimal Value, string DisplayValue,
    decimal? Delta, string? DeltaDirection, string? DeltaPolarity,
    IReadOnlyList<QaSparklinePoint> Sparkline)
{
    public string? Rag { get; init; }
}

public record QaSparklinePoint(string SprintName, decimal Value);

/// <summary>
/// Quality breakdown — shared domain type used by both QaMetricsCalculator and SprintSummaryCalculator.
/// Defined once here; SprintSummaryResult.cs references this type without redefining it.
/// </summary>
public record QaBreakdownData(
    decimal CoverageScore,
    int CoverageWeight,
    decimal PassRateScore,
    int PassRateWeight);
