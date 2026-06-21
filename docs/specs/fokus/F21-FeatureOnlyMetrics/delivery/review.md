# FeatureOnlyMetrics — Review

## Reviewed By

`reviewer` (Sonnet agent)

## Verdict: APPROVE

## Pre-commitment Predictions

1. **`SprintMetrics` record redundancy** — Predicted `SpCompleted` field would receive the same value as `FeatureCompleted`. Confirmed: both receive `featureCompleted` at line 284. Dead field in a private record — MEDIUM.
2. **Frontend label vs field name confusion** — Predicted `totalScopeSp` field might get renamed. Confirmed NOT renamed: field name stays, only display label changes. Correct approach.
3. **Bug annotation decimal formatting** — Predicted `bugSpCompleted` might lack rounding. Confirmed: MetricsResult construction at line 173 passes raw unrounded sum, unlike all MetricCard values which go through `BuildMetricCard`/`Math.Round`. LOW.
4. **`removedToday` missing bug exclusion** — Predicted possible omission. Confirmed correctly applied at ScopeChangeService.cs lines 461-465.
5. **Double computation in sparkline helpers** — `ComputeCompletionRate` calls `ComputeMetrics` internally; matches existing `ComputeCarryOverRate` pattern. Not a new pattern; acceptable.

## Findings

### MEDIUM: `SprintMetrics.SpCompleted` is a dead field holding a duplicate value

**File:** `src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs:223-225, 284`

**Issue:** The private `SprintMetrics` record declares `decimal SpCompleted` (position 2). At construction time (line 284), it receives `featureCompleted` — the same value also passed as `FeatureCompleted` (position 10). `SpCompleted` is never read after construction: all downstream usages reference `selected.FeatureCompleted` and `prior.FeatureCompleted`. The `SpCompleted` field is therefore dead code.

**Fix:** Either remove `SpCompleted` from the `SprintMetrics` record entirely, or keep it with a meaningful distinct value (e.g., total completed including bugs). As a private record, this is a zero-risk rename — no public API impact. Recommended: remove it or document why it's retained.

### LOW: `BugSpCompleted` value not rounded before serialization

**File:** `src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs:173`

**Issue:** `BugSpCompleted: selected.BugSpCompleted` passes the raw computed sum with no `Math.Round`. Every other numeric field in `MetricsResult` is either routed through `BuildMetricCard` (which calls `Math.Round(value, 1)`) or explicitly rounded. If effective SP values produce fractional sums (e.g., a default of 3 SP per bug applied to fractional counts), the JSON field could serialize as `9.123456...` rather than `9.1`. The frontend renders it directly in the annotation string without formatting: `(+${bugSp} bug SP)`.

**Fix:** Wrap the value: `BugSpCompleted: Math.Round(selected.BugSpCompleted, 1)`.

## Positive Observations

- **Two-committed-SP pattern is clean.** `committed` (total) and `featureCommitted` are maintained as distinct locals in `ComputeMetrics` with clear comments. The disruption rates correctly continue dividing by total committed, exactly as spec BR4a requires.
- **`IsBug` helper added to `DeveloperThroughputService` with a comment noting it mirrors `SprintSummaryService`.** Good cross-reference that aids future maintenance.
- **All eight plan steps are implemented with no missing steps.** Step 2 was correctly identified as a verification-only pass. Step 7 was correctly confirmed as requiring no frontend changes.
- **`bugSpAnnotation` returns `undefined` (not `""`) when zero** — the `v-if="annotation"` in the MetricCard template correctly hides it. Clean null-object pattern.
- **Burnup bug exclusion order is correct** — `!IsExcluded` is applied before `!IsBug` throughout `BuildBurnupData`, satisfying spec BR11 ("excluded-from-scope filtering applies before bug/feature split").
- **`ComputeCompletionRate` sparkline helper delegates to `ComputeMetrics`** rather than re-implementing the formula, ensuring the sparkline is always consistent with the card value.
- **KB updates are thorough and accurate.** The `cross-cutting.md` "Feature-Only Metrics" section documents surfaces, the filtering rule, and the two-committed-SP pattern. `health-score.md` correctly notes feature-only Completion % with unchanged disruption/carry-over.

## Gaps

- **No explicit guard on `GetEffectiveSp` result reuse in `ComputeMetrics`.** Each `.Where(...HasValue)` filter is followed by a `.Sum(m => m.GetEffectiveSp()!.Value)` — the method is called twice per membership per LINQ chain. This is the existing pattern throughout the file, so it is not a new issue introduced by this feature. Worth noting as a future optimization target.
- **Spec testing scenarios (§Testing Strategy) are not automated** — all 15 scenarios are manual. This is consistent with the project's current testing approach; not a blocker.

## Open Questions

None. All MEDIUM/LOW findings are clear-cut; no context from the developer would change the assessment.

## Evidence

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Build | **PASS** | `dotnet build Fokus.API.csproj --no-incremental -p:OutDir=...ReviewBuild` | `Build succeeded. 0 Error(s)` |
| Compilation warnings | Pre-existing only | Same command | NU1903 (transitive dep vulnerability, pre-existing), CS9107 in DeveloperRepository (pre-existing) — neither introduced by this feature |
| File lock errors | Not compilation errors | Full slnx build | 18 MSB3027/MSB3021 file-copy errors all caused by running `Fokus.API (36500)` process holding DLL locks — not code errors |
