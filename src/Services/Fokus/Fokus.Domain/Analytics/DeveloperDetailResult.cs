namespace Fokus.Domain.Analytics;

// Domain counterparts for DeveloperDetail wire records.
// CurrentSprintDetailData references DeveloperProgressSprintData, DayBreakdownData, CompletedTicketData
// from DeveloperProgressResult.cs (same namespace — no extra using needed).

public sealed record DeveloperDetailInfoData(
    string AccountId,
    string DisplayName,
    string? SubTeam,
    string? AvatarUrl,
    string Role,
    int DefaultCapacityPercent);

public sealed record SprintTrendData(
    int SprintId,
    string SprintName,
    DateTime StartDate,
    DateTime EndDate,
    decimal FeatureSp,
    decimal BugSp,
    decimal TotalSp,
    decimal AssignedSp,
    decimal CompletionPercent,
    int CapacityPercent,
    decimal? RollingAverageSp,
    decimal BugPercent);

public sealed record WorkAllocationData(
    decimal AverageBugPercent,
    int SprintsAboveTarget,
    int TotalSprints);

public sealed record CurrentSprintDetailData(
    DeveloperProgressSprintData Sprint,
    int CurrentDay,
    int TotalDays,
    decimal AssignedSp,
    decimal CompletedSp,
    decimal CompletionPercent,
    decimal FeatureCompletedSp,
    decimal BugCompletedSp,
    decimal DailyPace,
    bool IsBehindPace,
    decimal? PaceGapSp,
    List<DayBreakdownData> DailyBreakdown,
    List<TicketDetailData> Tickets);

public sealed record TicketDetailData(
    string Key,
    string Summary,
    string CurrentStatus,
    decimal? StoryPoints,
    string IssueType,
    int DaysInCurrentStatus,
    string State,
    bool IsStalled);

public sealed record DeveloperDetailResultData(
    DeveloperDetailInfoData Developer,
    List<SprintTrendData> SprintTrends,
    WorkAllocationData WorkAllocation,
    CurrentSprintDetailData? CurrentSprint,
    decimal BugRatioTarget,
    string JiraInstanceUrl);
