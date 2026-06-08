namespace Fokus.Domain.Analytics;

// --- ScopeChange domain result records ---
// Wire records live in Fokus.API/Features/Analytics/GetScopeChange/GetScopeChangeResponse.cs

public sealed record ScopeChangeSprintData(int Id, string Name, DateTime StartDate, DateTime EndDate);

public sealed record ScopeChangeSummaryData(
    decimal AverageDisruptionRate,
    decimal? AverageDisruptionRateDelta,
    string? AverageDisruptionRateDeltaDirection,
    decimal AverageNetScopeChange,
    int TotalBugsAdded);

public sealed record ScopeChangePerSprintMetricsData(
    int SprintId,
    decimal CommittedSpActive,
    decimal CommittedSpTotal,
    decimal AddedSp,
    decimal RemovedSp,
    decimal NetScopeChange,
    decimal CompletedSp,
    decimal DisruptionRate,
    int BugCount,
    decimal BugSpCompleted);

public sealed record ClassificationData(
    string Category,
    int TicketCount,
    decimal? SpTotal,
    decimal Percentage);

public sealed record ScopeChangeMultiSprintData(
    List<ScopeChangeSprintData> Sprints,
    ScopeChangeSummaryData SummaryMetrics,
    List<ScopeChangePerSprintMetricsData> PerSprintData,
    List<ClassificationData> ClassificationBreakdown);

// Single-sprint metric cards (MetricCardData, not ScopeMetricCard — that's wire-only)
public sealed record ScopeChangeSingleSprintMetricsData(
    MetricCardData CommittedSpActive,
    MetricCardData CommittedSpTotal,
    MetricCardData AddedSp,
    MetricCardData RemovedSp,
    MetricCardData NetScopeChange,
    MetricCardData DisruptionRate,
    MetricCardData BugCount);

public sealed record BurnupDataPointData(
    int DayNumber,
    DateTime Date,
    decimal TotalScopeSp,
    decimal CompletedSp,
    string Phase,
    decimal BugSp,
    int TotalScopeTickets,
    int CompletedTickets,
    int BugTickets,
    decimal CommittedTotalSp,
    int CommittedTotalTickets);

public sealed record ScopeChangeEventData(
    DateTime Date,
    int SprintDayNumber,
    string TicketKey,
    string TicketSummary,
    decimal? StoryPoints,
    string IssueType,
    string Action,
    string? Category,
    bool IsExcluded);

public sealed record BugTimeInProgressData(
    string TicketKey,
    string TicketSummary,
    decimal TimeInActiveDays,
    string CurrentStatus);

public sealed record ScopeChangeSingleSprintData(
    ScopeChangeSprintData Sprint,
    ScopeChangeSingleSprintMetricsData Metrics,
    List<BurnupDataPointData> BurnupData,
    List<ClassificationData> ClassificationBreakdown,
    List<ScopeChangeEventData> Events,
    List<BugTimeInProgressData> BugTimeInProgress);
