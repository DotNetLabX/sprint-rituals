# Workflow Auto-Detection — Summary

## Status: COMPLETE

## What Was Built
Workflow auto-detection analyzes Jira status transition history (collected during sprint sync) to propose an ordered workflow pipeline for cycle-time measurement. A new read-only endpoint aggregates transition edges (excluding bugs), runs a forward-flow scoring algorithm (cycle resolution by dominant direction, Kahn's topological sort, done-status anchoring), and returns ordered stages, sidelined statuses, and confidence metadata. The Settings view auto-detects on first visit when stages are empty, displays a confidence summary, offers a Re-detect button, and lets users move sidelined statuses into the main pipeline before saving.

## Key Outcomes
- Files created: 4 (WorkflowDetectionService, DetectWorkflowStagesEndpoint, DetectWorkflowStagesQuery, plus records in TicketRepository)
- Files modified: 5 (TicketRepository, DependencyInjection.cs, types/index.ts, api/settings.ts, settingsStore.ts, SettingsView.vue)
- Build status: .NET build PASS, frontend build PASS
- Review verdict: APPROVED (1 review cycle, no CRITICAL or HIGH issues; 3 MEDIUM, 2 LOW)

## Deviations from Plan
- Store action renamed from `detectWorkflowStages()` to `runDetectWorkflowStages()` (undocumented in implementation.md but functionally consistent throughout)

## Notes
The reviewer identified 5 findings below CRITICAL/HIGH threshold. None block merge, but the user should decide whether to address them before committing:

**MEDIUM findings:**
1. **Store action name deviation** — Plan specified `detectWorkflowStages()`, implementation uses `runDetectWorkflowStages()`. Consistent internally but undocumented as a deviation. Fix: rename to match plan, or document the deviation.
2. **`removeSidelinedStatus` is dead code** — The plan specifies a dismiss-without-add action. The function exists in the store but no UI button triggers it. The "Other statuses" pills only have a "+" (move-to-stages) button, no dismiss button. Fix: add a dismiss button, or remove the dead function and note the omission.
3. **`reDetect()` overwrites manual stages with empty array** — When Re-detect returns zero stages, `form.workflowStages` is replaced with `[]`, losing any manually entered stages. The auto-detect-on-mount path guards against this (`stages.length > 0` check) but `reDetect()` does not. Fix: add the same `stages.length > 0` guard to `reDetect()`.

**LOW findings:**
4. **All statuses sidelined when done statuses don't match transition data** — If configured done statuses (e.g., "Done") don't appear in any transition edge, the algorithm has no terminal anchor and sidelines everything. Case differences are handled (`OrdinalIgnoreCase`), but spelling mismatches are not. No code change needed; consider a log warning or UI hint in a follow-up.
5. **Tie-break in cycle suppression is arbitrary for equal counts** — When a bidirectional status pair has identical transition counts, the algorithm deterministically but arbitrarily picks a direction. Acceptable for an approximation algorithm; no change needed.
