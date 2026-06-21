# FeatureOnlyMetrics — Implementation

## Files Created

None.

## Files Modified

- `src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs` — Added `BugSpCompleted` field to `MetricsResult` record; added `FeatureCompleted` and `BugSpCompleted` fields to `SprintMetrics` record; updated `ComputeMetrics` to compute `featureCommitted`, `featureCompleted`, and `bugSpCompleted` (feature-only completion rate uses `featureCommitted`/`featureCompleted`, disruption rates still use total `committed`); updated `ComputeSpCompleted` helper to exclude bugs (`!IsBug`); updated `MetricsResult` construction to use `selected.FeatureCompleted` for SP Completed card and pass `selected.BugSpCompleted`.

- `src/Services/Fokus/Fokus.API/Features/Analytics/ScopeChangeService.cs` — Made `BuildBurnupData` feature-only for: `startingCommitted`, `startingTotalScopeTickets`, `addedToday`, `removedToday`, `addedTicketsToday`, `removedTicketsToday`, `completedToday`, `completedTicketsToday` — all now filter `m.Ticket?.IssueType != "Bug"`. Bug SP area variables (`startingBugSp`, `bugAddedToday`, etc.) unchanged.

- `src/Services/Fokus/Fokus.API/Features/Analytics/DeveloperThroughputService.cs` — Added `IsBug` helper; filtered `spAssigned`, `spCompleted`, `ticketsDone`, `ticketsCarriedOver` with `!IsBug`; same filter applied to prior sprint delta computations; `ComputeRollingAverage` now excludes bugs from SP completed sum.

- `client/src/types/index.ts` — Added `bugSpCompleted: number` to `MetricsResult` interface.

- `client/src/components/dashboard/MetricCard.vue` — Added optional `annotation?: string` prop; renders annotation in `text-xs text-text-muted` below display value when present; updated SP Completed tooltip text to mention feature-only context.

- `client/src/views/DashboardView.vue` — Added `bugSpAnnotation` computed that returns `"(+X bug SP)"` when `bugSpCompleted > 0`, otherwise `undefined`; passed `:annotation="bugSpAnnotation"` to the SP Completed `MetricCardComponent`.

- `client/src/components/sprints/BurnupChart.vue` — Changed series name and tooltip row name from `'Total Scope SP'` to `'Scope SP'`.

- `docs/kb/analytics/health-score.md` — Updated Completion % formula to show feature-only inputs; noted disruption and carry-over remain total-scope.

- `docs/kb/analytics/scope-change.md` — Added feature-only note to Burnup Chart section; noted multi-sprint fields remain total-scope.

- `docs/kb/analytics/throughput.md` — Updated all formula descriptions to note `!IsBug` filter; updated rolling average note.

- `docs/kb/cross-cutting.md` — Added "Feature-Only Metrics (FeatureOnlyMetrics)" section documenting: which surfaces are feature-only, which remain total-scope, the filtering rule, the two-committed-SP pattern, and the `bugSpCompleted` annotation field.

## Key Decisions

- `SprintMetrics` record keeps `SpCompleted` as `featureCompleted` (same field, now feature-only value) — consistent with sparkline helper which also became feature-only. No rename needed since the record is private.
- `bugSpAnnotation` returns `undefined` (not empty string) when bug SP is zero so the `v-if` in the template hides it cleanly.
- Step 7 (DevelopersView verification): column header title attributes don't reference "all tickets" so no tooltip updates needed.

## Deviations from Plan

None. All 8 steps implemented as specified. Step 2 confirmed as verification-only: `ComputeHealthScore` consumes `metrics.CompletionRate` from `SprintMetrics`, which is now feature-only after Step 1 — no additional code change required.
