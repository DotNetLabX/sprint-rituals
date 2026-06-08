using Fokus.Domain.Analytics;
using Mapster;

namespace Fokus.API.Features.Analytics.GetEpicProgress;

// Wire records for /api/analytics/epic-progress.

public record EpicProgressSummaryMetrics(
    int ActiveEpicCount,
    int CompletedEpicCount,
    decimal AverageCompletionPercentage);

public record TestRunSummaryDto(
    int Passed,
    int Failed,
    int Todo,
    int Executing,
    int Aborted);

public record EpicProgressTicketEntry(
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
    TestRunSummaryDto? TestRunSummary);

public record EpicProgressEntry(
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
    List<EpicProgressTicketEntry> Tickets,
    decimal? CoverageRate,
    decimal? PassRate,
    int BugsFound,
    int FeatureTicketCount,
    int CoveredTicketCount,
    string? CoverageRag,
    string? PassRateRag);

public record EpicProgressUnlinkedWork(
    int TicketCount,
    decimal TotalSp);

public record EpicProgressResponse(
    EpicProgressSummaryMetrics SummaryMetrics,
    List<EpicProgressEntry> Epics,
    EpicProgressUnlinkedWork UnlinkedWork,
    bool HasQaData,
    decimal? AverageTestCoverage);

// --- Mapster mappings ---

public class EpicProgressMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<EpicProgressSummaryMetricsData, EpicProgressSummaryMetrics>();
        config.NewConfig<TestRunSummaryData, TestRunSummaryDto>();
        config.NewConfig<EpicProgressTicketEntryData, EpicProgressTicketEntry>();
        config.NewConfig<EpicProgressEntryData, EpicProgressEntry>();
        config.NewConfig<EpicProgressUnlinkedWorkData, EpicProgressUnlinkedWork>();
        config.NewConfig<EpicProgressResultData, EpicProgressResponse>();
    }
}
