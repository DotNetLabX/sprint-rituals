using Fokus.Domain.Analytics;
using Mapster;

namespace Fokus.API.Features.Analytics.GetCycleTime;

// --- Wire records (public API contracts — names/shapes must remain stable) ---

public record CycleTimeSprintInfo(int Id, string Name, DateTime StartDate, DateTime EndDate);

// Wire card type — MetricCardData → CycleTimeMetricCard via Mapster
public record CycleTimeMetricCard(
    string Name,
    decimal Value,
    string DisplayValue,
    decimal? Delta,
    string? DeltaDirection,
    string? DeltaPolarity);

public record StageBreakdownEntry(string StageName, decimal DurationDays);

public record CycleTimeScatterPoint(
    string TicketKey,
    string TicketSummary,
    string IssueType,
    decimal CycleTimeDays,
    DateTime CompletionDate,
    List<StageBreakdownEntry> StageBreakdown,
    int ReworkCount);

public record StageFunnelEntry(string StageName, decimal AverageDurationDays, decimal Percentage);

public record CycleTimeIssueTypeEntry(
    string IssueType,
    int TicketCount,
    decimal MedianCycleTime,
    decimal P85CycleTime);

public record CycleTimeDeveloperEntry(
    string DisplayName,
    string? AvatarUrl,
    string? SubTeam,
    int TicketsCompleted,
    decimal MedianCycleTime,
    decimal P85CycleTime,
    string DominantStage);

public record CycleTimeOutlierEntry(
    string TicketKey,
    string TicketSummary,
    string IssueType,
    decimal CycleTimeDays,
    List<StageBreakdownEntry> StageBreakdown,
    int ReworkCount);

public record CycleTimeBoundaries(string StartStage, string EndStage);

public record CycleTimeSingleSprintResponse(
    CycleTimeSprintInfo? Sprint,
    List<CycleTimeMetricCard>? MetricCards,
    List<CycleTimeScatterPoint> ScatterPlot,
    List<StageFunnelEntry> StageFunnel,
    List<CycleTimeIssueTypeEntry> IssueTypeBreakdown,
    List<CycleTimeDeveloperEntry> DeveloperBreakdown,
    List<CycleTimeOutlierEntry> Outliers,
    CycleTimeBoundaries Boundaries);

public record CycleTimeTrendEntry(
    int SprintId,
    string SprintName,
    decimal P85CycleTime,
    decimal MedianCycleTime,
    int TicketsCompleted);

public record CycleTimeSprintSummaryEntry(
    int SprintId,
    string SprintName,
    DateTime StartDate,
    int TicketsCompleted,
    decimal MedianCycleTime,
    decimal P85CycleTime,
    int OutlierCount);

public record CycleTimeMultiSprintResponse(
    List<CycleTimeSprintInfo> Sprints,
    List<CycleTimeMetricCard>? MetricCards,
    List<CycleTimeTrendEntry> Trend,
    List<StageFunnelEntry> StageFunnel,
    List<CycleTimeSprintSummaryEntry> SprintSummaries,
    CycleTimeBoundaries Boundaries);

// --- Mode envelope (wire-only) ---

public record CycleTimeResponse(
    string Mode,
    CycleTimeSingleSprintResponse? SingleSprint,
    CycleTimeMultiSprintResponse? MultiSprint);

// --- Mapster mappings ---

public class CycleTimeMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CycleTimeSprintData, CycleTimeSprintInfo>();
        // MetricCardData → CycleTimeMetricCard
        config.NewConfig<MetricCardData, CycleTimeMetricCard>();
        config.NewConfig<StageBreakdownData, StageBreakdownEntry>();
        config.NewConfig<CycleTimeScatterPointData, CycleTimeScatterPoint>();
        config.NewConfig<StageFunnelData, StageFunnelEntry>();
        config.NewConfig<CycleTimeIssueTypeData, CycleTimeIssueTypeEntry>();
        config.NewConfig<CycleTimeDeveloperData, CycleTimeDeveloperEntry>();
        config.NewConfig<CycleTimeOutlierData, CycleTimeOutlierEntry>();
        config.NewConfig<CycleTimeBoundariesData, CycleTimeBoundaries>();
        config.NewConfig<CycleTimeSingleSprintData, CycleTimeSingleSprintResponse>();
        config.NewConfig<CycleTimeTrendData, CycleTimeTrendEntry>();
        config.NewConfig<CycleTimeSprintSummaryData, CycleTimeSprintSummaryEntry>();
        config.NewConfig<CycleTimeMultiSprintData, CycleTimeMultiSprintResponse>();
    }
}
