# BoundaryDrivenCompletion — Implementation

## Files Created

- `src/Services/Fokus/Fokus.API/Features/Analytics/CompletionChecker.cs` — Static utility. `ResolveCompletedStatuses(AppSettings)` builds ordered sequence (WorkflowStages ++ DoneStatuses), resolves end stage (CycleTimeEndStage ?? DoneStatuses[0]), returns a `HashSet<string>(OrdinalIgnoreCase)` of all statuses from that index onward. `IsCompleted` is a trivial wrapper. Full fallback chain: no CycleTimeEndStage → DoneStatuses[0]; end stage not in sequence → DoneStatuses[0]; no done statuses → empty set.

## Files Modified

- `src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs` — `ComputeMetrics`, `ComputeSpCompleted`, `ComputeCompletionRate`, `ComputeCarryOverRate`, `ComputeTopEpics`, `ComputeLeaderboard`, `ComputeFlags` all updated from `List<string> doneStatuses` to `HashSet<string> completedStatuses`. `BuildSparkline`: vestigial unused `doneStatuses` parameter removed entirely (lambdas already closed over outer scope). `ComputeSummary`: switched to `CompletionChecker.ResolveCompletedStatuses(settings)`.
- `src/Services/Fokus/Fokus.API/Features/Analytics/ScopeChangeService.cs` — `ComputePerSprintData`, `ComputeSprintMetrics`, `BuildBurnupData` signatures updated; `doneTransitionByTicket` construction and `completedSp` filter switched to `completedStatuses`.
- `src/Services/Fokus/Fokus.API/Features/Analytics/CarryOverService.cs` — `IsCarryOver`, `ComputeCarryOverMetrics`, `ComputePerSprintData`, `BuildCarryOverDestination` updated. `StringComparer.OrdinalIgnoreCase` dropped from Contains calls (HashSet already carries it).
- `src/Services/Fokus/Fokus.API/Features/Analytics/DeveloperThroughputService.cs` — `ComputeRollingAverage` and all spCompleted/ticketsDone/ticketsCarriedOver filters updated.
- `src/Services/Fokus/Fokus.API/Features/Analytics/BugRatioService.cs` — `CompletedMemberships`, `EvaluateAlert` signatures updated; OrdinalIgnoreCase dropped from Contains calls.
- `src/Services/Fokus/Fokus.API/Features/Analytics/LeaderboardService.cs` — `CompletedMemberships` signature updated; OrdinalIgnoreCase dropped.
- `src/Services/Fokus/Fokus.API/Features/Analytics/EpicProgressService.cs` — `doneTickets`, `remainingTickets`, velocity, and `isCompleted` filters updated.
- `src/Services/Fokus/Fokus.API/Features/Analytics/CycleTimeService.cs` — `ComputeTicketCycleTime` updated; `completedMemberships` filter and `doneTransition` lookup switched to `completedStatuses`. `ResolveBoundaries` left unchanged (still used for duration measurement only).
- `src/Services/Fokus/Fokus.API/Features/Analytics/ExcludedDeveloperFilter.cs` — `GetExcludedDeveloperIds` parameter changed from `List<string> doneStatuses` to `HashSet<string> completedStatuses`.
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetSprintSummary/GetSprintSummaryEndpoint.cs` — `GetExcludedDeveloperIds` call updated to pass `CompletionChecker.ResolveCompletedStatuses(settings)`.
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetCycleTime/GetCycleTimeEndpoint.cs` — Both single and multi-sprint call sites updated.
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetCarryOver/GetCarryOverEndpoint.cs` — Both call sites updated.
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetBugRatio/GetBugRatioEndpoint.cs` — Both call sites updated.
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetDeveloperThroughput/GetDeveloperThroughputEndpoint.cs` — Call site updated.
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetScopeChange/GetScopeChangeEndpoint.cs` — Both call sites updated.
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetLeaderboard/GetLeaderboardEndpoint.cs` — Both call sites updated.
- `docs/kb/cross-cutting.md` — Added full "Boundary-Driven Completion" section documenting the rule, utility, fallback behavior, and affected services.
- `docs/kb/domain/settings.md` — DoneStatuses and CycleTimeEndStage rows updated to reflect new role.
- `docs/kb/analytics/health-score.md` — Completed definition updated to reference completedStatuses.
- `docs/kb/analytics/scope-change.md` — completedSp formula updated.
- `docs/kb/analytics/carry-over.md` — IsCarryOver condition and CarryOverDestination "Completed" row updated.
- `docs/kb/analytics/throughput.md` — spCompleted, ticketsDone, ticketsCarriedOver formulas updated.
- `docs/kb/analytics/cycle-time.md` — Pre-filter and done-transition lookup descriptions updated.
- `docs/kb/analytics/epic-progress.md` — doneTickets formula updated.
- `docs/kb/analytics/bug-ratio.md` — Completed definition updated.
- `docs/kb/analytics/leaderboard.md` — Completed filter updated.

## Key Decisions

- `CompletionChecker` placed in `Fokus.API/Features/Analytics/` (not a subfolder) — consistent with `ExcludedDeveloperFilter` placement, both are cross-cutting utilities scoped to the analytics feature area.
- `BuildSparkline`'s `doneStatuses` parameter was removed entirely rather than changed to `HashSet<string>` — the parameter was never used inside the method body; lambdas already closed over the outer `completedStatuses` variable. Removing dead parameters is cleaner than changing their type.
- `CycleTimeService.ResolveBoundaries` left fully intact — it serves duration boundary measurement and is independent from completion checking. Only `ComputeTicketCycleTime` and its callers were updated.
- Endpoint callers compute `completedStatuses` inline at `GetExcludedDeveloperIds` call sites rather than storing as a variable — `settings` is available at that point and the call is O(n) where n = workflow stage count (trivial).

## Deviations from Plan

- None. All 10 steps implemented as specified.
