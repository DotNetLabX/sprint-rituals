# TransitionBasedSprintScope — Implementation

## Files Created

- `src/Services/Fokus/Fokus.API/Features/Analytics/TransitionAttributionChecker.cs` — New static utility class for transition-timestamp-based sprint scope attribution. Implements ResolveStartIndex, ResolveEndIndex, GetStageIndex, IsStartedInSprint, IsCompletedInSprint, IsAddedInSprint, IsCarryOver. Start stage fallback uses orderedStages[0] (wider than cycle time per spec BR19).

## Files Modified

- `src/Services/Fokus/Fokus.Persistence/Repositories/TicketRepository.cs` — Added `GetStatusTransitionsForSprintTicketsAsync(List<int> sprintIds)` — bulk loads transitions for all tickets in given sprints via two-step query (distinct TicketIds from SprintMemberships, then StatusTransitions WHERE TicketId IN those IDs).

- `src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs` — ComputeSummary gains `List<StatusTransition> statusTransitions` parameter. ComputeMetrics rewritten: activeSp/completedSp/addedSp/carryOverSp all transition-based and feature-only. SpCommitted replaced with ActiveSp. BuildSparkline refactored to `Func<List<SprintMembership>, List<StatusTransition>, Sprint, AppSettings, decimal>`. All 5 sparkline helpers rewritten with new signature. ComputeTopEpics and ComputeLeaderboard use transition-based completion.

- `src/Services/Fokus/Fokus.API/Features/Analytics/ScopeChangeService.cs` — ComputeMultiSprint and ComputeSingleSprint gain `List<StatusTransition> statusTransitions`. ScopeChangePerSprintData gains `BugSpCompleted` field. ComputeSprintMetrics fully transition-based and feature-only. BuildBurnupData reshaped: scope line = cumulative started tickets SP per day; completed line = cumulative completed tickets SP per day.

- `src/Services/Fokus/Fokus.API/Features/Analytics/CarryOverService.cs` — ComputeMultiSprint and ComputeSingleSprint gain `List<StatusTransition> statusTransitions`. Carry-over definition: isStarted AND !isCompleted (replaces FinalStatus-based). Feature-only. BuildCarryOverDestination: "Completed" = isCompletedInSprint, "Carried Again" = isStarted AND !isCompleted, "Dropped" = in sprint but no qualifying start transition.

- `src/Services/Fokus/Fokus.API/Features/Analytics/DeveloperThroughputService.cs` — ComputeThroughput gains `List<StatusTransition> statusTransitions`. Removed `completedStatuses` usage. New ComputeMetrics helper centralizes transition-based per-developer metrics. ComputeRollingAverage gains `statusTransitions` and `settings`, drops `completedStatuses`.

- `src/Services/Fokus/Fokus.API/Features/Analytics/BugRatioService.cs` — ComputeMultiSprint and ComputeSingleSprint gain `List<StatusTransition> statusTransitions`. CompletedMemberships helper replaced with GetTransitionCompletedMemberships and ComputeCompletedBugNonBugSp. EvaluateAlert updated to use transition-based completion per sprint.

- `src/Services/Fokus/Fokus.API/Features/Analytics/LeaderboardService.cs` — ComputeMultiSprint and ComputeSingleSprint gain `List<StatusTransition> statusTransitions`. CompletedMemberships helper replaced with GetTransitionCompletedMemberships using IsCompletedInSprint.

- `src/Services/Fokus/Fokus.API/Features/Analytics/EpicProgressService.cs` — ComputeEpicProgress gains `List<StatusTransition> statusTransitions` and `List<Sprint> closedSprints`. Progress tracking unchanged (position-based CurrentStatus). Velocity tracking switched to transition-based: IsCompletedInSprint per sprint's date range using sprintDateLookup.

- `src/Services/Fokus/Fokus.API/Features/Analytics/CycleTimeService.cs` — ComputeTicketCycleTime: removed `completedStatuses` parameter; pre-filter uses IsCompletedInSprint; done-transition search uses GetStageIndex >= endIndex. Both callers (ComputeSingleSprint, ComputeMultiSprint) compute endIndex via GetStageIndex(endStage, orderedStages).

