# Transition-Based Sprint Scope

**Feature Spec:** `docs/features/TransitionBasedSprintScope/spec.md`

## Context

All sprint scope metrics currently use snapshot-based attribution: `WasCommitted` flag determines committed scope, `FinalStatus` position (via `CompletionChecker`) determines completion. This double-counts carry-over tickets — a ticket In Progress in Sprint 26 that completes in Sprint 27 adds its SP to both sprints' committed totals and shows as completed in both.

This feature replaces snapshot-based attribution with transition-timestamp-based attribution. A ticket's "started" event belongs to the sprint where the StatusTransition to CycleTimeStartStage (or beyond) occurred. A ticket's "completed" event belongs to the sprint where the StatusTransition to CycleTimeEndStage (or beyond) occurred. Each transition timestamp falls in exactly one sprint — no double-counting.

**Services impacted:** Fokus (sole service). Changes span a new attribution utility, all 8 analytics services + ExcludedDeveloperFilter, all 7 analytics endpoints (to load StatusTransitions), the burnup chart (reshaped from plan-based to cumulative-transitions), and the multi-sprint bar chart (feature-only + separate bug bars). No frontend changes are specified in this feature (frontend will adapt to changed response values naturally). No new endpoints or settings.

## Scope

**In scope:**
- Create a `TransitionAttributionChecker` utility that encapsulates "is ticket T started/completed in sprint S" using StatusTransitions
- Replace `WasCommitted`-based and `FinalStatus`-based attribution in all 8 analytics services with transition-based attribution
- Make all scope surfaces feature-only (carry-over rate, disruption rate denominators, scope cards)
- Reshape burnup chart from plan-based to cumulative-transitions-per-day
- Add separate bug bars to multi-sprint bar chart data
- Update all endpoints to load StatusTransitions (currently only ScopeChange single-sprint and CycleTime do)
- Update ExcludedDeveloperFilter to use transition-based completion
- Update KB entries

**Explicitly out of scope:**
- Removing WasCommitted or FinalStatus fields from SprintMembership
- New endpoints, new settings UI, new entities
- Frontend component changes (frontend reads the same response shapes with updated values; additive fields like `BugSpCompleted` on `ScopeChangePerSprintData` are non-breaking — frontend ignores unknown fields until it consumes them)
- Sprint-overlap handling (deferred per spec)
- Burnup "expected completion" trend line
- Renaming "Committed SP" labels

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | (none) | — | Create TransitionAttributionChecker utility with start/completed/added/carry-over checks | Cross-cutting analytics utility — no skill |
| 2 | (none) | — | Add repository method for bulk transition loading by sprint ticket IDs | No skill for repository query additions |
| 3 | (none) | — | Update SprintSummaryService to transition-based + feature-only | Service-specific inline changes |
| 4 | (none) | — | Update ScopeChangeService metrics + burnup reshaping | Service-specific inline changes |
| 5 | (none) | — | Update CarryOverService to transition-based + feature-only | Service-specific inline changes |
| 6 | (none) | — | Update DeveloperThroughputService to transition-based | Service-specific inline changes |
| 7 | (none) | — | Update BugRatioService to transition-based | Service-specific inline changes |
| 8 | (none) | — | Update LeaderboardService to transition-based | Service-specific inline changes |
| 9 | (none) | — | Update EpicProgressService velocity to transition-based | Service-specific inline changes |
| 10 | (none) | — | Update CycleTimeService pre-filter + ExcludedDeveloperFilter | Service-specific inline changes |
| 11 | (none) | — | Update all analytics endpoints to load StatusTransitions | Endpoint data-loading changes |
| 12 | (none) | — | Update KB entries | Documentation |

No skills exist for cross-cutting analytics computation changes or inline predicate replacement. All steps are "None" disposition because this feature modifies existing computation logic. Logged as observation — a future `metric-query-patterns` skill would cover this pattern class.

## Domain Model Changes

None. No new entities, value objects, or domain events. StatusTransition already has `TicketId`, `ToStatus`, `Timestamp` — all fields needed for transition-based attribution. SprintMembership retains `WasCommitted` and `FinalStatus` as stored fields (no longer drive attribution).

## Data Model Changes

None. No migrations needed. All changes are computation-level — reinterpreting existing StatusTransition data.

## Implementation Steps

### Step 1: Create TransitionAttributionChecker utility

