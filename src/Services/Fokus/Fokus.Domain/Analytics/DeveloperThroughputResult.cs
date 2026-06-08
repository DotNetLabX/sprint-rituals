namespace Fokus.Domain.Analytics;

// Domain counterparts for DeveloperThroughput wire records.
// Wire types live in Fokus.API/Features/Analytics/ — domain types must not reference them (ADR-006).

public sealed record SprintSummaryItemData(int Id, string Name, DateTime StartDate, DateTime EndDate);

public sealed record SprintBreakdownData(
    int SprintId,
    decimal SpAssigned,
    decimal SpCompleted,
    decimal CompletionPercent,
    int TicketsDone,
    int TicketsCarriedOver,
    int CapacityPercent,
    decimal? RollingAverageSpCompleted,
    decimal? SpAssignedDelta,
    decimal? SpCompletedDelta,
    decimal? CompletionPercentDelta,
    int? TicketsDoneDelta,
    int? TicketsCarriedOverDelta,
    string? SpAssignedDeltaDirection,
    string? SpCompletedDeltaDirection,
    string? CompletionPercentDeltaDirection,
    string? TicketsDoneDeltaDirection,
    string? TicketsCarriedOverDeltaDirection,
    string? SpAssignedDeltaPolarity,
    string? SpCompletedDeltaPolarity,
    string? CompletionPercentDeltaPolarity,
    string? TicketsDoneDeltaPolarity,
    string? TicketsCarriedOverDeltaPolarity);

public sealed record DeveloperThroughputEntryData(
    string AccountId,
    string DisplayName,
    string? SubTeam,
    string? AvatarUrl,
    List<SprintBreakdownData> SprintBreakdowns);

public sealed record DeveloperThroughputResultData(
    List<SprintSummaryItemData> Sprints,
    List<DeveloperThroughputEntryData> Developers);