- `src/Services/Fokus/Fokus.API/Features/Analytics/ExcludedDeveloperFilter.cs` — GetExcludedDeveloperIds: removed `completedStatuses` parameter, added `List<StatusTransition> statusTransitions` and `AppSettings settings`. "Completed any ticket" check uses IsCompletedInSprint.

- `src/Services/Fokus/Fokus.API/Features/Analytics/GetSprintSummary/GetSprintSummaryEndpoint.cs` — Loads statusTransitions via GetStatusTransitionsForSprintTicketsAsync(windowIds). Updates ExcludedDeveloperFilter call and passes statusTransitions to ComputeSummary.

- `src/Services/Fokus/Fokus.API/Features/Analytics/GetScopeChange/GetScopeChangeEndpoint.cs` — Replaces ticket-ID-based transition loading with GetStatusTransitionsForSprintTicketsAsync. Updates ExcludedDeveloperFilter calls. Passes statusTransitions to both ComputeSingleSprint and ComputeMultiSprint.

- `src/Services/Fokus/Fokus.API/Features/Analytics/GetCarryOver/GetCarryOverEndpoint.cs` — Adds TicketRepository dependency. Loads statusTransitions for all closed sprint IDs. Updates ExcludedDeveloperFilter calls. Passes statusTransitions to both service methods.

- `src/Services/Fokus/Fokus.API/Features/Analytics/GetDeveloperThroughput/GetDeveloperThroughputEndpoint.cs` — Adds TicketRepository dependency. Loads statusTransitions for allSprintIds (covers target + rolling average window). Updates ExcludedDeveloperFilter calls. Passes statusTransitions to ComputeThroughput.

- `src/Services/Fokus/Fokus.API/Features/Analytics/GetBugRatio/GetBugRatioEndpoint.cs` — Adds TicketRepository dependency. Loads statusTransitions for all closed sprint IDs. Updates ExcludedDeveloperFilter calls. Passes statusTransitions to both service methods.

- `src/Services/Fokus/Fokus.API/Features/Analytics/GetLeaderboard/GetLeaderboardEndpoint.cs` — Adds TicketRepository dependency. Loads statusTransitions for all closed sprint IDs. Updates ExcludedDeveloperFilter calls. Passes statusTransitions to both service methods.

- `src/Services/Fokus/Fokus.API/Features/Analytics/GetCycleTime/GetCycleTimeEndpoint.cs` — Replaces ticket-ID-based transition loading with GetStatusTransitionsForSprintTicketsAsync. Updates ExcludedDeveloperFilter calls.

- `src/Services/Fokus/Fokus.API/Features/Analytics/GetEpicProgress/GetEpicProgressEndpoint.cs` — Loads closedSprints (lightweight), loads statusTransitions via GetStatusTransitionsForSprintTicketsAsync(closedSprintIds). Passes both to ComputeEpicProgress.

- `docs/kb/cross-cutting.md` — Updated Feature-Only Metrics section to reflect all scope surfaces now feature-only. Replaced Boundary-Driven Completion section with new Transition-Based Sprint Scope section documenting TransitionAttributionChecker. Retained CompletionChecker section noting its retained uses.

- `docs/kb/analytics/scope-change.md` — Updated Core Formulas to transition-based. Updated Burnup Chart section to reflect cumulative-transitions scope line. Added Multi-Sprint Bar Chart section documenting BugSpCompleted.

- `docs/kb/analytics/carry-over.md` — Updated Core Formulas to transition-based feature-only. Updated Carry-Over Destination table to use IsCompletedInSprint / IsStartedInSprint.

- `docs/kb/analytics/health-score.md` — Updated Input Metrics to transition-based formulas with activeSp denominator.

- `docs/kb/analytics/throughput.md` — Updated per-sprint metrics and rolling average to transition-based.

- `docs/kb/analytics/cycle-time.md` — Updated Per-Ticket Computation step 1 to note transition-based pre-filter.

- `docs/kb/analytics/bug-ratio.md` — Updated completed filter and alert evaluation to transition-based.

- `docs/kb/analytics/leaderboard.md` — Updated completed filter to transition-based.

- `docs/kb/analytics/epic-progress.md` — Added Dual Mode section documenting progress (position-based) vs velocity (transition-based).

- `docs/kb/domain/settings.md` — Updated CycleTimeStartStage and CycleTimeEndStage descriptions to note their role in TransitionAttributionChecker scope attribution.

## Key Decisions

