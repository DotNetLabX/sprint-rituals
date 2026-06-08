namespace Fokus.Domain.Analytics;

// --- CycleTime domain result records ---
// Wire records live in Fokus.API/Features/Analytics/GetCycleTime/GetCycleTimeResponse.cs

public sealed record CycleTimeSprintData(int Id, string Name, DateTime StartDate, DateTime EndDate);

// MetricCardData (shared) is used for MetricCards collections — no per-calculator card type needed.

public sealed record StageBreakdownData(string StageName, decimal DurationDays);

public sealed record CycleTimeScatterPointData(
    string TicketKey,
    string TicketSummary,
    string IssueType,
    decimal CycleTimeDays,
    DateTime CompletionDate,
    List<StageBreakdownData> StageBreakdown,
    int ReworkCount);

public sealed record StageFunnelData(string StageName, decimal AverageDurationDays, decimal Percentage);

public sealed record CycleTimeIssueTypeData(
    string IssueType,
    int TicketCount,
    decimal MedianCycleTime,
    decimal P85CycleTime);

public sealed record CycleTimeDeveloperData(
    string DisplayName,
    string? AvatarUrl,
    string? SubTeam,
    int TicketsCompleted,
    decimal MedianCycleTime,
    decimal P85CycleTime,
    string DominantStage);

public sealed record CycleTimeOutlierData(
    string TicketKey,
    string TicketSummary,
    string IssueType,
    decimal CycleTimeDays,
    List<StageBreakdownData> StageBreakdown,
    int ReworkCount);

public sealed record CycleTimeBoundariesData(string StartStage, string EndStage);

public sealed record CycleTimeSingleSprintData(
    CycleTimeSprintData? Sprint,
    List<MetricCardData>? MetricCards,
    List<CycleTimeScatterPointData> ScatterPlot,
    List<StageFunnelData> StageFunnel,
    List<CycleTimeIssueTypeData> IssueTypeBreakdown,
    List<CycleTimeDeveloperData> DeveloperBreakdown,
    List<CycleTimeOutlierData> Outliers,
    CycleTimeBoundariesData Boundaries);

public sealed record CycleTimeTrendData(
    int SprintId,
    string SprintName,
    decimal P85CycleTime,
    decimal MedianCycleTime,
    int TicketsCompleted);

public sealed record CycleTimeSprintSummaryData(
    int SprintId,
    string SprintName,
    DateTime StartDate,
    int TicketsCompleted,
    decimal MedianCycleTime,
    decimal P85CycleTime,
    int OutlierCount);

public sealed record CycleTimeMultiSprintData(
    List<CycleTimeSprintData> Sprints,
    List<MetricCardData>? MetricCards,
    List<CycleTimeTrendData> Trend,
    List<StageFunnelData> StageFunnel,
    List<CycleTimeSprintSummaryData> SprintSummaries,
    CycleTimeBoundariesData Boundaries);
