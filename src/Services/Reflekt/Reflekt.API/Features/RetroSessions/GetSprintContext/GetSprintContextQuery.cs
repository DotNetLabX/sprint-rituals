namespace Reflekt.API.Features.RetroSessions.GetSprintContext;

// ── Request ──────────────────────────────────────────────────────────────────

public sealed record SprintContextRequest(int RetroSessionId);

// ── Response ─────────────────────────────────────────────────────────────────

/// <summary>
/// Top-level response: ordered per-sprint sections (earliest sprint start first;
/// sections without a StartDate — cached/unavailable — sorted last).
/// </summary>
public sealed record SprintContextResponse(IReadOnlyList<SprintContextSection> Sprints);

/// <summary>
/// Freshness of data for a single sprint section.
/// "live"        — pulled from Fokus gRPC in this request.
/// "cached"      — gRPC unavailable; data from the local FokusSprintSnapshot (name/state only, no metrics).
/// "unavailable" — neither gRPC nor a snapshot exists for this sprint.
/// </summary>
public sealed record SprintContextSection(
    int SprintId,
    string? SprintName,
    DateTime? StartDate,
    DateTime? EndDate,
    string Freshness,
    // Metric fields — null when Freshness != "live"
    decimal? CompletionRate,
    string? CompletionRag,
    decimal? DisruptionRate,
    string? DisruptionRag,
    decimal? CarryOverRate,
    string? CarryOverRag,
    decimal? HealthScore,
    string? HealthRag,
    IReadOnlyList<ScopeAdditionDto>? AddedMidSprint,
    int? AddedMidSprintOverflow);

/// <summary>
/// A single mid-sprint scope addition — key and title only. No assignee data (ADR-019 / Rule 4).
/// </summary>
public sealed record ScopeAdditionDto(string TicketKey, string Summary, decimal? StoryPoints);
