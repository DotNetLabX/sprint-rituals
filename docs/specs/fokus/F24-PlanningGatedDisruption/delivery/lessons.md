# Planning-Gated Disruption — Lessons

## Developer Lessons

- When a method parameter is renamed for semantic clarity (e.g. `sprintStart` → `planningCutoff`), all callers must be updated even if the logic is unchanged — the compiler won't catch it since the type is the same (`DateTime`). Review all call sites explicitly.
- When restructuring a loop that previously skipped removed tickets with `continue`, splitting the removed-ticket path first (check, accumulate, `continue`) and then the active-ticket path keeps the logic clean and avoids double-processing.
- `BuildEventTable` can show all events while applying classification selectively — the "show everything, classify subset" pattern requires the same eligibility checks as the metric computation, so extracting those checks into a shared predicate (or re-applying inline) is important for consistency.
- Unused imports in Vue components (`useSettingsStore`) left behind after a refactor cause no build failure but should be cleaned up immediately.

## Architect Lessons

- When a plan step says "skip if excluded status" for removed tickets but the spec says "non-excluded," the ambiguity comes from removed tickets having potentially stale FinalStatus values. Future plans should explicitly state whether excluded-status filtering applies to removed tickets and why, rather than listing it as a filter without rationale.
- The plan's approach of restructuring the main loop to handle removed tickets inline (rather than as a separate LINQ) was clean and the developer followed it exactly. This pattern -- splitting a loop's `continue` to handle two branches (removed vs non-removed) -- is worth noting as a recurring refactoring shape for scope change metric modifications.
- The `IsRemovedPostPlanning` method on `TransitionAttributionChecker` mirrors the `IsAddedInSprint` pattern well. Both are static predicates that encapsulate a two-condition check (timing gate + cycle-entry). This symmetry makes the utility easier to reason about.
- Renaming `IsAddedInSprint`'s parameter from `sprintStart` to `planningCutoff` while keeping the body identical was the right call -- it makes the semantic shift explicit without changing behavior, and callers are forced to think about which value to pass. This is a lightweight technique for making breaking semantic changes visible at call sites.
- The `settingsStore` removal from ClassificationTable.vue was a good dead-code cleanup that fell out naturally from removing Planning Overflow. Plans could proactively note "removing this branch may leave dead imports" to help the developer catch it, but in this case the developer found it independently.

## Reviewer Lessons

- When a loop is restructured to handle removed tickets as an early-return branch, verify whether any filters from the original flow (e.g. excluded-status check) were intentionally or accidentally bypassed. The spec's eligibility conditions for the new branch must be checked independently — they don't inherit from the surrounding flow.
- When a KB file has two sections documenting the same method (an old feature section and a new feature section), both must be updated. The old section's "Key methods" list and the new section's change note can contradict each other if only the new section is written.
- Pre-commitment prediction #5 (removedSp excluded-status gap) was confirmed. This type of filtering gap — where a new early-return branch handles a case that previously fell through to a shared filter — is a reliable prediction target for loop restructuring reviews.

## Skill Gaps

- **Missing skill:** Inline analytics computation changes (updating existing service method formulas, restructuring loops, threading new parameters through call sites). Suggested name: `update-analytics-computation`. Coverage: parameter threading, loop restructuring, eligibility predicate updates, call site identification. Reference files used: `ScopeChangeService.cs`, `SprintSummaryService.cs`, `TransitionAttributionChecker.cs`.