**Files to create:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/TransitionAttributionChecker.cs`

Create a static utility class that encapsulates transition-based sprint scope attribution. This is the counterpart to `CompletionChecker` (which handles position-based FinalStatus checks and is retained for non-sprint-scope uses like EpicProgress progress tracking).

**Members:**

1. `ResolveStartIndex(AppSettings settings) -> (List<string> orderedStages, int startIndex)` — returns the ordered stage sequence and the index of the start boundary stage.
   - Build ordered sequence: `WorkflowStages ++ DoneStatuses`
   - Resolve start stage: `CycleTimeStartStage ?? orderedStages[0]` (first stage — spec BR19 fallback divergence: scope attribution captures all sprint engagement including queue entry)
   - Find startIndex = position of start stage in orderedStages (case-insensitive)
   - If not found, fall back to index 0

2. `ResolveEndIndex(AppSettings settings, List<string> orderedStages) -> int` — returns the index of the end boundary stage.
   - Resolve end stage: `CycleTimeEndStage ?? DoneStatuses[0]`
   - Find endIndex in orderedStages (case-insensitive)
   - If not found, fall back to first done status position; if still not found, return -1 (nothing completes)

3. `GetStageIndex(string status, List<string> orderedStages) -> int` — returns the index of a status in orderedStages, or -1 if not found. Case-insensitive.

4. `IsStartedInSprint(string ticketId, List<StatusTransition> transitions, DateTime sprintStart, DateTime sprintEnd, List<string> orderedStages, int startIndex) -> (bool isStarted, DateTime? startedAt)` — checks if ticket has any transition within [sprintStart, sprintEnd] where `GetStageIndex(ToStatus) >= startIndex`. Returns earliest qualifying transition timestamp.

5. `IsCompletedInSprint(string ticketId, List<StatusTransition> transitions, DateTime sprintStart, DateTime sprintEnd, List<string> orderedStages, int endIndex) -> (bool isCompleted, DateTime? completedAt)` — checks if ticket has any transition within [sprintStart, sprintEnd] where `GetStageIndex(ToStatus) >= endIndex`. Returns earliest qualifying transition timestamp.

6. `IsAddedInSprint(SprintMembership membership, bool isStarted, DateTime sprintStart) -> bool` — `membership.AddedAt > sprintStart AND isStarted`. Spec BR5: mid-sprint addition that entered the cycle.

7. `IsCarryOver(bool isStarted, bool isCompleted) -> bool` — `isStarted AND NOT isCompleted`. Spec BR12.

**Performance design:** The utility operates on pre-grouped transitions (`Dictionary<string, List<StatusTransition>>` keyed by ticketId). Each service builds this lookup once per request from the bulk-loaded transition list. The orderedStages list and boundary indices are computed once per request from settings.

**Pattern reference:** `CompletionChecker.cs` for the static utility class pattern. `CycleTimeService.ResolveBoundaries` (lines 291-303) for ordered stage construction.

**Why separate from CompletionChecker:** CompletionChecker answers "is this status a completed status?" (position-based, FinalStatus snapshot). TransitionAttributionChecker answers "did this ticket transition to a stage during this sprint?" (transition-timestamp-based). Both use the same boundary settings but answer different questions. CompletionChecker remains in use for EpicProgress progress tracking (current status position check — spec BR16) and ExcludedFromScope filtering (FinalStatus at sprint end — spec BR15).

**Depends on:** Nothing.

### Step 2: Add repository method for bulk transition loading

**Files to modify:**
- `src/Services/Fokus/Fokus.Persistence/Repositories/TicketRepository.cs`

Add a new method alongside the existing `GetStatusTransitionsForTicketsAsync`:

```
GetStatusTransitionsForSprintTicketsAsync(List<int> sprintIds, CancellationToken ct)
```

This loads all StatusTransitions for tickets that appear in any of the given sprints' memberships. Single query joining SprintMemberships to StatusTransitions:

```
SELECT st.* FROM StatusTransitions st
WHERE st.TicketId IN (SELECT DISTINCT sm.TicketId FROM SprintMemberships sm WHERE sm.SprintId IN @sprintIds)
```

**Why a new method:** Every analytics endpoint currently loads sprints with memberships, then (only ScopeChange and CycleTime) optionally loads transitions for specific ticket IDs. After this feature, ALL endpoints need transitions. Rather than constructing ticket ID lists in every endpoint and calling `GetStatusTransitionsForTicketsAsync`, a single method that accepts sprint IDs avoids the intermediate materialization step and makes a single round-trip.

**Performance:** The query joins on SprintMemberships (indexed by SprintId) and StatusTransitions (indexed by TicketId). For a typical team (~200 tickets across 5 sprints, ~1000 transitions), this is a lightweight query.

**Depends on:** Nothing.

### Step 3: Update SprintSummaryService to transition-based + feature-only

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs`

