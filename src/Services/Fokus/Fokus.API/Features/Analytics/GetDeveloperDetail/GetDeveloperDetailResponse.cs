using Fokus.Domain.Analytics;
using Mapster;

namespace Fokus.API.Features.Analytics.GetDeveloperDetail;

// Wire records for /api/analytics/developer-detail/{accountId}.
// DeveloperProgressSprintInfo, DayBreakdownEntry, CompletedTicketEntry are in SharedAnalyticsWire.cs.
// TicketDetailEntry is Detail-only — declared here.

public record DeveloperDetailInfo(
    string AccountId,
    string DisplayName,
    string? SubTeam,
    string? AvatarUrl,
    string Role,
    int DefaultCapacityPercent);

public record SprintTrendEntry(
    int SprintId,
    string SprintName,
    DateTime StartDate,
    DateTime EndDate,
    decimal FeatureSp,
    decimal BugSp,
    decimal TotalSp,
    decimal AssignedSp,
    decimal CompletionPercent,
    int CapacityPercent,
    decimal? RollingAverageSp,
    decimal BugPercent);

public record WorkAllocationSummary(
    decimal AverageBugPercent,
    int SprintsAboveTarget,
    int TotalSprints);

public record TicketDetailEntry(
    string Key,
    string Summary,
    string CurrentStatus,
    decimal? StoryPoints,
    string IssueType,
    int DaysInCurrentStatus,
    string State,
    bool IsStalled);

public record CurrentSprintDetail(
    DeveloperProgressSprintInfo Sprint,
    int CurrentDay,
    int TotalDays,
    decimal AssignedSp,
    decimal CompletedSp,
    decimal CompletionPercent,
    decimal FeatureCompletedSp,
    decimal BugCompletedSp,
    decimal DailyPace,
    bool IsBehindPace,
    decimal? PaceGapSp,
    List<DayBreakdownEntry> DailyBreakdown,
    List<TicketDetailEntry> Tickets);

public record DeveloperDetailResponse(
    DeveloperDetailInfo Developer,
    List<SprintTrendEntry> SprintTrends,
    WorkAllocationSummary WorkAllocation,
    CurrentSprintDetail? CurrentSprint,
    decimal BugRatioTarget,
    string JiraInstanceUrl);

// --- Mapster mappings ---

public class DeveloperDetailMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<DeveloperDetailInfoData, DeveloperDetailInfo>();
        config.NewConfig<SprintTrendData, SprintTrendEntry>();
        config.NewConfig<WorkAllocationData, WorkAllocationSummary>();
        config.NewConfig<TicketDetailData, TicketDetailEntry>();
        config.NewConfig<CompletedTicketData, CompletedTicketEntry>();
        config.NewConfig<DayBreakdownData, DayBreakdownEntry>();
        config.NewConfig<DeveloperProgressSprintData, DeveloperProgressSprintInfo>();
        config.NewConfig<CurrentSprintDetailData, CurrentSprintDetail>();
        config.NewConfig<DeveloperDetailResultData, DeveloperDetailResponse>();
    }
}
