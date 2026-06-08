using Fokus.Domain.Analytics;
using Mapster;

namespace Fokus.API.Features.Analytics.GetDeveloperProgress;

// Wire records for /api/analytics/developer-progress.
// DeveloperProgressSprintInfo, CompletedTicketEntry, DayBreakdownEntry, StalledTicketEntry are
// declared in SharedAnalyticsWire.cs — not re-declared here.

public record DeveloperProgressAlert(
    string AccountId,
    string DisplayName,
    string? AvatarUrl,
    decimal GapSp,
    decimal GapDays,
    decimal? GapDelta,
    string Direction);

public record DeveloperProgressEntry(
    string AccountId,
    string DisplayName,
    string? SubTeam,
    string? AvatarUrl,
    decimal AssignedSp,
    decimal CompletedSp,
    decimal FeatureCompletedSp,
    decimal BugCompletedSp,
    decimal CompletionPercent,
    int CapacityPercent,
    decimal DailyPace,
    bool IsBehindPace,
    decimal? PaceGapSp,
    List<StalledTicketEntry> StalledTickets,
    List<DayBreakdownEntry> DailyBreakdown);

public record DeveloperProgressResponse(
    bool HasActiveSprint,
    DeveloperProgressSprintInfo? Sprint,
    int CurrentDay,
    int TotalDays,
    bool IsGracePeriod,
    List<DeveloperProgressAlert> Alerts,
    List<DeveloperProgressEntry> Developers);

// --- Mapster mappings ---

public class DeveloperProgressMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<DeveloperProgressSprintData, DeveloperProgressSprintInfo>();
        config.NewConfig<CompletedTicketData, CompletedTicketEntry>();
        config.NewConfig<DayBreakdownData, DayBreakdownEntry>();
        config.NewConfig<StalledTicketData, StalledTicketEntry>();
        config.NewConfig<DeveloperProgressAlertData, DeveloperProgressAlert>();
        config.NewConfig<DeveloperProgressEntryData, DeveloperProgressEntry>();
        config.NewConfig<DeveloperProgressResultData, DeveloperProgressResponse>();
    }
}
