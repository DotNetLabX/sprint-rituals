# Normalized Capacity Indicator — Review

## Reviewed By

`reviewer` (Sonnet agent) — Sonnet-only review. Codex cross-validation was not requested.

**Cycle 2 re-review:** `reviewer` (Sonnet agent) — Fix verification only. Verified the single HIGH finding from cycle 1.

## Verdict: APPROVE

## Pre-commitment Predictions

1. **`normalizedTotalSp` type narrowing complexity** — Predicted possible type cast issues. Actual: cast to structural type is safe; `LeaderboardDeveloperSprintBreakdown` has both fields. No issue.
2. **BR6 multi-sprint normalization correctness** — Predicted average-then-normalize vs per-sprint-then-average confusion. Actual: all three surfaces correctly implement per-sprint-then-average. No issue.
3. **Capacity lookup null handling** — Predicted possible KeyNotFoundException. Actual: all sites use `TryGetValue` pattern. No issue.
4. **`ComputeLeaderboard` parameter threading** — Predicted parameter plumbing errors. Actual: `allDevelopers` correctly passed alongside `capacityLookup` and `sprintId` through `ComputeSummary` → `ComputeLeaderboard`. No issue.
5. **Tooltip text exactness** — Predicted text mismatch. Actual: all `(~X)` spans use the exact text from `help.tooltips.md`. No issue.

---

## Findings

### [HIGH] KB leaderboard entry missing normalization business rules

**File:** `docs/kb/analytics/leaderboard.md`
**Issue:** The `kb-maintenance.md` rule explicitly states: "Reviewer: Verifies KB entries match the implementation. Flag stale or missing entries as HIGH severity." This feature adds `capacityPercent` to all leaderboard response records and introduces normalization logic (formula, display threshold, capacity resolution, multi-sprint BR6 rule), but `docs/kb/analytics/leaderboard.md` was not updated. Missing: (1) `capacityPercent` field on all three response records, (2) normalization formula (`spCompleted / (effectiveCapacity / 100)`, rounded), (3) display threshold (suppressed at 100% and 0% capacity), (4) multi-sprint rule (per-sprint normalize then average, not average-then-normalize), (5) capacity resolution (sprint-specific override, then developer default, then 100). The `implementation.md` only lists `docs/kb/frontend-map.md` as updated — the leaderboard analytics KB entry was skipped.
**Fix:** Add a `## Normalized SP Indicator` section to `docs/kb/analytics/leaderboard.md` covering the five items above.

---

## Positive Observations

- **`GetCapacity` helper pattern matched correctly.** `LeaderboardService.GetCapacity` (lines 230-243) is a direct match to `DeveloperThroughputService.GetCapacity` as the plan specified, including `TryGetValue` double-key lookup and `DefaultCapacityPercent ?? 100` fallback.
- **Capacity lookup construction is consistent.** All three sites (GetLeaderboardEndpoint, GetSprintSummaryEndpoint, and the existing DeveloperThroughputService) use the same `GroupBy(DeveloperAccountId).ToDictionary(sprintId → capacityPercent)` pattern.
- **BR6 implemented correctly on all three surfaces.** `avgNormalizedSpCompleted` in DevelopersView.vue checks `anyReduced` before calculating, computes per-sprint normalization, then averages — exactly what the spec requires. `normalizedTotalSp` in LeaderboardTable.vue applies the same per-sprint approach. Neither regresses to average-then-normalize.
- **Type guard `isSingleEntry` is correct.** Uses `'delta' in dev` — safe because `delta` is always in the `LeaderboardDeveloperSingleEntry` object shape (even when null) and never in `LeaderboardDeveloperEntry`.
- **Tooltip text is exact.** All `(~X)` spans across DevelopersView.vue, LeaderboardTable.vue, and DashboardView.vue use `title="Estimated SP at full capacity. Shows what this developer's output would look like at 100% availability."` — matching `help.tooltips.md` character for character.
- **InfoTooltip added to both column headers** (single-sprint "SP Completed" and multi-sprint "Avg SP Completed") as specified in Step 9.
- **Dashboard features/bugs toggle handled cleanly.** The normalized indicator references the same ternary expression as the raw SP display — no duplication, no toggle-specific branch.
- **`ComputeLeaderboard` inline capacity resolution** vs extracted helper — inline is correct given this is a private static method with all required parameters already in scope. Noted as a valid deviation in lessons.md.
- **Build: clean.** 0 errors, 2 pre-existing NU1903 vulnerability warnings unrelated to this feature.

---

## Gaps

- **No test evidence for key calculation scenarios.** The plan's testing strategy lists 7 specific test cases. No evidence these were exercised. This is an observation, not a blocker — the codebase has no automated unit tests; manual verification is the project pattern.
- **`normalizedTotalSp` in multi-sprint shows average per-sprint normalized value next to a total SP sum.** The plan acknowledges this is the BR6-mandated approach. It is architecturally decided, not a code error. However, the label "Total SP" and the bracket showing an averaged value could confuse users (raw = sum, bracket = average). Logged as open question.

---

## Open Questions

- **Multi-sprint leaderboard: bracket shows average vs total.** For multi-sprint, `totalSp` is the sum across selected sprints (e.g. "25.0") but the `(~X)` bracket shows the per-sprint-normalized average (e.g. `(~9)` meaning 9 per sprint on average at full capacity). This is spec-mandated (BR6) and architect-approved, but the unit mismatch may cause user confusion. Worth revisiting in a future iteration.

---

## Evidence

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Build | PASS | `dotnet build Fokus.API.csproj` | 0 errors, 2 pre-existing NU1903 warnings |
| TypeScript | PASS | `npx tsc --noEmit` (per developer lessons.md) | No output = zero errors |
| Tooltip text match | PASS | Manual grep across 3 files | Exact match to `help.tooltips.md` text on all `(~X)` spans |
| BR6 compliance | PASS | Code review of `avgNormalizedSpCompleted`, `normalizedTotalSp` | Per-sprint normalize then average on all multi-sprint surfaces |
| Capacity lookup null-safety | PASS | Code review of all 3 call sites | All use `TryGetValue` double-key pattern, no KeyNotFoundException risk |
| KB leaderboard.md | FAIL | `docs/kb/analytics/leaderboard.md` read | No normalization content added |
| InfoTooltip wiring | PASS | Grep `InfoTooltip` in DevelopersView.vue | Present on both "SP Completed" and "Avg SP Completed" headers |

---

## Cycle 2 Re-Review — Fix Verification

**Fix applied:** `docs/kb/analytics/leaderboard.md` — Added `## Normalized SP Indicator` section.

Verified against the five items required by the HIGH finding:

| Required item | Present | Location in KB |
|---|---|---|
| `capacityPercent` field referenced on response records | YES | Lines 53, 60, 62, 68, 70 — referenced throughout the section |
| Normalization formula (`round(spCompleted / (effectiveCapacity / 100))`) | YES | Line 47 — exact formula |
| Display threshold (suppressed at 100% and at 0% capacity) | YES | Line 53 — "Only shown when `capacityPercent < 100` (and `capacityPercent > 0`)" |
| Multi-sprint rule: per-sprint normalize then average, not average-then-normalize | YES | Lines 68-70 — explicit statement and rationale |
| Capacity resolution: sprint-specific → developer default → fallback 100 | YES | Lines 59-63 — three-priority list matching `DeveloperThroughputService` |

All five items confirmed present and accurate. HIGH finding is resolved.

**Cycle 2 KB check result:** PASS
