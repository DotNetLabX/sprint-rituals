namespace Fokus.Domain.Analytics;

// --- SprintSummary domain result records ---
// QaBreakdownData is defined in QaMetricsResult.cs (same namespace) — not redefined here.
// TestingCrunchFlag is defined in TestTimelineResult.cs (same namespace) — not redefined here.

public record SprintInfoData(
    int Id,
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    int DurationDays,
    DateTime SyncedAt);

public record HealthScoreData(
    decimal CompositeScore,
    string CompositeRag,
    decimal CompletionSubScore,
    string CompletionRag,
    decimal DisruptionSubScore,
    string DisruptionRag,
    decimal CarryOverSubScore,
    string CarryOverRag)
{
    public decimal? QualitySubScore { get; init; }
    public string? QualityRag { get; init; }
    public QaBreakdownData? QualityBreakdown { get; init; }
}

public record SprintSparklinePoint(string SprintName, decimal Value);

public record SprintMetricCardData(
    string Name,
    decimal Value,
    string DisplayValue,
    decimal? Delta,
    string? DeltaDirection,
    string? DeltaPolarity,
    List<SprintSparklinePoint> Sparkline)
{
    public string? Rag { get; init; }
}

public record SprintMetricsData(
    SprintMetricCardData SpCompleted,
    SprintMetricCardData CompletionRate,
    SprintMetricCardData ScopeDisruptionRate,
    SprintMetricCardData BugDisruptionRate,
    SprintMetricCardData CarryOverRate,
    decimal BugSpCompleted);

public record EpicProgressData(
    string EpicName,
    decimal SpCompletedThisSprint,
    decimal TotalSp,
    decimal DoneSp,
    decimal CompletionPercentage);

public record DeveloperSummaryData(
    string DisplayName,
    string? AvatarUrl,
    string? SubTeam,
    decimal SpCompleted,
    decimal FeatureSp,
    decimal BugSp,
    int FeatureTickets,
    int BugTickets,
    int CapacityPercent);

public record ZombieTicketData(string TicketKey, string Summary, int SprintCount, string? AssigneeName);

public record MidSprintDisruptionData(decimal TotalSp, int TicketCount);

public record SprintFlagsData(
    List<ZombieTicketData> ZombieTickets,
    MidSprintDisruptionData? MidSprintDisruption,
    List<string> ZeroSpDevelopers,
    bool HasAnyFlags)
{
    public TestingCrunchFlag? TestingCrunch { get; init; }
}

public record SprintSummaryData(
    SprintInfoData? Sprint,
    HealthScoreData? HealthScore,
    SprintMetricsData? Metrics,
    List<EpicProgressData> TopEpics,
    List<DeveloperSummaryData> Leaderboard,
    SprintFlagsData Flags);
