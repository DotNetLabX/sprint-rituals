# Cross-Sprint QA Trends — Review

## Reviewed By
reviewer (Sonnet agent)

## Verdict: APPROVE

## Pre-commitment Predictions

| Prediction | Outcome |
|------------|---------|
| Pearson r denominator guard and <6-point null may be subtly wrong | NOT FOUND — both guards implemented correctly |
| N+1 extended sprint range for ComputeMultiSprint may be incomplete | NOT FOUND — extended sprint loading is correct |
| DefectCorrelationSection empty state conditions partially implemented | NOT FOUND — all three empty state paths present |
| AppSidebar fetchSettings race condition | LOW RISK — fetch is defensive, store is reactive |
| RAG discrete markers ApexCharts reactivity bug | FOUND (LOW) — see finding below |

---

## Findings

### [LOW] `discreteMarkers.value` accessed inside a sibling `computed()` — stale on prop change

**File:** `client/src/components/qa/QualityTrendsChart.vue:79`

**Issue:** The `options` computed reads `discreteMarkers.value` explicitly via `.value` instead of referencing `discreteMarkers` as a reactive dependency. Inside a `computed()` callback, Vue tracks reactive dependencies accessed directly. Using `.value` inside a `computed()` is correct syntax and IS tracked — Vue reactivity does unwrap refs inside computed getters. However, reading `discreteMarkers.value` rather than `discreteMarkers` as a ref is fine because `discreteMarkers` is itself a `ComputedRef`, and accessing `.value` inside another `computed()` correctly establishes the dependency chain. On inspection this is actually correct Vue 3 behavior: `computed()` getters track any `.value` access on a reactive ref/computed. No actual bug, but the idiom is slightly unusual and worth a note.

**Fix:** No fix required — this is valid Vue 3 reactivity. The pattern is correct.

*Self-audit: downgraded from MEDIUM to LOW after confirming Vue 3 tracks `.value` access inside computed getters correctly.*

---

### [LOW] `getQaTrends` sends `last=0` when user selects "All" — minor spec/plan discrepancy with no runtime impact

**File:** `client/src/api/analytics.ts:152`, `client/src/stores/qaTrendsStore.ts:47`

**Issue:** The spec (`spec.md:100`) says "All is expressed by omitting the `last` parameter." The implementation sends `last=0` when the user selects "All." However, the plan explicitly overrides this: "Convention: `last=0` means 'all' (frontend sends 0 when user picks 'All', omit the param on the backend side)" (plan.md Step 3, API function section). The backend handles both `last == null` and `last == 0` as "all" (`GetQaTrendsEndpoint.cs:66`). The deviation is documented in the plan and the backend handles both cases. No runtime impact.

**Fix:** No code fix required. The plan intentionally chose `last=0` over omitting the param, and the backend supports it. Acceptable deviation with justification.

---

### [LOW] `QaTrendsView` does not handle the initial loading state gap between `qaTrends === null` and `initializing === false`

**File:** `client/src/views/QaTrendsView.vue:66-108`

**Issue:** The view shows a loading spinner while `store.initializing` is true. After `initializing` completes, the view checks `store.qaTrends?.hasQaData`. But between the start of the component mount and the first time `initialize()` resolves, `qaTrends` is `null`. If `initializing` is `false` AND `qaTrends` is `null` (which can only occur momentarily before the first fetch, or if initialize failed silently), neither the loading state, the `hasQaData === false` branch, nor the `hasQaData === true` branch matches — the view renders nothing. In practice, `fetchTrends()` is always called inside `initialize()`, so this is transient and recoverable. The error branch is not explicitly rendered (only `error` ref is set), but this is a low-risk gap.

**Fix:** Consider adding `v-else` after the `hasQaData` branches to render an error message when `store.error` is set, or to handle the `qaTrends === null` case. Low priority.

---

## Positive Observations

- **Plan conformance is complete.** Every plan step (1–7) has a corresponding implementation entry. No plan steps are silently skipped.

