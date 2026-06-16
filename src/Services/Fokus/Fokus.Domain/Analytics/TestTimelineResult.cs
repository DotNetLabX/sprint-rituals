namespace Fokus.Domain.Analytics;

// --- TestTimeline domain result records ---

public sealed record TestTimelineData(
    bool HasQaData,
    bool IsXrayEnabled,
    DateTime SprintStartDate,
    DateTime SprintEndDate,
    int PlanningWindowDays,
    IReadOnlyList<BurnupDayData> BurnupData,
    IReadOnlyList<ScopeChangeDayData> ScopeChangeOverlay,
    TestingCrunchData TestingCrunch,
    PostSprintTestingData PostSprintTesting,
    UntestedAtCloseData UntestedAtClose,
    DevToTestGapData DevToTestGap);

public sealed record BurnupDayData(
    int DayNumber,
    DateTime CalendarDate,
    bool IsWithinSprint,
    int CumulativePass,
    int CumulativeFail,
    int CumulativeTotal,
    int DailyPass,
    int DailyFail);

public sealed record ScopeChangeDayData(
    int DayNumber,
    DateTime CalendarDate,
    decimal AddedSp,
    decimal RemovedSp,
    decimal NetSp);

public sealed record TestingCrunchData(
    bool IsCrunchFlagged,
    decimal? CrunchPercentage,
    int CrunchRunCount,
    int TotalRunCount,
    IReadOnlyList<CrunchTicketData> CrunchTickets);

public sealed record CrunchTicketData(
    string TicketKey,
    string Summary,
    string? AssigneeName,
    decimal? StoryPoints,
    int LateRunCount);

public sealed record PostSprintTestingData(
    bool HasPostSprintTesting,
    decimal? PostSprintPercentage,
    int PostSprintRunCount,
    int TotalRunCount,
    IReadOnlyList<PostSprintTicketData> PostSprintTickets);

public sealed record PostSprintTicketData(
    string TicketKey,
    string Summary,
    string? AssigneeName,
    decimal? StoryPoints,
    int PostSprintRunCount);

public sealed record UntestedAtCloseData(
    bool HasUntestedAtClose,
    int UntestedAtCloseCount,
    IReadOnlyList<UntestedTicketData> UntestedAtCloseTickets);

public sealed record UntestedTicketData(
    string TicketKey,
    string Summary,
    string? AssigneeName,
    decimal? StoryPoints,
    DateTime DevDoneDate);

public sealed record DevToTestGapData(
    decimal? MedianGapDays,
    decimal? MedianGapDelta,
    string? MedianGapDirection,
    IReadOnlyList<GapTicketData> GapTickets);

public sealed record GapTicketData(
    string TicketKey,
    string Summary,
    string? AssigneeName,
    DateTime DevDoneDate,
    DateTime FirstTestDate,
    decimal GapDays);

/// <summary>
/// Cross-consumed domain type: testing crunch flag used by SprintSummaryAnalyzer and gRPC service.
/// </summary>
public sealed record TestingCrunchFlag(
    decimal CrunchPercentage,
    int CrunchRunCount,
    int TotalRunCount);