**Signature change:** `ComputeSummary` gains a new parameter: `List<StatusTransition> statusTransitions`.

**Changes to `ComputeMetrics`:**

The method currently computes:
- `committed` = `m.WasCommitted && !removed` (total-scope)
- `featureCommitted` = `m.WasCommitted && !removed && !IsBug(m)` (feature-only)
- `featureCompleted` = `completedStatuses.Contains(m.FinalStatus) && !removed && !IsBug(m)`
- `carryOver` = `!completedStatuses.Contains(m.FinalStatus) && !removed`

Replace with transition-based attribution. The new computation:

1. Build `transitionsByTicket` lookup (Dictionary<string, List<StatusTransition>>) from the passed transitions.
2. Resolve orderedStages, startIndex, endIndex from settings (via TransitionAttributionChecker).
3. For each membership, compute: `isStarted`, `isCompleted`, `isAdded` using TransitionAttributionChecker.
4. Apply excluded-from-scope filter: a ticket whose FinalStatus is in ExcludedFromScopeStatuses is excluded even if it has qualifying transitions (spec BR15).
5. New aggregates (all feature-only per spec BR9):
   - `activeSp` = sum(SP) where isStarted AND !removed AND !IsBug AND !excluded
   - `completedSp` = sum(SP) where isCompleted AND !removed AND !IsBug AND !excluded
   - `addedSp` = sum(SP) where isAdded AND !removed AND !IsBug AND !excluded
   - `carryOverSp` = sum(SP) where isStarted AND !isCompleted AND !removed AND !IsBug AND !excluded
   - `bugSpCompleted` = sum(SP) where isCompleted AND !removed AND IsBug AND !excluded

6. New formulas (spec BR21):
   - `completionRate` = completedSp / activeSp * 100 (may exceed 100% per spec BR14 — carry-over completions)
   - `scopeDisruptionRate` = addedSp / activeSp * 100 (was: spAddedScope / committed; now: feature-only added / feature-only active)
   - `bugDisruptionRate` = bugAddedSp / activeSp * 100 (bug additions / feature active)
   - `carryOverRate` = carryOverSp / activeSp * 100 (was: carryOver / (committed + added); now: feature-only)

**Carry-over completions from previous sprints:** A ticket started in a prior sprint and completed in the current sprint adds to `completedSp` (it has a transition to endStage in the current sprint) but NOT to `activeSp` (it has no transition to startStage in the current sprint — that transition was in the prior sprint). This is why completion % can exceed 100% (spec BR14).

**Sparklines — `BuildSparkline` refactoring:** The current `BuildSparkline` lambda signature (`Func<List<SprintMembership>, decimal>`) cannot support transition-based computation because it lacks access to transitions and sprint dates. Refactor `BuildSparkline` to accept a value selector with the expanded signature: `Func<List<SprintMembership>, List<StatusTransition>, Sprint, AppSettings, decimal>`. The method body passes the full transitions list, the specific Sprint object (for date range), and AppSettings to the selector. Each sparkline point filters transitions for the specific sprint's date range internally.

The 5 helper methods that serve as sparkline selectors must all be rewritten with the new signature:
- `ComputeSpCompleted(memberships, transitions, sprint, settings) -> decimal`
- `ComputeCompletionRate(memberships, transitions, sprint, settings) -> decimal`
- `ComputeScopeDisruptionRate(memberships, transitions, sprint, settings) -> decimal`
- `ComputeBugDisruptionRate(memberships, transitions, sprint, settings) -> decimal`
- `ComputeCarryOverRate(memberships, transitions, sprint, settings) -> decimal`

Each helper resolves orderedStages/startIndex/endIndex from settings, builds the transition lookup, and computes its metric using TransitionAttributionChecker. The `completedStatuses` parameter is removed from these helpers (no longer needed for sprint scope attribution).

