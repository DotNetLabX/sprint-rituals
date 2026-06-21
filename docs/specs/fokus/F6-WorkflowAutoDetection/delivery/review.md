# Workflow Auto-Detection — Review

## Reviewed By
`reviewer` (Sonnet 4.6 agent)

## Verdict: APPROVE

No CRITICAL or HIGH issues. Three MEDIUM findings, two LOW.

---

## Pre-commitment Predictions

| Prediction | Actual |
|-----------|--------|
| `moveSidelinedToStages` / `removeSidelinedStatus` are identical bodies | Confirmed — both splice from sidelined; `removeSidelinedStatus` is also dead code (no UI trigger) |
| Store action name mismatch (plan says `detectWorkflowStages`, impl may rename) | Confirmed — renamed to `runDetectWorkflowStages` |
| `transitionCount` computation from grouped edges could lose fidelity | Not a bug — `edges.Sum(e => e.Count)` correctly sums occurrence counts from GroupBy |
| Done statuses forced to tail when absent from transition data | Confirmed as an edge-case gap — all statuses sidelined when no terminal can be identified |
| Tie-breaking when `reverseCount == forwardCount` | Confirmed — arbitrary but deterministic; acceptable |

---

## Findings

### [MEDIUM] Store action name deviates from plan

**File:** `client/src/stores/settingsStore.ts:58`
**Issue:** Plan step 5 specifies the store action as `detectWorkflowStages()`. Implementation exports `runDetectWorkflowStages()`. The view calls `store.runDetectWorkflowStages()` consistently, so there is no breakage — but it is an undocumented deviation from the plan's stated interface.
**Fix:** Either rename to `detectWorkflowStages` to match the plan, or note this deviation in implementation.md. As-is the deviation is unmarked ("None" is claimed in Deviations from Plan).

---

### [MEDIUM] `removeSidelinedStatus` is dead code — "dismiss" path has no UI trigger

**File:** `client/src/stores/settingsStore.ts:82-88`, `client/src/views/SettingsView.vue:215-231`
**Issue:** The plan (step 5) defines `removeSidelinedStatus(status)` as a way to dismiss a sidelined status without adding it to stages. The function is implemented and exported. However, the "Other statuses" pill in the view only has a "+" button that calls `moveSidelined`, which calls `store.moveSidelinedToStages`. There is no dismiss/remove button — the dismiss-without-add use case specified in the plan has no UI entry point. `removeSidelinedStatus` is exported but unreachable.
**Fix:** Either add a dismiss button (×) to each sidelined pill that calls `store.removeSidelinedStatus(status)`, or remove the function and note in implementation.md that the dismiss path was intentionally omitted. Currently the plan states it should be present.

---

### [MEDIUM] `reDetect()` overwrites manually-entered stages with empty array when no data

**File:** `client/src/views/SettingsView.vue:50-55`
**Issue:**
```typescript
async function reDetect() {
  await store.runDetectWorkflowStages()
  if (store.detectionResult) {
    form.workflowStages = [...store.detectionResult.stages]  // stages may be []
  }
}
```
When Re-detect is called but no transition data exists, `detectionResult.stages` is `[]`. The condition `if (store.detectionResult)` is satisfied (the result object is non-null), so `form.workflowStages` is replaced with an empty array. A user who had manually typed stages into the form before clicking Re-detect loses them silently. The auto-detect path on mount guards against this (`if (store.detectionResult.stages.length > 0)` at line 44), but `reDetect()` does not apply the same guard.
**Fix:** Apply the same guard: `if (store.detectionResult && store.detectionResult.stages.length > 0)`. Alternatively, only replace form stages when detection returns a non-empty proposal — preserving manual entries when detection has nothing to offer.

---

### [LOW] All statuses sidelined when no configured done status appears in transition edges

