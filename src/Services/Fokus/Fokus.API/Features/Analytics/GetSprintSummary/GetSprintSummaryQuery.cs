namespace Fokus.API.Features.Analytics.GetSprintSummary;

public class GetSprintSummaryQuery
{
    public int? SprintId { get; set; }
    public string? SubTeam { get; set; }
}

public class GetSprintSummaryQueryValidator : Validator<GetSprintSummaryQuery>
{
    public GetSprintSummaryQueryValidator()
    {
        RuleFor(x => x.SprintId)
            .GreaterThan(0)
            .When(x => x.SprintId.HasValue)
            .WithMessage("Sprint ID must be greater than 0.");

        RuleFor(x => x.SubTeam)
            .NotEmpty()
            .When(x => x.SubTeam is not null)
            .WithMessage("SubTeam must not be empty when provided.");
    }
}

// --- Wire types (endpoint-owned, ADR-010) ---

public record SprintInfo(
    int Id,
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    int DurationDays,
    DateTime SyncedAt);

public record HealthScoreResult(
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
    public QualityBreakdownResult? QualityBreakdown { get; init; }
}

public record QualityBreakdownResult(
    decimal CoverageScore,
    int CoverageWeight,
    decimal PassRateScore,
    int PassRateWeight);

public record SparklinePoint(string SprintName, decimal Value);

public record MetricCard(
    string Name,
    decimal Value,
    string DisplayValue,
    decimal? Delta,
    string? DeltaDirection,
    string? DeltaPolarity,
    List<SparklinePoint> Sparkline)
{
    public string? Rag { get; init; }
}

public record MetricsResult(
    MetricCard SpCompleted,
    MetricCard CompletionRate,
    MetricCard ScopeDisruptionRate,
    MetricCard BugDisruptionRate,
    MetricCard CarryOverRate,
    decimal BugSpCompleted);

public record EpicProgress(
    string EpicName,
    decimal SpCompletedThisSprint,
    decimal TotalSp,
    decimal DoneSp,
    decimal CompletionPercentage);

public record DeveloperSummary(
    string DisplayName,
    string? AvatarUrl,
    string? SubTeam,
    decimal SpCompleted,
    decimal FeatureSp,
    decimal BugSp,
    int FeatureTickets,
    int BugTickets,
    int CapacityPercent);

public record ZombieTicket(string TicketKey, string Summary, int SprintCount, string? AssigneeName);

public record MidSprintDisruption(decimal TotalSp, int TicketCount);

public record FlagsResult(
    List<ZombieTicket> ZombieTickets,
    MidSprintDisruption? MidSprintDisruption,
    List<string> ZeroSpDevelopers,
    bool HasAnyFlags)
{
    public TestingCrunchResult? TestingCrunch { get; init; }
}

public record TestingCrunchResult(
    decimal CrunchPercentage,
    int CrunchRunCount,
    int TotalRunCount);

public record SprintSummaryResponse(
    SprintInfo? Sprint,
    HealthScoreResult? HealthScore,
    MetricsResult? Metrics,
    List<EpicProgress> TopEpics,
    List<DeveloperSummary> Leaderboard,
    FlagsResult Flags);
