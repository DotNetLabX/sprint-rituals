using Fokus.Domain.Analytics;
using Mapster;

namespace Fokus.API.Features.Analytics.GetBugRatio;

// --- Wire records (public API contracts — names/shapes must remain stable) ---

public record BugRatioSprintInfo(int Id, string Name, DateTime StartDate, DateTime EndDate);

// --- Multi-sprint ---

public record BugRatioTeamTrendEntry(
    int SprintId,
    decimal BugRatioPercent,
    decimal BugSp,
    decimal NonBugSp,
    decimal CompletedSp);

public record BugRatioTeamMetrics(
    decimal BugRatioPercent,
    decimal TotalBugSp,
    decimal TotalNonBugSp,
    decimal TotalCompletedSp,
    List<BugRatioTeamTrendEntry> PerSprintTrend);

public record BugRatioIssueTypeEntry(
    string IssueType,
    int TicketCount,
    decimal SpTotal);

public record BugRatioAlertStatus(bool IsActive, int ConsecutiveSprintCount, int ThresholdPercent);

public record BugRatioDeveloperSprintBreakdown(
    int SprintId,
    decimal BugSp,
    decimal NonBugSp,
    decimal CompletedSp,
    decimal BugRatioPercent,
    int BugTicketCount,
    int NonBugTicketCount);

public record BugRatioDeveloperEntry(
    string AccountId,
    string DisplayName,
    string? SubTeam,
    string? AvatarUrl,
    decimal BugSp,
    decimal NonBugSp,
    decimal CompletedSp,
    decimal BugRatioPercent,
    int BugTicketCount,
    int NonBugTicketCount,
    List<BugRatioDeveloperSprintBreakdown> SprintBreakdowns,
    BugRatioAlertStatus Alert);

public record BugRatioMultiSprintResponse(
    List<BugRatioSprintInfo> Sprints,
    BugRatioTeamMetrics TeamMetrics,
    List<BugRatioIssueTypeEntry> IssueTypeBreakdown,
    List<BugRatioDeveloperEntry> Developers);

// --- Single-sprint ---

// Wire card type — MetricCardData → BugRatioMetricCard via Mapster
public record BugRatioMetricCard(
    string Name,
    decimal Value,
    string DisplayValue,
    decimal? Delta,
    string? DeltaDirection,
    string? DeltaPolarity);

public record BugRatioTeamSingleMetrics(
    BugRatioMetricCard BugRatioPercent,
    BugRatioMetricCard BugSp,
    BugRatioMetricCard NonBugSp);

// Wire flat delta bundle — BugRatioDeveloperDeltaData → BugRatioDeveloperDelta via Mapster
public record BugRatioDeveloperDelta(
    decimal BugSpDelta,
    string BugSpDeltaDirection,
    string BugSpDeltaPolarity,
    decimal NonBugSpDelta,
    string NonBugSpDeltaDirection,
    string NonBugSpDeltaPolarity,
    decimal BugRatioPercentDelta,
    string BugRatioPercentDeltaDirection,
    string BugRatioPercentDeltaPolarity,
    int BugTicketCountDelta,
    string BugTicketCountDeltaDirection,
    string BugTicketCountDeltaPolarity,
    int NonBugTicketCountDelta,
    string NonBugTicketCountDeltaDirection,
    string NonBugTicketCountDeltaPolarity);

public record BugRatioDeveloperSingleEntry(
    string AccountId,
    string DisplayName,
    string? SubTeam,
    string? AvatarUrl,
    decimal BugSp,
    decimal NonBugSp,
    decimal CompletedSp,
    decimal BugRatioPercent,
    int BugTicketCount,
    int NonBugTicketCount,
    BugRatioDeveloperDelta? Delta,
    BugRatioAlertStatus Alert);

public record BugRatioSingleSprintResponse(
    BugRatioSprintInfo Sprint,
    BugRatioTeamSingleMetrics TeamMetrics,
    List<BugRatioIssueTypeEntry> IssueTypeBreakdown,
    List<BugRatioDeveloperSingleEntry> Developers);

// --- Mode envelope (wire-only) ---

public record BugRatioResponse(
    string Mode,
    BugRatioMultiSprintResponse? MultiSprint,
    BugRatioSingleSprintResponse? SingleSprint);

// --- Mapster mappings ---

public class BugRatioMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<BugRatioSprintData, BugRatioSprintInfo>();
        config.NewConfig<BugRatioTeamTrendData, BugRatioTeamTrendEntry>();
        config.NewConfig<BugRatioTeamMetricsData, BugRatioTeamMetrics>();
        config.NewConfig<BugRatioIssueTypeData, BugRatioIssueTypeEntry>();
        config.NewConfig<BugRatioAlertData, BugRatioAlertStatus>();
        config.NewConfig<BugRatioDeveloperSprintData, BugRatioDeveloperSprintBreakdown>();
        config.NewConfig<BugRatioDeveloperData, BugRatioDeveloperEntry>();
        config.NewConfig<BugRatioMultiSprintData, BugRatioMultiSprintResponse>();
        // MetricCardData → BugRatioMetricCard
        config.NewConfig<MetricCardData, BugRatioMetricCard>();
        config.NewConfig<BugRatioTeamSingleData, BugRatioTeamSingleMetrics>();
        // Flat delta bundle mapping
        config.NewConfig<BugRatioDeveloperDeltaData, BugRatioDeveloperDelta>();
        config.NewConfig<BugRatioDeveloperSingleData, BugRatioDeveloperSingleEntry>();
        config.NewConfig<BugRatioSingleSprintData, BugRatioSingleSprintResponse>();
    }
}
