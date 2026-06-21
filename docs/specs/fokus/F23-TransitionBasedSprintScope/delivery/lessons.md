# TransitionBasedSprintScope — Lessons

## Developer Lessons

- **`Func` signature expansion for sparklines:** When a lambda selector needs additional context (sprint dates, transitions, settings), the entire `BuildSparkline` call site must be updated. The old `Func<List<SprintMembership>, decimal>` could not support transition-based computation. New pattern: `Func<List<SprintMembership>, List<StatusTransition>, Sprint, AppSettings, decimal>` passes all needed context. Each helper resolves boundaries internally — redundant per-call but negligible cost for a 4-sprint window.

- **File locking during build:** If the API process is running when `dotnet build` is invoked, the build will fail with MSB3027 (file locked) even though compilation itself succeeded with 0 CS errors. Kill the running process before the build step.

- **Parameter order matters at call sites:** When adding a new parameter to a service method, verify the endpoint call passes arguments in the same order as the updated signature. The SprintSummaryService endpoint call had `statusTransitions` inserted before `settings` in the endpoint but after `allDevelopers` in the service — caught before build.

- **CycleTimeService endIndex derivation:** The `endStage` string comes from `ResolveBoundaries` (a different method than `TransitionAttributionChecker.ResolveEndIndex`). To bridge these, use `TransitionAttributionChecker.GetStageIndex(endStage, orderedStages)` — this converts the string boundary to the integer index that `IsCompletedInSprint` needs. The orderedStages sequence from `ResolveBoundaries` (`WorkflowStages ++ DoneStatuses`) matches `TransitionAttributionChecker`'s sequence exactly.

- **EpicProgress needs both `closedSprints` (lightweight) and `closedMemberships`:** The endpoint previously only loaded `closedMemberships` (via `GetAllClosedSprintMembershipsAsync`). Transition-based velocity needs sprint date ranges — these come from the lightweight `GetClosedSprintsAsync` result, not from the memberships. Two separate loads are needed.

- **GetStatusTransitionsForSprintTicketsAsync covers all patterns:** The new bulk method handles all endpoint patterns uniformly. For endpoints that previously built ticket ID lists (CycleTime, ScopeChange single-sprint), this replaces the intermediate materialization step cleanly.

- **Plan performance design patterns must be applied at every call site.** When a plan specifies a Dictionary pre-grouping pattern for O(N+M) performance, it must be applied in every method that loops over memberships and calls the checker — not just the primary `ComputeMetrics` entry point. Shared helper methods (`GetTransitionCompletedMemberships`, `ComputeCarryOverMetrics`, `BuildCarryOverDestination`, etc.) each receive their own flat `statusTransitions` list and must each build their own `transitionsByTicket` dictionary. A single top-level dictionary is insufficient because these helpers are called independently.

- **Plan wording "replace X with Y" implies tracing all private helpers.** When a plan says to change how completion is determined in a service, verify that private helper methods also use the new approach — not just the public entry point. Both `ComputeTopEpics` (private) and `ComputeMetrics` (entry point) needed the same change, but only the entry point was updated in the initial pass.

## Skill Gaps

- **Missing skill:** `metric-query-patterns` — covers the pattern of replacing FinalStatus/WasCommitted snapshot predicates with TransitionAttributionChecker calls across multiple analytics services. Reference files: all 8 `*Service.cs` files in `Features/Analytics/`. Would eliminate the need for inline detail in plan steps for this class of change.

## Architect Lessons

- **Critic review caught lambda signature gaps.** The `BuildSparkline` refactoring (expanding the lambda to pass transitions, sprint, and settings) was not in the original plan draft. The critic correctly identified that the current `Func<List<SprintMembership>, decimal>` signature physically cannot support transition-based computation. Lesson: when a plan changes how data flows through a service, trace the full call graph of helper methods that consume that data — lambdas and delegates are easy to miss.

- **Rolling average methods are hidden complexity.** `ComputeRollingAverage` in DeveloperThroughputService iterates backward through historical sprints, computing per-sprint metrics. The original plan mentioned it needed transitions but did not specify the signature change explicitly. Lesson: any method that internally loops over multiple sprints and computes metrics per-sprint needs its own explicit signature change callout when the metric computation model changes.

- **ComputeTopEpics has dual-mode attribution.** The plan initially missed that `ComputeTopEpics` uses `completedStatuses.Contains(m.FinalStatus)` for sprint-level ranking (which should be transition-based) while overall epic progress uses position-based completion. The critic caught this. Lesson: when a single method mixes sprint-attribution and current-state checks, both paths must be explicitly addressed in the plan.

- **Response shape additions need scope clarification.** Adding `BugSpCompleted` to `ScopeChangePerSprintData` is additive and non-breaking, but the scope section originally said "same response shape." Lesson: distinguish between breaking shape changes (field removal, type change) and additive fields. Additive fields should be called out in scope as non-breaking additions rather than claiming "no shape changes."

- **12-step plans work for single-domain refactors.** Despite exceeding the 11-step auto-approve threshold, this plan was tractable for a single developer because all steps share the same codebase area (analytics services) and follow the same mechanical pattern (replace snapshot predicate with TransitionAttributionChecker call). The split recommendation (keep single developer) was correct — splitting would have caused merge conflicts in shared utility files.

## Reviewer Lessons

- **Plan performance design is a conformance requirement.** When a plan explicitly specifies a performance pattern (the `Dictionary<string, List<StatusTransition>>` pre-grouping), its absence is a plan conformance finding (MEDIUM), not just a style preference. Check every explicit data-structure design decision in the plan against the implementation — they are as binding as behavior changes.

- **Dead code in refactors signals incomplete edits.** The `new AppSettings()` call in `ComputeTopEpics` is a classic sign of a partial refactor: a line was added (the `ResolveStartIndex` call) but the actual predicate below it was not updated to use the result. When reviewing large refactors with many call sites, explicitly check that each new utility call is actually *used* by the code that follows it, not just present.

- **Scan IssueType filters on aggregates that claim to be feature-only.** For any metric described as "feature-only," verify that every contributing aggregate (activeSp, addedSp, removedSp, etc.) filters `!IsBug(m)`. A bug-inclusive `removedSp` silently inflates `committedSpTotal` even when all other terms are feature-only. The pattern to check: does each variable declaration include a `!IsBug` guard?

- **Method parameters not flowing through to a callee is a reliable finding pattern.** When a caller computes `orderedStages` and `endIndex` but passes neither to a private method that needs them (instead passing `settings` or `completedStatuses`), the callee is using a different or stale source. Check every private method call to verify the pre-computed values actually reach the implementation.
