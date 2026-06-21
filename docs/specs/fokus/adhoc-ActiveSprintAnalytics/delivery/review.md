# ActiveSprintAnalytics — Review

## Reviewed By
reviewer (Sonnet agent) — Cycle 1 initial review + Cycle 1 fix re-review

## Verdict: APPROVE

---

## Cycle 1 Re-review (fix verification)

All four findings from the initial review are resolved. Evidence below.

### [CRITICAL] FIXED — GetQaMetrics active sprint crash

`GetQaMetricsEndpoint.cs:52-61`: When `closedIndex < 0` (active sprint), the endpoint now loads the sprint separately via `GetSprintsWithMembershipsAsync([req.SprintId])` and assigns it as `selectedSprint`. The `.First()` on `windowSprints` is now guarded by the `closedIndex >= 0` branch.

Line 67: `transitionIds` correctly includes `req.SprintId` when active, so status transitions for the active sprint's tickets are loaded.

Line 80: Prior sprint for active case: `closedIndex < 0 && sortedWindow.Count > 0 ? sortedWindow[^1] : null` — last closed sprint in window, correct.

Lines 93–95: Sparkline base for active case: `sortedWindow.TakeLast(4)` (all closed window sprints), correct — avoids the `Take(0)` empty window.

No remaining logic paths crash or produce an empty sparkline when an active sprint is requested.

### [HIGH] FIXED — GetQaWorkload single-sprint rejects active sprint

`GetQaWorkloadEndpoint.cs:44`: `HandleSingleSprintAsync` signature now takes `allSprints` as an additional parameter.

`GetQaWorkloadEndpoint.cs:114`: Lookup changed to `allSprints.FirstOrDefault(s => s.Id == req.SprintId!.Value)` — active sprint is found.

`GetQaWorkloadEndpoint.cs:138`: `targetIndex` is computed against `ascending` (closed only), so -1 when active.

`GetQaWorkloadEndpoint.cs:143-145`: Prior sprint computed with the same three-way pattern used in other Category D endpoints: `targetIndex > 0` → prior in closed list; `targetIndex < 0 && ascending.Count > 0` → last closed sprint; otherwise null.

`GetQaWorkloadEndpoint.cs:160`: Sparkline anchor: `targetIndex >= 0 ? targetIndex : ascending.Count - 1` — full closed history used as candidate set when active. Correct.

Alert baseline (lines 176–185): still loads from `ascending` (closed only). Correct.

### [MEDIUM] FIXED — KB updated

`docs/kb/domain/sprint.md` now has an "Analytics Query Scope" section (lines 43–64) documenting: `GetAnalyticsSprintsAsync` scope (Active + Closed, no Future), the closed-only guard rule with the canonical code pattern, active-sprint prior-sprint resolution, sparkline anchor behaviour, and the single-sprint lookup rule. Content is accurate and matches implementation.

### [LOW] FIXED — SprintEditDialog case corrected

`client/src/components/sprints/SprintEditDialog.vue:26`: `props.sprint.state === 'Closed'` (capital C). Matches backend serialisation.

---

## Pre-commitment Predictions (Cycle 1)

| Prediction | Outcome |
|---|---|
| Closed-only guard inconsistency across 9 endpoints | Mostly consistent — two endpoints broken (both fixed) |
| `ascending[^1]` throws when no closed sprints exist | Guarded correctly throughout |
| Error message update missed on one of the 9 endpoints | All 9 updated correctly |
| `SyncTab.vue` still references old sprint type | Clean — no sprint types referenced |
| `useQualityAverages.ts` needs client-side guard | Clean — server-provided breakdowns only |

---

## Findings

No outstanding findings. All Cycle 1 findings resolved.

---

## Positive Observations

- The closed-only guard pattern is applied correctly and consistently across all 9 Category C/D endpoints after fixes.
- `GetQaMetrics` fix uses the same sparkline-anchor and prior-sprint patterns established by `GetSprintSummaryEndpoint` and `GetDeveloperQualityEndpoint` — no new patterns invented.
- `GetQaWorkload` fix correctly keeps the sparkline candidate set and alert baseline closed-only while widening only the initial lookup.
- KB entry is precise and actionable — documents the exact code pattern agents should replicate, not just prose.
- Frontend rename remains thorough across all stores, views, and components.
- Backend build: 0 errors, 13 pre-existing warnings (all in `QaWorkloadService.cs`, unrelated to this feature).

---

## Gaps

None remaining.

---

## Open Questions

None.

---

## Evidence

| Check | Result | Command | Output |
|---|---|---|---|
| Backend build (cycle 1 initial) | PASS | `dotnet build Fokus.API.csproj -o tmp-review-build --nologo -v quiet` | 0 errors, 13 pre-existing warnings |
| Frontend type-check (cycle 1 initial) | PASS | `npx vue-tsc --noEmit` | No output (clean) |
| Old names removed (frontend) | PASS | Grep across `client/src/` | No matches |
| Old folder deleted | PASS | `Test-Path GetClosedSprints/` | False |
| Old method names removed (backend) | PASS | Grep across `src/` | No source file matches |
| Backend build (cycle 1 re-review) | PASS | `dotnet build Fokus.API.csproj -o tmp-review-build --nologo -v quiet` | 0 errors, 13 pre-existing warnings |
| GetQaMetrics active-sprint fix | PASS | Read endpoint lines 52–95 | Active sprint loaded separately; sparkline and prior correct |
| GetQaWorkload single-sprint fix | PASS | Read endpoint lines 104–201 | Lookup uses allSprints; anchor pattern correct |
| KB update | PASS | Read `docs/kb/domain/sprint.md` lines 43–64 | Analytics Query Scope section present and accurate |
| SprintEditDialog case fix | PASS | Read line 26 | `=== 'Closed'` (capital C) |