- **QaTrendsService is cleanly structured.** Records defined above the service class, pure computation with no I/O, private helpers extracted clearly. This follows the BugRatioService/QaMetricsService pattern exactly as specified.

- **Pearson r implementation is correct.** The standard formula matches plan BR12 exactly. The denominator zero guard (`if (denominator == 0) return null`) handles the all-identical-values edge case. The `n < 6` null guard is in the right place (before computation, not after).

- **N+1 correlation data loading is well-reasoned.** The endpoint correctly identifies the sprint one index beyond the target range in `ascending` (the full closed sprint list) and includes it in `extendedTargetSprints` for `ComputeMultiSprint`. The `bugRatioBySprintId` dictionary is built from the extended result. This matches the plan's intent.

- **RAG coloring uses `ExecutionGreen`/`ExecutionAmber` for execution rate** (`QaTrendsService.cs:97`), matching the threshold structure that exists in `AppSettings.QaHealthThresholds`. Consistent with `QaMetricsService` and `DeveloperQualityService`.

- **Sidebar implementation is clean.** Split into `baseNavItems`, `qaNavItem`, and `tailNavItems`, then assembled in a `computed()` property gated on `settingsStore.settings.xrayEnabled`. The `fetchSettings()` call on `onMounted` is a defensively correct addition. QA entry is placed correctly between Epics and Cycle Time.

- **All three DefectCorrelation empty state paths are implemented** in `emptyMessage` computed: `null/dataPointCount===0`, `pearsonR===null && dataPointCount<6`, and the "show charts" path. Matches plan Step 6 spec exactly.

- **TypeScript check passes** (zero errors, no output from `npx tsc --noEmit`).

- **Backend build passes** (zero errors, 13 pre-existing warnings all in `QaWorkloadService`, none in new code).

- **All tooltip text matches `help.tooltips.md`.** Each tooltip string in the Vue components was verified against the tooltip file.

- **KB updated.** `docs/kb/frontend-map.md` has the `QaTrendsView` row as specified.

- **Router placement is correct.** `/qa` route is before `/settings` as specified in plan Step 7.

- **Sub-team filtering in DefectCorrelationSection uses `nextSprintName`** as the X-axis label for bug ratio panel, correctly showing the N+1 offset rather than the coverage sprint name.

---

## Gaps

- No explicit error state rendering in `QaTrendsView` when `store.error` is set. The `error` ref is populated in both `initialize()` and `fetchTrends()` catch blocks but not surfaced to the user in the template. This is a minor UX gap but not a plan requirement.

- The `QualityTrendsChart` renders nothing (empty `<div>`) when `qualityTrends.length === 0` because of the `v-if`. Since the parent view already handles `hasQaData === false`, this is safe — the chart will never receive an empty array when visible.

---

## Open Questions

None. All findings reviewed with high confidence. No findings moved here from self-audit.

---

## Evidence

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Backend build | PASS | `dotnet build Fokus.API.csproj --no-incremental -v quiet` | 0 errors, 13 pre-existing warnings |
| Frontend TypeScript | PASS | `npx tsc --noEmit` | No output (zero errors) |
| Plan step conformance | PASS | Manual trace of all 7 steps | All steps have implementation entries |
| KB update | PASS | `docs/kb/frontend-map.md` | QaTrendsView row present |
| Tooltip text | PASS | Compared Vue components vs `help.tooltips.md` | All tooltips match |
| Pearson r formula | PASS | `QaTrendsService.cs:186-192` | Formula matches plan BR12 exactly |
| RAG thresholds | PASS | `QaTrendsService.cs:96-98` | Uses `ExecutionGreen`/`ExecutionAmber` correctly |
| Sidebar gating | PASS | `AppSidebar.vue:98-105` | `xrayEnabled` check in computed navItems |
| Route placement | PASS | `router.ts:56-59` | `/qa` before `/settings` |
