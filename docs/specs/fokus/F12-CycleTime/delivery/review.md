# Cycle Time — Review

## Reviewed By
`reviewer` (Sonnet agent). Cycle 1 included Codex cross-validation. Cycle 2 (re-review) is Sonnet-only per protocol.

## Verdict: APPROVE

---

## Pre-commitment Predictions (Cycle 1)

| Predicted area | Actual finding |
|---|---|
| BR18 sprint clamping edge cases | Correctly implemented — enter clamped at sprint start, exit clamped at min(nextTransition, sprintEnd) |
| `last=0` convention alignment | Plan deliberately deviates from spec "400 when last<1" to match scope-change `last=0` pattern — justified and documented |
| Sub-team filter matching `FilterMemberships` pattern | Correctly implemented, matches SprintSummaryService pattern exactly |
| TypeScript types alignment with C# records | Full alignment verified — all 16 types match |
| Tooltip wiring completeness | All help.tooltips.md entries wired via native `title` attributes — complete |
| (not predicted) BR13 empty state condition | HIGH (cycle 1): `hasWorkflowStages` used combined `availableStages` — FIXED in cycle 2 |
| (not predicted) BR21 "Other" bucket | MEDIUM (cycle 1): Unrecognized stages dropped before funnel — FIXED in cycle 2 |

---

## Cycle 2 Fix Verification

### Fix 1 — [HIGH] BR13: `WorkflowStageCount` added to boundary response

**Files changed:**
- `src/Services/Fokus/Fokus.API/Features/Settings/GetCycleTimeBoundaries/GetCycleTimeBoundariesEndpoint.cs` — `WorkflowStageCount` added to `GetCycleTimeBoundariesResponse` record (line 7); `settings.WorkflowStages.Count` passed as fourth argument (line 30)
- `client/src/types/index.ts:728` — `workflowStageCount: number` added to `CycleTimeBoundariesResponse`
- `client/src/stores/cycleTimeStore.ts:23-24` — `hasWorkflowStages` now checks `boundaries.value.workflowStageCount > 0`

**Verdict: RESOLVED.** When `workflowStages = []` but `doneStatuses = ["Done", "Closed"]`, `workflowStageCount == 0`, `hasWorkflowStages == false`, and `CycleTimeView.vue:110` correctly shows the empty state directing the user to configure workflow stages.

---

### Fix 2 — [MEDIUM] BR21: Unrecognized stages accumulated as `__Other__` sentinel

**Files changed:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/CycleTimeService.cs` — When `stageIdx == -1` (stage not in `orderedStages`), time is accumulated under `"__Other__"` key in `stageDurations` (lines 367-387). The existing `BuildStageFunnel` `else` branch (line 582) now receives `"__Other__"` entries since `orderedStages.Contains("__Other__")` is false, accumulating them into `otherSum`. `BuildMultiSprintStageFunnel` at line 645 already guards for `"Other"` in `allStageNames`.

**Verdict: RESOLVED.** The "Other" funnel segment now appears when tickets have transitions to renamed/removed stage names. `BuildStageBreakdown` at line 782 filters by `boundaryStages.Contains(kvp.Key)` — `"__Other__"` is not in `boundaryStages`, so it correctly stays out of per-ticket stage breakdowns in the scatter plot and outlier table. The sentinel key is internal only.

**One side effect noted (LOW, not a blocker):** `totalCycleTime` at line 425 is `stageDurations.Values.Sum()`, which now includes `__Other__` duration. A ticket with time in an unrecognized stage will have that time counted in its cycle time value, percentiles, and outlier detection — even though that time is invisible in the scatter plot stage breakdown. This is consistent with the spec's intent (all measured time within the sprint window counts), but creates a minor presentation gap: the cycle time Y-axis value may not be fully explained by the visible stage breakdown entries. Not a plan deviation, acceptable behavior.

---

## Findings (Cycle 2)

No new findings. All cycle 1 HIGH and MEDIUM findings are resolved.

Cycle 1 LOW findings remain (style/robustness observations, no fix required):
- Spec deviation `last >= 0` vs `last < 1` — justified plan alignment with scope-change convention
- `BuildStageFunnel` redundant recognized-stage condition — harmless
- `PercentileToggle` modelValue has no undefined guard — store always initializes to 85

---

## Positive Observations

- The `WorkflowStageCount` fix is minimal and correct: one field added to the response record, one to the TypeScript type, one condition change in the store computed. No other files touched.
- The `__Other__` sentinel approach is clean: the sentinel key is only meaningful inside `BuildStageFunnel`, which already had the "Other" output logic. No protocol changes, no new types, no API contract changes.
- Both fixes compile cleanly with 0 errors on backend and frontend.
- `BuildMultiSprintStageFunnel` line 645 already guarded for `"Other"` in `allStageNames` — the fix integrates correctly without requiring changes in the multi-sprint path.

---

## Open Questions (carried from cycle 1, no fixes required)

- **Spec `last < 1` vs plan `last >= 0`:** Architect should update the spec to reflect the `last=0 = all sprints` convention now shared by scope-change and cycle-time endpoints.
- **Cycle time includes time in end stage:** By BR2 ("inclusive of the end stage"), time sitting in "Done" is measured. No code change needed unless the product intent changes.
- **Delta null when prior sprint has 0 measurable tickets:** Reasonable interpretation of BR11; not a plan deviation.

---

## Evidence

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Backend build (cycle 2) | **PASS** | `dotnet build src/Services/Fokus/Fokus.API` | 0 errors, 0 build warnings (2 pre-existing NU1903) |
| Frontend build (cycle 2) | **PASS** | `npm run build` (client/) | 0 TypeScript errors, 151 modules, clean Vite output |
| Fix 1: WorkflowStageCount field | **PASS** | Read endpoint + types + store | Correct field added at all three layers |
| Fix 1: hasWorkflowStages condition | **PASS** | `cycleTimeStore.ts:24` | `workflowStageCount > 0` (not `availableStages.length > 0`) |
| Fix 2: __Other__ accumulation path | **PASS** | `CycleTimeService.cs:367-387` | `stageIdx == -1` branches to sentinel accumulation |
| Fix 2: BuildStageFunnel "Other" output | **PASS** | `CycleTimeService.cs:580-599` | `else` branch receives `__Other__`, emits "Other" funnel entry |
| Fix 2: BuildStageBreakdown excludes __Other__ | **PASS** | `CycleTimeService.cs:782` | `boundaryStages.Contains(kvp.Key)` — `__Other__` correctly excluded |
| Fix 2: Multi-sprint "Other" propagation | **PASS** | `CycleTimeService.cs:645` | `allStageNames.Contains("Other")` guard already present |

## Cross-Validation (Codex — Cycle 1 only)

Codex independently reviewed the implementation in cycle 1. Reconciliation:

**Agreed:** `last=0` vs spec `last<1=400` — both assessed as justified plan deviation, not a code bug.

**Codex HIGH — investigated and demoted:** "Cycle time accumulates done-state time after completion date." After re-reading plan BR2 ("inclusive of the end stage") and spec BR2 ("stops when it first enters a stage beyond the end stage"), this is by design. Downgraded to Open Question.

**Codex MEDIUM → HIGH confirmed:** BR13 empty state. Confirmed real bug. Fixed in cycle 2.

**Codex MEDIUM confirmed:** BR21 "Other" bucket unreachable. Confirmed real bug. Fixed in cycle 2.

**Codex MEDIUM → Open Question:** Delta null when prior sprint has 0 tickets. Reasonable interpretation of BR11; not escalated.
