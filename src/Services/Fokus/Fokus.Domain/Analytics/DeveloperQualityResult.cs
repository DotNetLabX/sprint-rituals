namespace Fokus.Domain.Analytics;

// Domain counterparts for DeveloperQuality result records.
// DeveloperQualitySparklinePointData is a local domain sparkline point — NOT reused from other calculators
// (3c-A pattern: each calculator owns its own domain sparkline type).

public sealed record DeveloperQualitySprintData(int Id, string Name, DateTime StartDate, DateTime EndDate);

public sealed record DeveloperQualitySparklinePointData(string SprintName, decimal Value);

public sealed record DeveloperQualitySprintBreakdownData(
    int SprintId,
    int Stories,
    int Covered,
    decimal CoveragePercent,
    decimal PassRatePercent,
    int Untested,
    int BugsFound,
    string? CoverageRag,
    string? PassRateRag,
    decimal? CoveragePercentDelta,
    string? CoveragePercentDeltaDirection,
    string? CoveragePercentDeltaPolarity,
    decimal? PassRatePercentDelta,
    string? PassRatePercentDeltaDirection,
    string? PassRatePercentDeltaPolarity,
    int? BugsFoundDelta,
    string? BugsFoundDeltaDirection,
    string? BugsFoundDeltaPolarity,
    List<DeveloperQualitySparklinePointData>? CoverageSparkline,
    List<DeveloperQualitySparklinePointData>? PassRateSparkline);

public sealed record DeveloperQualityEntryData(
    string AccountId,
    string DisplayName,
    string? SubTeam,
    string? AvatarUrl,
    List<DeveloperQualitySprintBreakdownData> SprintBreakdowns,
    int? BelowMedianStreak);

public sealed record DeveloperQualityResultData(
    bool HasQaData,
    List<DeveloperQualitySprintData> Sprints,
    List<DeveloperQualityEntryData> Developers);
