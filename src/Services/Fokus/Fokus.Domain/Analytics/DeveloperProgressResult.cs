namespace Fokus.Domain.Analytics;

// Domain counterparts for DeveloperProgress wire records.
// Shared-wire counterparts (SprintSummaryItemData is in DeveloperThroughputResult.cs).
// DeveloperDetail (Step 4) reuses DeveloperProgressSprintData, DayBreakdownData, CompletedTicketData.

public sealed record DeveloperProgressSprintData(int Id, string Name, DateTime StartDate, DateTime EndDate);

public sealed record DeveloperProgressAlertData(
    string AccountId,
    string DisplayName,
    string? AvatarUrl,
    decimal GapSp,
    decimal GapDays,
    decimal? GapDelta,
    string Direction);

public sealed record CompletedTicketData(string Key, string Summary, decimal? StoryPoints, string IssueType);

public sealed record DayBreakdownData(int Day, DateTime Date, List<CompletedTicketData> CompletedTickets, decimal CumulativeSp, decimal ExpectedCumulativeSp);

public sealed record StalledTicketData(string Key, string Summary, string CurrentStatus, string IssueType, decimal? StoryPoints, int DaysSinceLastTransition);

public sealed record DeveloperProgressEntryData(
    string AccountId,
    string DisplayName,
    string? SubTeam,
    string? AvatarUrl,
    decimal AssignedSp,
    decimal CompletedSp,
    decimal FeatureCompletedSp,
    decimal BugCompletedSp,
    decimal CompletionPercent,
    int CapacityPercent,
    decimal DailyPace,
    bool IsBehindPace,
    decimal? PaceGapSp,
    List<StalledTicketData> StalledTickets,
    List<DayBreakdownData> DailyBreakdown);

public sealed record DeveloperProgressResultData(
    bool HasActiveSprint,
    DeveloperProgressSprintData? Sprint,
    int CurrentDay,
    int TotalDays,
    bool IsGracePeriod,
    List<DeveloperProgressAlertData> Alerts,
    List<DeveloperProgressEntryData> Developers);
