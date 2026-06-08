namespace Fokus.Domain.Analytics;

// Domain result records for QaWorkloadAnalyzer.
// QaWorkloadSparklinePointData is a local domain sparkline point — NOT reused from other calculators
// (3c-A pattern: each calculator owns its own domain sparkline type).

public sealed record QaWorkloadSparklinePointData(string SprintName, decimal Value);

// QaWorkload-specific metric card — carries Sparkline + Rag fields not present on shared MetricCardData.
public sealed record QaWorkloadMetricCardData(
    string Name,
    decimal Value,
    string DisplayValue,
    decimal? Delta,
    string? DeltaDirection,
    string? DeltaPolarity,
    List<QaWorkloadSparklinePointData> Sparkline,
    string? Rag = null);

// Multi-sprint domain records

public sealed record QaWorkloadTeamMetricsData(
    int TotalTes,
    int TotalRunsCompleted,
    int PassCount,
    int FailCount,
    decimal TeamPassRate);

public sealed record QaWorkloadSprintBreakdownData(
    int SprintId,
    string SprintName,
    int TesOwned,
    int RunsCompleted,
    int PassCount,
    int FailCount,
    decimal PassRate,
    int StoriesCovered,
    int BugsFound);

public sealed record WorkloadAlertData(bool IsActive, int ConsecutiveSprintCount, int ThresholdPercent);

public sealed record QaWorkloadEntryData(
    string? AccountId,
    string DisplayName,
    string? SubTeam,
    string? AvatarUrl,
    int TesOwned,
    int RunsCompleted,
    int PassCount,
    int FailCount,
    decimal PassRate,
    int StoriesCovered,
    int BugsFound,
    List<QaWorkloadSprintBreakdownData> SprintBreakdowns,
    WorkloadAlertData WorkloadAlert);

public sealed record QaWorkloadMultiSprintResultData(
    List<SprintSummaryItemData> Sprints,
    QaWorkloadTeamMetricsData TeamMetrics,
    List<QaWorkloadEntryData> Developers);

// Single-sprint domain records

public sealed record QaWorkloadSingleTeamMetricsData(
    QaWorkloadMetricCardData TotalTes,
    QaWorkloadMetricCardData TotalRunsCompleted,
    QaWorkloadMetricCardData TeamPassRate);

public sealed record QaWorkloadSingleEntryData(
    string? AccountId,
    string DisplayName,
    string? SubTeam,
    string? AvatarUrl,
    int TesOwned,
    int RunsCompleted,
    int PassCount,
    int FailCount,
    decimal PassRate,
    int StoriesCovered,
    int BugsFound,
    decimal? TesOwnedDelta,
    string? TesOwnedDirection,
    string? TesOwnedPolarity,
    decimal? RunsCompletedDelta,
    string? RunsCompletedDirection,
    string? RunsCompletedPolarity,
    decimal? PassCountDelta,
    string? PassCountDirection,
    string? PassCountPolarity,
    decimal? FailCountDelta,
    string? FailCountDirection,
    string? FailCountPolarity,
    decimal? PassRateDelta,
    string? PassRateDirection,
    string? PassRatePolarity,
    decimal? StoriesCoveredDelta,
    string? StoriesCoveredDirection,
    string? StoriesCoveredPolarity,
    decimal? BugsFoundDelta,
    string? BugsFoundDirection,
    string? BugsFoundPolarity,
    WorkloadAlertData WorkloadAlert);

public sealed record QaWorkloadSingleSprintResultData(
    SprintSummaryItemData Sprint,
    QaWorkloadSingleTeamMetricsData TeamMetrics,
    List<QaWorkloadSingleEntryData> Developers);
