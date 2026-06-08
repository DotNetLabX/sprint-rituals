namespace Fokus.Domain.Analytics;

// Domain result records for EpicProgressAnalyzer.
// EpicQaData is a domain-layer input type (not a wire DTO) — declared here alongside the result records.

public sealed record EpicQaData(
    Dictionary<string, List<string>> TestsLinksByTicket,
    Dictionary<string, List<string>> BlocksLinksByTicket,
    Dictionary<string, List<TestRun>> RunsByTeId);

public sealed record EpicProgressSummaryMetricsData(
    int ActiveEpicCount,
    int CompletedEpicCount,
    decimal AverageCompletionPercentage);

public sealed record TestRunSummaryData(
    int Passed,
    int Failed,
    int Todo,
    int Executing,
    int Aborted);

public sealed record EpicProgressTicketEntryData(
    string TicketKey,
    string Summary,
    string IssueType,
    decimal? StoryPoints,
    string CurrentStatus,
    string? AssigneeDisplayName,
    bool IsDone,
    string? TestStatus,
    decimal? TestPassRate,
    int? TestBugsFound,
    TestRunSummaryData? TestRunSummary);

public sealed record EpicProgressEntryData(
    string EpicKey,
    string EpicName,
    int TotalTickets,
    int DoneTickets,
    int RemainingTickets,
    decimal TicketCompletionPercentage,
    decimal TotalSp,
    decimal DoneSp,
    decimal RemainingSp,
    decimal? ImputedSp,
    decimal? AdjustedTotalSp,
    decimal? SpCompletionPercentage,
    int UnestimatedTicketCount,
    decimal? Velocity,
    decimal? ProjectedSprintsRemaining,
    string? ProjectionConfidence,
    int ActiveSprintCount,
    DateOnly? StartedDate,
    DateOnly? LastWorkDate,
    bool IsCompleted,
    List<EpicProgressTicketEntryData> Tickets,
    decimal? CoverageRate,
    decimal? PassRate,
    int BugsFound,
    int FeatureTicketCount,
    int CoveredTicketCount,
    string? CoverageRag,
    string? PassRateRag);

public sealed record EpicProgressUnlinkedWorkData(
    int TicketCount,
    decimal TotalSp);

public sealed record EpicProgressResultData(
    EpicProgressSummaryMetricsData SummaryMetrics,
    List<EpicProgressEntryData> Epics,
    EpicProgressUnlinkedWorkData UnlinkedWork,
    bool HasQaData,
    decimal? AverageTestCoverage);
