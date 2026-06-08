using Fokus.Domain.Analytics;
using Mapster;

namespace Fokus.API.Features.Analytics.GetLeaderboard;

// --- Wire records (public API contracts — names/shapes must remain stable) ---

public record LeaderboardSprintInfo(int Id, string Name, DateTime StartDate, DateTime EndDate);

public record LeaderboardDeveloperSprintBreakdown(
    int SprintId,
    decimal FeatureSp,
    decimal BugSp,
    decimal TotalSp,
    int FeatureTickets,
    int BugTickets,
    int CapacityPercent);

public record LeaderboardDeveloperEntry(
    string AccountId,
    string DisplayName,
    string? SubTeam,
    string? AvatarUrl,
    decimal FeatureSp,
    decimal BugSp,
    decimal TotalSp,
    int FeatureTickets,
    int BugTickets,
    int TotalTickets,
    int CapacityPercent,
    List<LeaderboardDeveloperSprintBreakdown> SprintBreakdowns);

public record LeaderboardMultiSprintResponse(
    List<LeaderboardSprintInfo> Sprints,
    List<LeaderboardDeveloperEntry> Developers);

public record LeaderboardDeveloperDelta(
    decimal FeatureSpDelta,
    string FeatureSpDeltaDirection,
    string FeatureSpDeltaPolarity,
    decimal BugSpDelta,
    string BugSpDeltaDirection,
    string BugSpDeltaPolarity,
    decimal TotalSpDelta,
    string TotalSpDeltaDirection,
    string TotalSpDeltaPolarity,
    int FeatureTicketsDelta,
    string FeatureTicketsDeltaDirection,
    string FeatureTicketsDeltaPolarity,
    int BugTicketsDelta,
    string BugTicketsDeltaDirection,
    string BugTicketsDeltaPolarity);

public record LeaderboardDeveloperSingleEntry(
    string AccountId,
    string DisplayName,
    string? SubTeam,
    string? AvatarUrl,
    decimal FeatureSp,
    decimal BugSp,
    decimal TotalSp,
    int FeatureTickets,
    int BugTickets,
    int TotalTickets,
    int CapacityPercent,
    LeaderboardDeveloperDelta? Delta);

public record LeaderboardSingleSprintResponse(
    LeaderboardSprintInfo Sprint,
    List<LeaderboardDeveloperSingleEntry> Developers);

// --- Mode envelope (wire-only) ---

public record LeaderboardResponse(
    string Mode,
    LeaderboardMultiSprintResponse? MultiSprint,
    LeaderboardSingleSprintResponse? SingleSprint);

// --- Mapster mappings ---

public class LeaderboardMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<LeaderboardSprintData, LeaderboardSprintInfo>();
        config.NewConfig<LeaderboardDeveloperSprintData, LeaderboardDeveloperSprintBreakdown>();
        config.NewConfig<LeaderboardDeveloperData, LeaderboardDeveloperEntry>();
        config.NewConfig<LeaderboardMultiSprintData, LeaderboardMultiSprintResponse>();
        // Flat delta bundle mapping
        config.NewConfig<LeaderboardDeveloperDeltaData, LeaderboardDeveloperDelta>();
        config.NewConfig<LeaderboardDeveloperSingleData, LeaderboardDeveloperSingleEntry>();
        config.NewConfig<LeaderboardSingleSprintData, LeaderboardSingleSprintResponse>();
    }
}
