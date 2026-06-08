namespace Fokus.Domain.Analytics;

// --- CarryOver domain result records ---
// Wire records live in Fokus.API/Features/Analytics/GetCarryOver/GetCarryOverResponse.cs

public sealed record CarryOverSprintData(int Id, string Name, DateTime StartDate, DateTime EndDate);

public sealed record CarryOverSummaryData(
    decimal AverageCarryOverRate,
    decimal AverageCarryOverSp,
    int TotalZombieTickets);

public sealed record CarryOverStatusDistributionData(
    string StageName,
    int TicketCount,
    decimal SpTotal,
    decimal Percentage);

public sealed record CarryOverPerSprintMetricsData(
    int SprintId,
    decimal CarryOverSp,
    int CarryOverTicketCount,
    decimal CarryOverRate,
    decimal TotalScopeSp,
    List<CarryOverStatusDistributionData> StatusDistribution);

public sealed record CarryOverIssueTypeData(
    string IssueType,
    int TicketCount,
    decimal SpTotal,
    decimal Percentage);

public sealed record CarryOverZombieData(
    string TicketKey,
    string Summary,
    string IssueType,
    string CurrentStatus,
    decimal? StoryPoints,
    int SprintCount);

public sealed record CarryOverMultiSprintData(
    List<CarryOverSprintData> Sprints,
    CarryOverSummaryData SummaryMetrics,
    List<CarryOverPerSprintMetricsData> PerSprintData,
    List<CarryOverIssueTypeData> IssueTypeBreakdown,
    List<CarryOverZombieData> ZombieTickets);

// Single-sprint domain records

public sealed record CarryOverSingleSprintMetricsData(
    MetricCardData CarryOverRate,
    MetricCardData CarryOverSp,
    MetricCardData CarryOverTicketCount);

public sealed record CarryOverDestinationBucketData(int Count, decimal Sp);

public sealed record CarryOverDestinationData(
    int PriorSprintId,
    string PriorSprintName,
    int PriorCarryOverCount,
    decimal PriorCarryOverSp,
    CarryOverDestinationBucketData Completed,
    CarryOverDestinationBucketData CarriedAgain,
    CarryOverDestinationBucketData Removed,
    CarryOverDestinationBucketData Dropped);

public sealed record CarryOverTicketData(
    string TicketKey,
    string Summary,
    string IssueType,
    decimal? StoryPoints,
    string FinalStatus,
    string WorkflowStage,
    int SprintCount,
    bool IsZombie,
    bool IsExcluded);

public sealed record ZombieTrajectorySprintData(int SprintId, string SprintName, string FinalStatus);

public sealed record ZombieTrajectoryData(
    string TicketKey,
    string Summary,
    string IssueType,
    decimal? StoryPoints,
    string CurrentStatus,
    int SprintCount,
    List<ZombieTrajectorySprintData> Sprints);

public sealed record CarryOverSingleSprintData(
    CarryOverSprintData Sprint,
    CarryOverSingleSprintMetricsData Metrics,
    List<CarryOverStatusDistributionData> StatusDistribution,
    List<CarryOverIssueTypeData> IssueTypeBreakdown,
    CarryOverDestinationData? CarryOverDestination,
    List<CarryOverTicketData> Tickets,
    List<ZombieTrajectoryData> ZombieTrajectories);
