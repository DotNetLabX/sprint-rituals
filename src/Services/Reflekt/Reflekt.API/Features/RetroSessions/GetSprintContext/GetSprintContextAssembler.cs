using Blocks.Contracts.Grpc;
using Blocks.Contracts.Grpc.DataContracts;
using Microsoft.Extensions.Logging;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.RetroSessions.GetSprintContext;

/// <summary>
/// Assembles the sprint-context response for a list of Fokus sprint IDs.
/// Extracted from <see cref="GetSprintContextEndpoint"/> so the assembly logic
/// is testable without requiring the FastEndpoints HTTP pipeline.
/// </summary>
public sealed class GetSprintContextAssembler(
    FokusSprintSnapshotRepository snapshotRepository,
    ISprintAnalyticsService sprintAnalyticsService,
    ILogger<GetSprintContextAssembler> logger)
{
    private const int ScopeAdditionsDisplayLimit = 3;

    /// <summary>
    /// Builds ordered sections for the given Fokus sprint IDs.
    /// Each sprint is fetched independently; a gRPC failure on one sprint
    /// degrades that section to "cached" or "unavailable" without affecting others.
    /// </summary>
    public async Task<IReadOnlyList<SprintContextSection>> BuildSectionsAsync(
        IEnumerable<int> fokusSprintIds, CancellationToken ct)
    {
        var sections = new List<SprintContextSection>();

        foreach (var sprintId in fokusSprintIds)
        {
            var section = await BuildSectionAsync(sprintId, ct);
            sections.Add(section);
        }

        return OrderSections(sections);
    }

    private async Task<SprintContextSection> BuildSectionAsync(int fokusSprintId, CancellationToken ct)
    {
        try
        {
            var summary = await sprintAnalyticsService.GetSprintSummaryAsync(
                new SprintQuery { SprintId = fokusSprintId }, ct);

            return BuildLiveSection(summary);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Per-sprint isolation: one sprint's gRPC failure never blanks the panel (Rule 9).
            // OperationCanceledException propagates — swallowing it would produce a spurious
            // snapshot fallback on request cancellation.
            logger.LogWarning(ex,
                "Fokus gRPC pull failed for sprint {SprintId}; falling back to snapshot.", fokusSprintId);
        }

        return await BuildFallbackSectionAsync(fokusSprintId, ct);
    }

    private static SprintContextSection BuildLiveSection(AnonymizedSprintSummary summary)
    {
        var addedMidSprint = BuildScopeAdditions(summary, out var overflow);

        // StartDate/EndDate come from the Fokus gRPC contract as non-nullable DateTime.
        // The server guarantees real sprint dates — a default DateTime.MinValue would
        // mis-sort to the front of the panel if that guarantee ever broke.
        return new SprintContextSection(
            SprintId: summary.SprintId,
            SprintName: summary.SprintName,
            StartDate: summary.StartDate,
            EndDate: summary.EndDate,
            Freshness: "live",
            CompletionRate: summary.CompletionRate,
            CompletionRag: summary.CompletionRag,
            DisruptionRate: summary.DisruptionRate,
            DisruptionRag: summary.DisruptionRag,
            CarryOverRate: summary.CarryOverRate,
            CarryOverRag: summary.CarryOverRag,
            HealthScore: summary.HealthScore,
            HealthRag: summary.HealthRag,
            AddedMidSprint: addedMidSprint,
            AddedMidSprintOverflow: overflow);
    }

    /// <summary>
    /// Rule 6: scope additions shown only when DisruptionRag is amber or worse (not green).
    /// Limited to <see cref="ScopeAdditionsDisplayLimit"/> entries.
    /// Overflow is the count of top-5-by-SP entries not displayed (Fokus truncates server-side at 5).
    /// </summary>
    private static IReadOnlyList<ScopeAdditionDto>? BuildScopeAdditions(
        AnonymizedSprintSummary summary, out int? overflow)
    {
        overflow = null;

        if (summary.DisruptionRag == "green")
            return null;

        var top = summary.TopScopeAdditions;
        var displayed = top.Take(ScopeAdditionsDisplayLimit)
            .Select(e => new ScopeAdditionDto(e.TicketKey, e.Summary, e.StoryPoints))
            .ToList();

        // Overflow: how many among the top-5-by-SP are not shown.
        // "+N more among the top-5-by-SP" — not the true total (Fokus truncates at 5).
        overflow = Math.Max(0, top.Count - ScopeAdditionsDisplayLimit);
        return displayed;
    }

    private async Task<SprintContextSection> BuildFallbackSectionAsync(
        int fokusSprintId, CancellationToken ct)
    {
        var snapshot = await snapshotRepository.GetByIdAsync(fokusSprintId, ct);

        if (snapshot is not null)
        {
            // "cached": snapshot exists — return name/state, no metric numbers (PC3: snapshot stays thin).
            // Dates are absent from FokusSprintSnapshot; RA5 revisit if the UI needs offline dates.
            return new SprintContextSection(
                SprintId: fokusSprintId,
                SprintName: snapshot.SprintName,
                StartDate: null,
                EndDate: null,
                Freshness: "cached",
                CompletionRate: null, CompletionRag: null,
                DisruptionRate: null, DisruptionRag: null,
                CarryOverRate: null, CarryOverRag: null,
                HealthScore: null, HealthRag: null,
                AddedMidSprint: null, AddedMidSprintOverflow: null);
        }

        // "unavailable": neither live pull nor snapshot — sprint was never synced
        return new SprintContextSection(
            SprintId: fokusSprintId,
            SprintName: null,
            StartDate: null,
            EndDate: null,
            Freshness: "unavailable",
            CompletionRate: null, CompletionRag: null,
            DisruptionRate: null, DisruptionRag: null,
            CarryOverRate: null, CarryOverRag: null,
            HealthScore: null, HealthRag: null,
            AddedMidSprint: null, AddedMidSprintOverflow: null);
    }

    /// <summary>
    /// Orders sections: live sections (with StartDate) ascending by start date;
    /// cached/unavailable sections (no StartDate) sorted last, then by SprintId ascending
    /// for determinism. The plan's resolution for sections without a date (PC3).
    /// </summary>
    private static IReadOnlyList<SprintContextSection> OrderSections(
        IEnumerable<SprintContextSection> sections)
    {
        return [.. sections.OrderBy(s => s.StartDate.HasValue ? 0 : 1)
                           .ThenBy(s => s.StartDate)
                           .ThenBy(s => s.SprintId)];
    }
}