**File:** `src/Services/Fokus/Fokus.API/Features/Settings/WorkflowDetectionService.cs:35-36`
**Issue:** `terminalStatuses` is built by intersecting `allStatuses` (from edges) with `doneSet` (from settings). If the configured done statuses (e.g., "Done", "Closed") do not match any status name in the transition data, `terminalStatuses` is empty. The backward-propagation reachability loop has no anchor, so `canReachTerminal` stays empty, and every non-terminal status ends up in `sidelined`. The result is `stages = []`, `sidelined = [all statuses]`. This is technically correct behavior (cannot determine direction without a terminal) but could confuse users who have data and configured done statuses that differ by case or spelling from what Jira returned. Note: the `StringComparer.OrdinalIgnoreCase` on `doneSet` mitigates case differences.
**Fix:** No code change required. Consider adding a note in the confidence summary or a log warning when done statuses don't match any transition status names. This is a product-level edge case best handled in a follow-up.

---

### [LOW] Tie-break in cycle suppression preserves original edge direction arbitrarily when counts are equal

**File:** `src/Services/Fokus/Fokus.API/Features/Settings/WorkflowDetectionService.cs:63-70`
**Issue:** `if (reverseCount <= forwardCount)` suppresses the reverse when counts are equal. When a status pair has identical transition counts in both directions, the ordering depends on lexicographic comparison of status names (which determines which edge is "forward" in the loop). This is deterministic but arbitrary.
**Fix:** No change required. The algorithm is explicitly an approximation. Equal-weight bidirectional pairs are inherently ambiguous; the current behavior is acceptable.

---

## Positive Observations

- The three-query approach in `GetTransitionEdgesForDetectionAsync` (non-bug IDs → edges group-by → ticket/sprint counts) is well-reasoned for SQLite's join limitations. The approach is clean and avoids EF expression-tree pitfalls with large `Contains` sets.
- Kahn's algorithm implementation handles residual cycles correctly: any `mainCandidates` not visited by the topological sort (which only happens if a cycle survived the suppression step) are demoted to `sidelined` at lines 171-174. Defensive and correct.
- The `!store.detecting` guard in the empty-state message condition (`form.workflowStages.length === 0 && store.detectionResult === null && !store.detecting`) prevents the "Sync sprints" message from flashing during the auto-detect call on mount. Good UX defensive coding.
- Store/form state separation is clean: `moveSidelinedToStages` in the store mutates only store-owned state (the sidelined list); the view's `moveSidelined` handles the form append. This preserves the proposal-vs-saved separation described in the developer lessons.
- DI registration follows the `SprintIssueSyncService` precedent exactly (`AddScoped`, no constructor dependencies), consistent with the codebase pattern.
- `WorkflowDetectionService` is a plain class with no infrastructure dependencies — pure algorithmic logic. Correct decision not to inject anything via constructor.
- `StringComparer.OrdinalIgnoreCase` on `doneSet` protects against case mismatches between configured done statuses and transition data status names.
- Endpoint follows the `GetSettingsEndpoint` pattern precisely: `EndpointWithoutRequest`, `[AllowAnonymous]`, `[Tags("Settings")]`, constructor injection.
- Response class uses mutable properties with defaults, consistent with `GetSettingsResponse` (not a record deviation).

---

## Gaps

- **No UI for dismissing sidelined statuses without adding them to stages.** The plan specifies `removeSidelinedStatus` for this; it exists in the store but is unreachable. (Also captured as MEDIUM finding above.)
- **No test coverage for the "done statuses absent from transition data" edge case.** The testing strategy in the plan lists five backend scenarios; this edge case is not among them.
- **`removeSidelinedStatus` function body is identical to `moveSidelinedToStages`.** If a dismiss button is added later, this is the correct implementation. But two exported functions doing the same thing without a behavioral distinction in the UI is confusing for future maintainers.

---

## Open Questions

- Should Re-detect preserve manually-entered stages when detection returns no data, or is overwriting-with-empty the intended UX? The plan's Flow 2 says it "replaces the current list with a fresh proposal" — but a zero-result is arguably not a proposal. Worth clarifying with the PO before a future cycle adds a confirm step.

---

## Evidence

| Check | Result | Command | Output summary |
|-------|--------|---------|----------------|
| .NET build | PASS | `dotnet build src/Services/Fokus/Fokus.API/Fokus.API.csproj` | Build succeeded, 0 errors, 2 warnings (pre-existing NU1903 vulnerability on Microsoft.Build.Tasks.Core — not introduced by this feature) |
| Frontend build | PASS | `npm run build` (from `client/`) | ✓ built in 413ms, 0 errors, 1 pre-existing chunk-size warning |