**`ComputeTopEpics` (line 454-501):** Currently uses `completedStatuses.Contains(m.FinalStatus)` to identify which tickets completed in the selected sprint for top epics ranking. Replace with transition-based completion: a ticket completed in the sprint has a transition to CycleTimeEndStage during the sprint's date range. The epic's `SpCompletedThisSprint` uses transition-based attribution. The epic's overall `DoneSp` and `CompletionPercentage` remain position-based (uses `completedStatuses.Contains(t.CurrentStatus)`) — this is the same dual-mode as EpicProgressService (spec BR16: progress = current status position, sprint attribution = transition-based).

**Leaderboard in ComputeSummary:** Currently uses FinalStatus-based `completedStatuses.Contains(m.FinalStatus)`. Replace with transition-based completion check.

**Health score:** Uses transition-based completion %, disruption rate, carry-over rate. Formula unchanged, inputs change.

**Depends on:** Steps 1, 2.

### Step 4: Update ScopeChangeService metrics + burnup reshaping

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/ScopeChangeService.cs`

**Signature change:** `ComputeMultiSprint` gains `List<StatusTransition> statusTransitions` parameter. Pass to `ComputePerSprintData` along with the Sprint object (for date range access).

**Part A: Multi-sprint `ComputePerSprintData` and `ComputeSprintMetrics`**

Replace snapshot-based logic with transition-based attribution (same pattern as Step 3):
- `committedSpActive` (now "Active SP") = sum(SP) where isStarted, !removed, !IsBug, !excluded
- `committedSpTotal` = Active + Removed feature tickets SP
- `addedSp` = sum(SP) where isAdded, !removed, !IsBug, !excluded (spec BR5)
- `removedSp` = unchanged (RemovedAt-based, spec BR6)
- `completedSp` = sum(SP) where isCompleted, !removed, !IsBug, !excluded
- `disruptionRate` = addedSp / activeSp * 100
- `bugCount` = count of bug tickets with transitions to endStage during sprint (transition-based bug completion count)

Add new field to `ScopeChangePerSprintData`: `BugSpCompleted` (decimal) — bug SP completed this sprint, for separate bug bars in the bar chart (spec BR10).

**Part B: Single-sprint `ComputeSprintMetrics`**

Same formula changes as Part A. The method signature gains `List<StatusTransition> transitions, Sprint sprint` parameters (needs sprint dates for transition window).

**Part C: Burnup chart `BuildBurnupData` reshaping (spec BR11)**

The current burnup logic:
- Scope line starts at committedSpActive, steps up with additions, down with removals per day
- Completed line uses first done-status transition per ticket per day

Replace with transition-based cumulative lines:
- **Scope line** = cumulative feature tickets that have transitioned to CycleTimeStartStage (or beyond) within [sprintStart, current day]. Starts near zero and grows as tickets enter the pipeline. For each day, count newly-started tickets (first qualifying transition on that day) and add their SP.
- **Completed line** = cumulative feature tickets that have transitioned to CycleTimeEndStage (or beyond) within [sprintStart, current day]. Same day-by-day accumulation.
- **Bug area** = unchanged (tracks remaining bug work — starts at bug SP present at sprint start, grows with bug additions, shrinks with bug completions and removals).

The `BurnupDataPoint` record shape is unchanged. The semantic of `TotalScopeSp` changes from "plan-based scope" to "cumulative started tickets SP."

**Part D: Multi-sprint bar chart bug bars**

The `ScopeChangePerSprintData` record needs a `BugSpCompleted` field (decimal). Each per-sprint entry computes bug SP completed using transition-based attribution for bugs.

**Depends on:** Steps 1, 2.

### Step 5: Update CarryOverService to transition-based + feature-only

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/CarryOverService.cs`

**Signature changes:** `ComputeMultiSprint` and `ComputeSingleSprint` gain `List<StatusTransition> statusTransitions` parameter.

**`IsCarryOver` replacement (spec BR12):**

Current: `!completedStatuses.Contains(m.FinalStatus) && m.RemovedAt == null`

New: `isStarted AND !isCompleted AND !removed` — a ticket is carry-over when it has a transition to CycleTimeStartStage during the sprint but no transition to CycleTimeEndStage during the sprint.

**Feature-only (spec BR12):** Carry-over becomes feature-only. Add `!IsBug(m)` filter to carry-over ticket selection.