- **Start stage fallback divergence from cycle time:** TransitionAttributionChecker uses `orderedStages[0]` (first stage) as fallback when CycleTimeStartStage is null. CycleTimeService uses `WorkflowStages[1]` (second stage). This is intentional per spec BR19 — scope attribution captures all sprint engagement including queue entry.

- **EpicProgress dual mode:** Progress tracking (doneTickets, doneSp, spCompletionPct) remains position-based (CompletionChecker on CurrentStatus). Velocity switches to transition-based. This matches spec BR16.

- **CarryOver "Dropped" bucket:** Tickets in the current sprint that have no qualifying start transition are classified as "Dropped" (fell back to backlog). This is a reasonable interpretation — they appeared in the sprint but never entered the active cycle.

- **CycleTime endIndex computation:** CycleTimeService.ComputeTicketCycleTime previously received `completedStatuses` (HashSet) for pre-filter and done-transition search. Both replaced with `endIndex` (int) computed via `TransitionAttributionChecker.GetStageIndex(endStage, orderedStages)`. The pre-filter uses IsCompletedInSprint; the done-transition search uses `GetStageIndex(t.ToStatus) >= endIndex`.

- **No changes to WasCommitted or FinalStatus fields:** These remain stored on SprintMembership but no longer drive attribution. Out of scope per plan.

## Deviations from Plan

None. All 12 steps implemented as specified.

---

## Review Cycle 1 Fixes

### [HIGH] ComputeTopEpics — fixed completion predicate

- `src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs` — Removed dead `new AppSettings()` call from `ComputeTopEpics`. Added `orderedStages` and `endIndex` parameters passed from `ComputeSummary` caller (already computed at lines 104-105). Replaced `completedStatuses.Contains(t.ToStatus)` predicate with `TransitionAttributionChecker.GetStageIndex(t.ToStatus, orderedStages) >= endIndex`. Updated call site in `ComputeSummary` to pass `orderedStages, endIndex`.

### [MEDIUM] removedSp includes bug removals — fixed

- `src/Services/Fokus/Fokus.API/Features/Analytics/ScopeChangeService.cs` — Added `&& m.Ticket?.IssueType != "Bug"` filter to `removedSp` LINQ in `ComputeSprintMetrics`. Keeps `committedSpTotal = activeSp + removedSp` consistent with spec BR9 (all scope surfaces feature-only).

### [MEDIUM] Per-ticket O(N×M) scan — pre-grouping dictionary applied to all services

- `src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs` — Added `transitionsByTicket` dictionary in `ComputeMetrics` and all 5 sparkline helpers (ComputeSpCompleted, ComputeCompletionRate, ComputeScopeDisruptionRate, ComputeBugDisruptionRate, ComputeCarryOverRate). Each passes `transitionsByTicket.GetValueOrDefault(m.TicketId, [])` to checker calls.
- `src/Services/Fokus/Fokus.API/Features/Analytics/ScopeChangeService.cs` — Added `transitionsByTicket` dictionary in `ComputeSprintMetrics` before the membership foreach loop.
- `src/Services/Fokus/Fokus.API/Features/Analytics/CarryOverService.cs` — Added `transitionsByTicket` dictionary in `ComputeMultiSprint` (shared across issue-type LINQ and zombie loop), `ComputeSingleSprint`, `ComputeCarryOverMetrics`, `ComputePerSprintData`, and `BuildCarryOverDestination`.
- `src/Services/Fokus/Fokus.API/Features/Analytics/BugRatioService.cs` — Added `transitionsByTicket` dictionary in `GetTransitionCompletedMemberships`.
- `src/Services/Fokus/Fokus.API/Features/Analytics/LeaderboardService.cs` — Added `transitionsByTicket` dictionary in `GetTransitionCompletedMemberships`.
- `src/Services/Fokus/Fokus.API/Features/Analytics/EpicProgressService.cs` — Added `transitionsByTicket` dictionary before the velocity `sprintSpCompleted` GroupBy chain.

### [LOW] Two-query repository — no fix (documented deviation)

- `GetStatusTransitionsForSprintTicketsAsync` executes two queries (TicketIds from SprintMemberships, then StatusTransitions IN those IDs). This is EF Core's natural SQLite translation of the `Contains()` pattern and is functionally equivalent to a single join. No code change. Documented as known deviation.
