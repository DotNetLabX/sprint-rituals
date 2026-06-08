using Fokus.Domain.Analytics;
using Mapster;

namespace Fokus.API.Features.Analytics.GetScopeChange;

// --- Wire records (public API contracts — names/shapes must remain stable) ---

public record ScopeChangeSprintInfo(int Id, string Name, DateTime StartDate, DateTime EndDate);

public record ScopeChangeSummaryMetrics(
    decimal AverageDisruptionRate,
    decimal? AverageDisruptionRateDelta,
    string? AverageDisruptionRateDeltaDirection,
    decimal AverageNetScopeChange,
    int TotalBugsAdded);

public record ScopeChangePerSprintData(
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

public record ClassificationEntry(
    string Category,
    int TicketCount,
    decimal? SpTotal,
    decimal Percentage);

public record ScopeChangeMultiSprintResponse(
    List<ScopeChangeSprintInfo> Sprints,
    ScopeChangeSummaryMetrics SummaryMetrics,
    List<ScopeChangePerSprintData> PerSprintData,
    List<ClassificationEntry> ClassificationBreakdown);

// Wire card type — MetricCardData → ScopeMetricCard via Mapster.
// Note: ScopeMetricCard also exists in Fokus.API.Features.Analytics.GetCarryOver (different namespace).
public record ScopeMetricCard(
    string Name,
    decimal Value,
    string DisplayValue,
    decimal? Delta,
    string? DeltaDirection,
    string? DeltaPolarity);

public record ScopeChangeSingleSprintMetrics(
    ScopeMetricCard CommittedSpActive,
    ScopeMetricCard CommittedSpTotal,
    ScopeMetricCard AddedSp,
    ScopeMetricCard RemovedSp,
    ScopeMetricCard NetScopeChange,
    ScopeMetricCard DisruptionRate,
    ScopeMetricCard BugCount);

public record BurnupDataPoint(
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

public record ScopeChangeEvent(
    DateTime Date,
    int SprintDayNumber,
    string TicketKey,
    string TicketSummary,
    decimal? StoryPoints,
    string IssueType,
    string Action,
    string? Category,
    bool IsExcluded);

public record BugTimeInProgress(
    string TicketKey,
    string TicketSummary,
    decimal TimeInActiveDays,
    string CurrentStatus);

public record ScopeChangeSingleSprintResponse(
    ScopeChangeSprintInfo Sprint,
    ScopeChangeSingleSprintMetrics Metrics,
    List<BurnupDataPoint> BurnupData,
    List<ClassificationEntry> ClassificationBreakdown,
    List<ScopeChangeEvent> Events,
    List<BugTimeInProgress> BugTimeInProgress);

// --- Mode envelope (wire-only) ---

public record ScopeChangeResponse(
    string Mode,
    ScopeChangeMultiSprintResponse? MultiSprint,
    ScopeChangeSingleSprintResponse? SingleSprint);

// --- Mapster mappings ---

public class ScopeChangeMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ScopeChangeSprintData, ScopeChangeSprintInfo>();
        config.NewConfig<ScopeChangeSummaryData, ScopeChangeSummaryMetrics>();
        config.NewConfig<ScopeChangePerSprintMetricsData, ScopeChangePerSprintData>();
        config.NewConfig<ClassificationData, ClassificationEntry>();
        config.NewConfig<ScopeChangeMultiSprintData, ScopeChangeMultiSprintResponse>();
        // MetricCardData → ScopeMetricCard (scope-change's wire card type)
        config.NewConfig<MetricCardData, ScopeMetricCard>();
        config.NewConfig<ScopeChangeSingleSprintMetricsData, ScopeChangeSingleSprintMetrics>();
        config.NewConfig<BurnupDataPointData, BurnupDataPoint>();
        config.NewConfig<ScopeChangeEventData, ScopeChangeEvent>();
        config.NewConfig<BugTimeInProgressData, BugTimeInProgress>();
        config.NewConfig<ScopeChangeSingleSprintData, ScopeChangeSingleSprintResponse>();
    }
}