**`ComputeCarryOverMetrics` changes:**
- `carryOverSp` = sum(SP) for tickets that are carry-over (started, not completed, not removed, not bug, not excluded)
- `totalScopeSp` (denominator) = activeSp (feature tickets that transitioned to startStage during sprint, not removed, not excluded). This replaces `committedSpActive + addedSp`.
- `carryOverRate` = carryOverSp / totalScopeSp * 100

**Carry-over destination (spec BR22):**

Current: "Completed" = `completedStatuses.Contains(cm.FinalStatus)` in current sprint.

New: "Completed" = ticket has a transition to CycleTimeEndStage during the current sprint (transition-based). "Carried Again" = started in current sprint but not completed (transition-based carry-over check in current sprint). "Removed" and "Dropped" unchanged.

**Depends on:** Steps 1, 2.

### Step 6: Update DeveloperThroughputService to transition-based

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/DeveloperThroughputService.cs`

**Signature change:** `ComputeThroughput` gains `List<StatusTransition> statusTransitions` parameter.

**Per-developer per-sprint changes:**

Current:
- `spAssigned` = sum(SP) where !removed, !IsBug
- `spCompleted` = sum(SP) where !removed, completedStatuses.Contains(FinalStatus), !IsBug
- `ticketsDone` = count where completed, !IsBug
- `ticketsCarriedOver` = count where !completed, !IsBug

New (transition-based):
- `spAssigned` = sum(SP) where isStarted (active in sprint), !removed, !IsBug (replaces the total-assigned semantic with active-in-sprint)
- `spCompleted` = sum(SP) where isCompleted in sprint, !removed, !IsBug
- `completionPercent` = spCompleted / spAssigned * 100
- `ticketsDone` = count where isCompleted, !removed, !IsBug
- `ticketsCarriedOver` = count where isStarted AND !isCompleted, !removed, !IsBug

**Rolling average — `ComputeRollingAverage` signature changes:** The method gains parameters: `List<StatusTransition> statusTransitions` and `AppSettings settings`. Remove the existing `completedStatuses` parameter (no longer needed). The method resolves orderedStages/startIndex/endIndex from settings once, then for each historical sprint iterated backward, uses `TransitionAttributionChecker.IsCompletedInSprint` with that sprint's `StartDate`/`EndDate` to determine which tickets the developer completed in that sprint. The transitions list must cover all loaded sprints (the endpoint loads transitions for all sprint IDs passed to the service).

**Deltas:** Same formula, transition-based values.

**Depends on:** Steps 1, 2.

### Step 7: Update BugRatioService to transition-based

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/BugRatioService.cs`

**Signature changes:** `ComputeMultiSprint` and `ComputeSingleSprint` gain `List<StatusTransition> statusTransitions` parameter.

**`CompletedMemberships` replacement:**

Current: `completedStatuses.Contains(m.FinalStatus) && !removed && !excluded`

New: `isCompleted` (transition to CycleTimeEndStage during sprint) AND `!removed` AND `!excluded`. The method needs sprint start/end dates and the transition lookup.

**Bug vs Non-Bug split:** After identifying transition-based completed tickets, split by IsBug/!IsBug for bug ratio computation. Logic otherwise unchanged.

**Alert evaluation:** `EvaluateAlert` iterates sprints backward. Each sprint needs transition-based completion. The transitions for all sprints must be available.

**Depends on:** Steps 1, 2.

### Step 8: Update LeaderboardService to transition-based

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/LeaderboardService.cs`

**Signature changes:** `ComputeMultiSprint` and `ComputeSingleSprint` gain `List<StatusTransition> statusTransitions` parameter.

**Changes:** Replace `completedStatuses.Contains(m.FinalStatus)` in `CompletedMemberships` with transition-based completion check (isCompleted in sprint). Same pattern as BugRatioService (Step 7).

The leaderboard shows feature SP and bug SP completed — both use transition-based completion.

**Depends on:** Steps 1, 2.

### Step 9: Update EpicProgressService velocity to transition-based

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/EpicProgressService.cs`

**Signature change:** `ComputeEpicProgress` gains `List<StatusTransition> statusTransitions` and `List<Sprint> closedSprints` parameters (for sprint date ranges needed by transition checks).

