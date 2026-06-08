using Fokus.Domain.Analytics;
using Mapster;

namespace Fokus.API.Features.Analytics.GetCarryOver;

// --- Wire records (public API contracts — names/shapes must remain stable) ---

public record CarryOverSprintInfo(int Id, string Name, DateTime StartDate, DateTime EndDate);

// --- Multi-sprint ---

public record CarryOverSummaryMetrics(
    decimal AverageCarryOverRate,
    decimal AverageCarryOverSp,
    int TotalZombieTickets);

public record CarryOverStatusDistributionEntry(
    string StageName,
    int TicketCount,
    decimal SpTotal,
    decimal Percentage);

// Wire name kept for wire stability. Domain type is CarryOverPerSprintMetricsData.
// Mapster map: CarryOverPerSprintMetricsData → CarryOverPerSprintData (see RegisterMappings).
public record CarryOverPerSprintData(
    int SprintId,
    decimal CarryOverSp,
    int CarryOverTicketCount,
    decimal CarryOverRate,
    decimal TotalScopeSp,
    List<CarryOverStatusDistributionEntry> StatusDistribution);

public record CarryOverIssueTypeEntry(
    string IssueType,
    int TicketCount,
    decimal SpTotal,
    decimal Percentage);

public record CarryOverZombieSummary(
    string TicketKey,
    string Summary,
    string IssueType,
    string CurrentStatus,
    decimal? StoryPoints,
    int SprintCount);

public record CarryOverMultiSprintResponse(
    List<CarryOverSprintInfo> Sprints,
    CarryOverSummaryMetrics SummaryMetrics,
    List<CarryOverPerSprintData> PerSprintData,
    List<CarryOverIssueTypeEntry> IssueTypeBreakdown,
    List<CarryOverZombieSummary> ZombieTickets);

// --- Single-sprint ---

// Carry-over reuses ScopeMetricCard as its wire card type (wire stability).
// MetricCardData → ScopeMetricCard via Mapster.
public record ScopeMetricCard(
    string Name,
    decimal Value,
    string DisplayValue,
    decimal? Delta,
    string? DeltaDirection,
    string? DeltaPolarity);

public record CarryOverSingleSprintMetrics(
    ScopeMetricCard CarryOverRate,
    ScopeMetricCard CarryOverSp,
    ScopeMetricCard CarryOverTicketCount);

public record CarryOverDestinationBucket(int Count, decimal Sp);

public record CarryOverDestination(
    int PriorSprintId,
    string PriorSprintName,
    int PriorCarryOverCount,
    decimal PriorCarryOverSp,
    CarryOverDestinationBucket Completed,
    CarryOverDestinationBucket CarriedAgain,
    CarryOverDestinationBucket Removed,
    CarryOverDestinationBucket Dropped);

public record CarryOverTicketEntry(
    string TicketKey,
    string Summary,
    string IssueType,
    decimal? StoryPoints,
    string FinalStatus,
    string WorkflowStage,
    int SprintCount,
    bool IsZombie,
    bool IsExcluded);

public record ZombieTrajectorySprintEntry(int SprintId, string SprintName, string FinalStatus);

public record ZombieTrajectoryEntry(
    string TicketKey,
    string Summary,
    string IssueType,
    decimal? StoryPoints,
    string CurrentStatus,
    int SprintCount,
    List<ZombieTrajectorySprintEntry> Sprints);

public record CarryOverSingleSprintResponse(
    CarryOverSprintInfo Sprint,
    CarryOverSingleSprintMetrics Metrics,
    List<CarryOverStatusDistributionEntry> StatusDistribution,
    List<CarryOverIssueTypeEntry> IssueTypeBreakdown,
    CarryOverDestination? CarryOverDestination,
    List<CarryOverTicketEntry> Tickets,
    List<ZombieTrajectoryEntry> ZombieTrajectories);

// --- Mode envelope (wire-only) ---

public record CarryOverResponse(
    string Mode,
    CarryOverMultiSprintResponse? MultiSprint,
    CarryOverSingleSprintResponse? SingleSprint);

// --- Mapster mappings ---

public class CarryOverMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CarryOverSprintData, CarryOverSprintInfo>();
        config.NewConfig<CarryOverSummaryData, CarryOverSummaryMetrics>();
        config.NewConfig<CarryOverStatusDistributionData, CarryOverStatusDistributionEntry>();
        // Finding 9: domain CarryOverPerSprintMetricsData → wire CarryOverPerSprintData
        config.NewConfig<CarryOverPerSprintMetricsData, CarryOverPerSprintData>();
        config.NewConfig<CarryOverIssueTypeData, CarryOverIssueTypeEntry>();
        config.NewConfig<CarryOverZombieData, CarryOverZombieSummary>();
        config.NewConfig<CarryOverMultiSprintData, CarryOverMultiSprintResponse>();
        // MetricCardData → ScopeMetricCard (carry-over's wire card type)
        config.NewConfig<MetricCardData, ScopeMetricCard>();
        config.NewConfig<CarryOverSingleSprintMetricsData, CarryOverSingleSprintMetrics>();
        config.NewConfig<CarryOverDestinationBucketData, CarryOverDestinationBucket>();
        config.NewConfig<CarryOverDestinationData, CarryOverDestination>();
        config.NewConfig<CarryOverTicketData, CarryOverTicketEntry>();
        config.NewConfig<ZombieTrajectorySprintData, ZombieTrajectorySprintEntry>();
        config.NewConfig<ZombieTrajectoryData, ZombieTrajectoryEntry>();
        config.NewConfig<CarryOverSingleSprintData, CarryOverSingleSprintResponse>();
    }
}
