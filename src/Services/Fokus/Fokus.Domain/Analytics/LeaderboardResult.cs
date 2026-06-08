namespace Fokus.Domain.Analytics;

// --- Leaderboard domain result records ---
// Wire records live in Fokus.API/Features/Analytics/GetLeaderboard/GetLeaderboardResponse.cs
// Leaderboard has no metric cards — only a flat delta bundle (LeaderboardDeveloperDeltaData).

public sealed record LeaderboardSprintData(int Id, string Name, DateTime StartDate, DateTime EndDate);

public sealed record LeaderboardDeveloperSprintData(
    int SprintId,
    decimal FeatureSp,
    decimal BugSp,
    decimal TotalSp,
    int FeatureTickets,
    int BugTickets,
    int CapacityPercent);

public sealed record LeaderboardDeveloperData(
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
    List<LeaderboardDeveloperSprintData> SprintBreakdowns);

public sealed record LeaderboardMultiSprintData(
    List<LeaderboardSprintData> Sprints,
    List<LeaderboardDeveloperData> Developers);

/// <summary>
/// Flat delta bundle — NOT a metric card. Carries pre-computed direction/polarity string fields.
/// IMPORTANT: TotalSpDeltaPolarity is the hard-coded literal "neutral" (LeaderboardService.cs:213 carve-out).
/// It must NOT be converted to MetricCardBuilder.Polarity(...) — doing so would compute sign-based polarity
/// and silently change wire output for non-flat total-SP deltas.
/// FeatureSpDeltaPolarity, BugSpDeltaPolarity, FeatureTicketsDeltaPolarity, BugTicketsDeltaPolarity
/// use MetricCardBuilder.Polarity(...).
/// TotalSpDeltaDirection IS MetricCardBuilder.Direction(...); only the polarity is the literal.
/// </summary>
public sealed record LeaderboardDeveloperDeltaData(
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

public sealed record LeaderboardDeveloperSingleData(
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
    LeaderboardDeveloperDeltaData? Delta);

public sealed record LeaderboardSingleSprintData(
    LeaderboardSprintData Sprint,
    List<LeaderboardDeveloperSingleData> Developers);
