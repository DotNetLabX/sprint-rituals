namespace Fokus.Domain.Analytics;

// --- BugRatio domain result records ---
// Wire records live in Fokus.API/Features/Analytics/GetBugRatio/GetBugRatioResponse.cs

public sealed record BugRatioSprintData(int Id, string Name, DateTime StartDate, DateTime EndDate);

public sealed record BugRatioTeamTrendData(
    int SprintId,
    decimal BugRatioPercent,
    decimal BugSp,
    decimal NonBugSp,
    decimal CompletedSp);

public sealed record BugRatioTeamMetricsData(
    decimal BugRatioPercent,
    decimal TotalBugSp,
    decimal TotalNonBugSp,
    decimal TotalCompletedSp,
    List<BugRatioTeamTrendData> PerSprintTrend);

public sealed record BugRatioIssueTypeData(
    string IssueType,
    int TicketCount,
    decimal SpTotal);

public sealed record BugRatioAlertData(bool IsActive, int ConsecutiveSprintCount, int ThresholdPercent);

public sealed record BugRatioDeveloperSprintData(
    int SprintId,
    decimal BugSp,
    decimal NonBugSp,
    decimal CompletedSp,
    decimal BugRatioPercent,
    int BugTicketCount,
    int NonBugTicketCount);

public sealed record BugRatioDeveloperData(
    string AccountId,
    string DisplayName,
    string? SubTeam,
    string? AvatarUrl,
    decimal BugSp,
    decimal NonBugSp,
    decimal CompletedSp,
    decimal BugRatioPercent,
    int BugTicketCount,
    int NonBugTicketCount,
    List<BugRatioDeveloperSprintData> SprintBreakdowns,
    BugRatioAlertData Alert);

public sealed record BugRatioMultiSprintData(
    List<BugRatioSprintData> Sprints,
    BugRatioTeamMetricsData TeamMetrics,
    List<BugRatioIssueTypeData> IssueTypeBreakdown,
    List<BugRatioDeveloperData> Developers);

// Single-sprint domain records

// MetricCardData (shared) replaces BugRatioMetricCard in domain types.

public sealed record BugRatioTeamSingleData(
    MetricCardData BugRatioPercent,
    MetricCardData BugSp,
    MetricCardData NonBugSp);

/// <summary>
/// Flat delta bundle — NOT a metric card. Carries pre-computed direction/polarity string fields.
/// All 10 direction/polarity fields are genuine MetricCardBuilder.Direction/Polarity calls (no hard-coded literals).
/// </summary>
public sealed record BugRatioDeveloperDeltaData(
    decimal BugSpDelta,
    string BugSpDeltaDirection,
    string BugSpDeltaPolarity,
    decimal NonBugSpDelta,
    string NonBugSpDeltaDirection,
    string NonBugSpDeltaPolarity,
    decimal BugRatioPercentDelta,
    string BugRatioPercentDeltaDirection,
    string BugRatioPercentDeltaPolarity,
    int BugTicketCountDelta,
    string BugTicketCountDeltaDirection,
    string BugTicketCountDeltaPolarity,
    int NonBugTicketCountDelta,
    string NonBugTicketCountDeltaDirection,
    string NonBugTicketCountDeltaPolarity);

public sealed record BugRatioDeveloperSingleData(
    string AccountId,
    string DisplayName,
    string? SubTeam,
    string? AvatarUrl,
    decimal BugSp,
    decimal NonBugSp,
    decimal CompletedSp,
    decimal BugRatioPercent,
    int BugTicketCount,
    int NonBugTicketCount,
    BugRatioDeveloperDeltaData? Delta,
    BugRatioAlertData Alert);

public sealed record BugRatioSingleSprintData(
    BugRatioSprintData Sprint,
    BugRatioTeamSingleData TeamMetrics,
    List<BugRatioIssueTypeData> IssueTypeBreakdown,
    List<BugRatioDeveloperSingleData> Developers);