**Dual mode (spec BR16):**
- **Progress tracking** (total done, completion %) — UNCHANGED. Uses `completedStatuses.Contains(t.CurrentStatus)` (position-based check on ticket's current status). This is not sprint attribution — it measures current state.
- **Velocity tracking** (SP completed per sprint) — CHANGED. The sprint where the CycleTimeEndStage transition occurred gets the velocity credit. Replace `completedStatuses.Contains(sm.FinalStatus)` in the velocity calculation with transition-based: for each sprint, find memberships whose ticket has a transition to endStage during that sprint's date range.

**Depends on:** Steps 1, 2.

### Step 10: Update CycleTimeService pre-filter + ExcludedDeveloperFilter

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/CycleTimeService.cs`
- `src/Services/Fokus/Fokus.API/Features/Analytics/ExcludedDeveloperFilter.cs`

**CycleTimeService changes (spec — cycle time pre-filter):**

Current pre-filter in `ComputeTicketCycleTime` (line 319-321):
```
completedMemberships = memberships.Where(m => m.RemovedAt == null && completedStatuses.Contains(m.FinalStatus))
```

New: Use transition-based completion. A ticket is included in cycle time calculations when it has a transition to CycleTimeEndStage (or beyond) during the sprint date range:
```
completedMemberships = memberships.Where(m => m.RemovedAt == null && TransitionAttributionChecker.IsCompletedInSprint(...).isCompleted)
```

The rest of cycle time computation (stage duration measurement using the `ResolveBoundaries` start/end stages) is UNCHANGED. The pre-filter determines which tickets enter the cycle time analysis; the duration measurement remains the same.

**ExcludedDeveloperFilter changes:**

Current (line 32-34):
```
completedStatuses.Contains(m.FinalStatus)
```

New: A developer is excluded (0% capacity, 0 completions) when they have no ticket with a transition to CycleTimeEndStage during the sprint. The filter needs `statusTransitions` and sprint dates as additional parameters.

**Signature change:** `GetExcludedDeveloperIds` gains `List<StatusTransition> statusTransitions` parameter. Also needs `AppSettings settings` (for boundary resolution) and sprint dates.

**Depends on:** Steps 1, 2.

### Step 11: Update all analytics endpoints to load StatusTransitions

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetSprintSummary/GetSprintSummaryEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetScopeChange/GetScopeChangeEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetCarryOver/GetCarryOverEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetDeveloperThroughput/GetDeveloperThroughputEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetBugRatio/GetBugRatioEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetLeaderboard/GetLeaderboardEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetCycleTime/GetCycleTimeEndpoint.cs`

**Pattern for each endpoint:**

After loading sprints with memberships (existing `GetSprintsWithMembershipsAsync` call), add:
```
var statusTransitions = await ticketRepository.GetStatusTransitionsForSprintTicketsAsync(sprintIds, ct);
```

Pass `statusTransitions` to the service method call and to `ExcludedDeveloperFilter.GetExcludedDeveloperIds`.

**Endpoints already loading transitions:** GetScopeChangeEndpoint (single-sprint only, via ticket ID list) and GetCycleTimeEndpoint. These switch to the new `GetStatusTransitionsForSprintTicketsAsync` method and pass transitions to the service. The existing `GetStatusTransitionsForTicketsAsync` calls are replaced.

**EpicProgress endpoint** (`GetEpicProgress/GetEpicProgressEndpoint.cs`): needs to load transitions for closed sprint tickets for velocity computation. Add the sprint-based transition loading.

**Performance:** Single additional DB query per endpoint request. For typical workloads (200 tickets, 1000 transitions), this adds ~5ms.

**Depends on:** Steps 2-10 (services expect the new parameter).

### Step 12: Update Knowledge Base entries

**Files to modify:**
- `docs/kb/cross-cutting.md` — Add section "Transition-Based Sprint Scope (TransitionBasedSprintScope)" documenting: the attribution rule (transition timestamps within sprint date range), the TransitionAttributionChecker utility, fallback behavior, feature-only scope surfaces, the relationship to CompletionChecker (retained for non-sprint-scope checks). Update "Feature-Only Metrics" section to reflect all scope surfaces are now feature-only. Update "Boundary-Driven Completion" section to note it's superseded for sprint scope attribution but retained for EpicProgress progress tracking and ExcludedFromScope filtering.
- `docs/kb/analytics/scope-change.md` — Replace formulas: committedSpActive uses transition-based "started" check, completedSp uses transition-based "completed" check. Update burnup section to reflect cumulative-transitions scope line. Add multi-sprint bug bars.
- `docs/kb/analytics/carry-over.md` — Update carry-over definition: "started but not completed this sprint" (transition-based). Note feature-only. Update carry-over destination to use transition-based completion.
- `docs/kb/analytics/health-score.md` — Update completion sub-score to note it uses transition-based completion %.
- `docs/kb/analytics/throughput.md` — Update spCompleted, ticketsDone, ticketsCarriedOver to transition-based definitions.
- `docs/kb/analytics/cycle-time.md` — Update pre-filter to note it uses transition-based completion.
- `docs/kb/analytics/bug-ratio.md` — Update "completed" filter to transition-based.
- `docs/kb/analytics/leaderboard.md` — Update completion filter to transition-based.
- `docs/kb/analytics/epic-progress.md` — Note dual mode: progress unchanged (position-based), velocity uses transition-based.
- `docs/kb/domain/settings.md` — Update CycleTimeStartStage description: now drives active/committed scope attribution across all analytics. Update CycleTimeEndStage description: now drives completion attribution across all analytics (supersedes F22 snapshot approach for sprint scope).

**Depends on:** Steps 3-11.

## Cross-Service Changes

None. Single-service system.

## Migration Notes

None. No data model changes. No EF migrations needed.

## Testing Strategy

### Core Attribution
1. **Started check:** Ticket transitions to "In Progress" (CycleTimeStartStage) during Sprint 26 date range. Verify it's counted as active in Sprint 26 only.
2. **Completed check:** Ticket transitions to "Testing" (CycleTimeEndStage) during Sprint 27 date range. Verify it's counted as completed in Sprint 27 only.
3. **No double-counting:** Carry-over ticket (started Sprint 26, completed Sprint 27). Verify SP counted once for active (Sprint 26) and once for completed (Sprint 27).
4. **Unstarted tickets:** Ticket stays in "To Do" the entire sprint (no qualifying transition). Verify not counted in any scope metric.
5. **Added + started:** Ticket added mid-sprint AND transitions to In Progress. Verify counted as "added."
6. **Added + not started:** Ticket added mid-sprint but stays in To Do. Verify NOT counted as "added."
7. **Stage-skipping:** Ticket goes from To Do directly to Testing. CycleTimeStartStage = In Progress. Testing index >= In Progress index. Verify counted as both started AND completed.

### Fallback Behavior
8. **CycleTimeStartStage not configured:** Defaults to first workflow stage (or first done status). Verify scope attribution uses first stage.
9. **CycleTimeEndStage not configured:** Defaults to first done status. Verify completion uses first done status.
10. **Unrecognized status:** Ticket transitions to a status not in orderedStages. Verify transition does not count.

### Feature-Only Scope
11. **All scope cards feature-only:** Bug tickets with qualifying transitions are excluded from Active SP, Completed SP, Added SP, Carry-Over SP.
12. **Bug bars separate:** Multi-sprint bar chart shows feature committed/completed bars plus separate bug SP completed bars.

### Burnup Chart
13. **Scope line starts near zero:** Scope line grows as tickets transition to startStage per day.
14. **Completed line cumulative:** Completed line grows as tickets transition to endStage per day.
15. **Bug area unchanged:** Bug remaining SP tracked separately.

### Cross-Feature Consistency
16. **Dashboard:** SP Completed, Completion %, Health Score, Carry-Over Rate all use transition-based values.
17. **Developer Throughput:** spCompleted, ticketsDone, ticketsCarriedOver, rolling average all transition-based.
18. **Carry-Over:** carry-over = started but not completed this sprint. Destination tracking uses transition-based completion.
19. **Bug Ratio:** Completed bugs use transition-based attribution. Alerts use transition-based.
20. **Epic Progress:** Velocity uses transition-based. Progress unchanged (position-based current status).
21. **Cycle Time:** Pre-filter includes tickets with transition-based completion during sprint.
22. **Leaderboard:** Completed filter uses transition-based.
23. **ExcludedDeveloperFilter:** "completed any ticket" uses transition-based.
24. **Settings change propagation:** Change CycleTimeStartStage/EndStage. Reload any page. Verify all metrics reflect new boundaries.

## Open Questions

None.
