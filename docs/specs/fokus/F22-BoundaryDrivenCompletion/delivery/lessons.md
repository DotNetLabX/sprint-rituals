# BoundaryDrivenCompletion — Lessons

## Developer Lessons

- When a method parameter is passed down through multiple layers but never used inside the deepest method (lambdas close over an outer variable instead), remove the dead parameter rather than changing its type. Read the method body before changing the signature.
- `HashSet<string>(StringComparer.OrdinalIgnoreCase)` subsumes all `List<string>.Contains(..., StringComparer.OrdinalIgnoreCase)` calls — the comparer is baked into the set at construction, so all subsequent `.Contains()` calls are case-insensitive without any additional argument. Drop the comparer argument at every usage site when migrating.
- Cross-cutting static utilities that are used by multiple analytics services belong in the feature area root (`Features/Analytics/`) alongside existing utilities like `ExcludedDeveloperFilter`, not in a god subfolder.
- `ResolveBoundaries` in `CycleTimeService` and `ResolveCompletedStatuses` in `CompletionChecker` are related but separate concerns: `ResolveBoundaries` resolves start/end stage indices for duration measurement; `ResolveCompletedStatuses` resolves a set of statuses for completion checking. Do not merge them — they have different callers and different return types.

## Architect Lessons

- When planning a cross-cutting predicate replacement (changing what "completed" means), the plan should explicitly call out dead parameters in method signatures rather than assuming all parameters are live. The `BuildSparkline` dead parameter was caught by the developer, not the plan. Future plans for signature migrations: read method bodies to identify dead parameters before specifying type changes.
- Endpoint files that call cross-cutting utilities (like `ExcludedDeveloperFilter.GetExcludedDeveloperIds`) are indirect consumers of the change. The plan correctly said "grep for all call sites" but did not enumerate the expected endpoint files. This worked because the instruction was clear, but for reviewability, listing known call sites in the plan is better than relying on the developer to discover them.
- The plan's 10-step structure (1 utility + 8 services + 1 KB) was the right decomposition for this cross-cutting change. Each step was independently testable and the single dependency (Step 1) made ordering trivial. This pattern works well for "replace predicate X with predicate Y across N services."

## Reviewer Lessons

- For a cross-cutting predicate replacement across many services, the most effective review path is: read CompletionChecker first (the new utility), then grep for ALL surviving `settings.DoneStatuses` / `doneStatuses.Contains` references in the feature area, then spot-check 2-3 service files for correctness. Exhaustive file-by-file reading of all 8 services is slower and adds no additional signal once the grep confirms no stale callers.
- When a plan says "remove dead parameter" as a deviation, verify the original method body directly (or via `git show`) before accepting it — dead parameter removal is the developer improving on the plan, not deviating from intent. In this case the deviation was valid: the parameter was never used inside `BuildSparkline`.
- KB review for a computation-change feature is as important as code review: stale KB entries propagate incorrect mental models to future agents. Always grep the KB for old formula wording after a computation change.

## Skill Gaps

- **Missing skill:** No skill for "migrate a shared parameter type across multiple service method signatures (e.g. List<string> → HashSet<string>)". Coverage: pattern for finding all method signatures, updating them consistently, dropping now-redundant comparer arguments. Reference files: `CarryOverService.cs`, `BugRatioService.cs`, `LeaderboardService.cs`.
