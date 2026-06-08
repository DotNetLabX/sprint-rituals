using Fokus.Domain.Analytics;
using Mapster;

namespace Fokus.API.Features.Analytics.GetDeveloperThroughput;

// Wire records for /api/analytics/developer-throughput.
// SprintSummaryItem is declared in SharedAnalyticsWire.cs (shared cross-endpoint) — not re-declared here.

public record SprintBreakdown(
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

public record DeveloperThroughputEntry(
    string AccountId,
    string DisplayName,
    string? SubTeam,
    string? AvatarUrl,
    List<SprintBreakdown> SprintBreakdowns);

public record DeveloperThroughputResponse(
    List<SprintSummaryItem> Sprints,
    List<DeveloperThroughputEntry> Developers);

// --- Mapster mappings ---

public class DeveloperThroughputMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<SprintSummaryItemData, SprintSummaryItem>();
        config.NewConfig<SprintBreakdownData, SprintBreakdown>();
        config.NewConfig<DeveloperThroughputEntryData, DeveloperThroughputEntry>();
        config.NewConfig<DeveloperThroughputResultData, DeveloperThroughputResponse>();
    }
}
