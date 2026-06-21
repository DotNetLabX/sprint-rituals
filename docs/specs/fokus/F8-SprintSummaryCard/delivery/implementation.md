# Sprint Summary Card — Implementation

## Files Created

- `src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs` — All response record types (SprintSummaryResponse hierarchy) plus the SprintSummaryService computation engine: sub-team filtering, core metrics, health score with interpolation, delta, sparkline, top epics, leaderboard, and flags.
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetSprintSummary/GetSprintSummaryQuery.cs` — Request type and validator for the sprint summary endpoint.
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetSprintSummary/GetSprintSummaryEndpoint.cs` — GET /api/analytics/sprint-summary endpoint with full orchestration logic.
- `src/Services/Fokus/Fokus.API/Features/Sprints/GetClosedSprints/GetClosedSprintsQuery.cs` — ClosedSprintItem response type.
- `src/Services/Fokus/Fokus.API/Features/Sprints/GetClosedSprints/GetClosedSprintsEndpoint.cs` — GET /api/sprints/closed endpoint.
- `src/Services/Fokus/Fokus.API/Features/Developers/GetSubTeams/GetSubTeamsQuery.cs` — Placeholder file (no request type needed).
- `src/Services/Fokus/Fokus.API/Features/Developers/GetSubTeams/GetSubTeamsEndpoint.cs` — GET /api/developers/sub-teams endpoint.
- `client/src/api/analytics.ts` — API module with getSprintSummary, getClosedSprints, getSubTeams functions.
- `client/src/stores/dashboardStore.ts` — Pinia store managing sprint selection, sub-team filter, summary data, and loading states.
- `client/src/components/dashboard/HealthScoreBadge.vue` — Composite health score display with RAG color circle and sub-metric breakdown.
- `client/src/components/dashboard/MetricCard.vue` — Single metric card with big number, delta arrow, and ApexCharts sparkline.
- `client/src/components/dashboard/SprintFlags.vue` — Flags section: zombie tickets, mid-sprint disruption, zero-SP developers, and "no flags" positive state.

## Files Modified

- `src/Services/Fokus/Fokus.Persistence/Repositories/SprintRepository.cs` — Added GetClosedSprintsAsync, GetSprintWithMembershipsAsync, GetSprintsWithMembershipsAsync.
- `src/Services/Fokus/Fokus.Persistence/Repositories/DeveloperRepository.cs` — Added GetDistinctSubTeamsAsync, GetActiveDevelopersAsync.
- `src/Services/Fokus/Fokus.Persistence/GlobalUsings.cs` — Added `global using Jira.Contracts;` to expose SprintState enum in Persistence project.
- `src/Services/Fokus/Fokus.API/DependencyInjection.cs` — Registered SprintSummaryService as scoped; added using for Analytics namespace.
- `client/src/types/index.ts` — Added all sprint summary TypeScript interfaces (SprintSummaryResponse hierarchy plus ClosedSprintItem).
- `client/src/components/PageToolbar.vue` — Replaced static visual stubs with functional dropdowns; added props (sprints, selectedSprintId, subTeams, selectedSubTeam, showSubTeamFilter, showAggregateOptions) and emit events (update:selectedSprintId, update:selectedSubTeam).
- `client/src/views/DashboardView.vue` — Complete rewrite: initializing/empty/summary conditional rendering, sprint header, health badge, 2x2 metric grid, top epics with progress bars, developer leaderboard, flags card. URL sync via route query param.

## Key Decisions

- `SprintState` enum lives in `Jira.Contracts` (not `Fokus.Domain`). The Persistence GlobalUsings only had `Fokus.Domain` so `SprintState` was not visible. Added `global using Jira.Contracts;` — the transitive assembly reference already existed.
- All response record types are co-located in `SprintSummaryService.cs` per the plan's file layout instruction. No separate DTOs file.
- `GetSprintWithMembershipsAsync` was defined per plan but not called by the endpoint (the endpoint uses `GetSprintsWithMembershipsAsync` for the bulk window load). It's available for future single-sprint use cases.
- Health score interpolation floors at 0 and caps at 100 per the plan's formulas; `Math.Max(0, ...)` guards added.
- The sparkline window is computed using `TakeLast(4)` on the ascending-sorted closed sprint list, sliced up to and including the selected sprint index.
- URL sync in DashboardView sets `selectedSprintId` before calling `initialize()` so the first fetch uses the URL-specified sprint rather than defaulting to most recent.

## Deviations from Plan

- Step 3 plan said the validator should validate `SubTeam` is non-empty when provided; implemented as `NotEmpty().When(x => x.SubTeam is not null)` — aligns with plan intent (empty string treated as null/omitted).
- `GetSubTeamsQuery.cs` created as a comment-only placeholder file because no request or response type is needed beyond `List<string>`; kept consistent with the plan's two-file pattern for each endpoint feature.
