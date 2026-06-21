# Boundary-Driven Completion

**Feature Spec:** `docs/features/BoundaryDrivenCompletion/spec.md`

## Context

All analytics features determine ticket completion by checking `doneStatuses.Contains(m.FinalStatus)` — a static list match against `AppSettings.DoneStatuses`. This produces incorrect numbers when the team considers an intermediate stage (e.g., "Testing") as dev-complete: tickets reaching Testing are counted as carry-over, deflating completion rates. Meanwhile, the cycle time boundary settings already define where work ends in the workflow but are only used by CycleTimeService for duration measurement.

This feature makes the cycle time end stage the single source of truth for completion across all analytics. A ticket is completed when its final status is at or after the cycle time end stage in the ordered stage sequence (WorkflowStages ++ DoneStatuses). The done statuses list is retained but no longer drives completion checks.

**Services impacted:** Fokus (sole service). Changes span a shared completion utility, 8 analytics services, ExcludedDeveloperFilter, and the burnup chart's done-transition lookup. No frontend changes. No API surface changes.

## Scope

**In scope:**
- Extract a shared `CompletionChecker` utility that encapsulates the position-based completion rule
- Replace every `doneStatuses.Contains(status)` completion check across all 8 analytics services and ExcludedDeveloperFilter with the boundary-driven check
- Replace the burnup chart's done-transition lookup in ScopeChangeService to use boundary-driven statuses
- Replace the cycle time pre-filter in CycleTimeService to use boundary-driven completion instead of done statuses
- Handle all fallback cases: no cycle time end stage configured, no workflow stages configured, end stage not in sequence
- Update KB entries

**Explicitly out of scope:**
- New endpoints, new settings UI, new entities
- Removing the done statuses setting (retained per spec)
- Renaming the "Done Statuses" label in Settings
- Updating other feature specs (spec says these are not revised)
- Bar chart vs burnup numerical discrepancy (tracked as separate bug)

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | (none) | — | Extract CompletionChecker utility with boundary resolution and position-based check | Cross-cutting analytics utility — no skill |
| 2 | (none) | — | Replace completion checks in SprintSummaryService | Service-specific inline changes |
| 3 | (none) | — | Replace completion checks in ScopeChangeService | Service-specific inline changes |
| 4 | (none) | — | Replace completion checks in CarryOverService | Service-specific inline changes |
| 5 | (none) | — | Replace completion checks in DeveloperThroughputService | Service-specific inline changes |
| 6 | (none) | — | Replace completion checks in BugRatioService | Service-specific inline changes |
| 7 | (none) | — | Replace completion checks in LeaderboardService | Service-specific inline changes |
| 8 | (none) | — | Replace completion checks in EpicProgressService | Service-specific inline changes |
| 9 | (none) | — | Replace completion check in CycleTimeService + ExcludedDeveloperFilter | Service-specific inline changes |
| 10 | (none) | — | Update KB entries | Documentation |

No skills exist for cross-cutting analytics computation changes or inline predicate replacement. All steps are "None" disposition because this feature modifies existing computation logic rather than creating new patterns. Logged as observation — a future `metric-query-patterns` skill (already in architecture doc gaps) would cover this pattern class.

## Domain Model Changes

None. No new entities, value objects, or domain events. This feature changes how existing settings are interpreted.

## Data Model Changes

None. No migrations needed. All changes are computation-level.

## Implementation Steps

### Step 1: Create CompletionChecker utility

**Files to create:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/CompletionChecker.cs`

Create a static utility class that encapsulates the boundary-driven completion rule. This class centralizes the logic that currently lives implicitly in CycleTimeService's `ResolveBoundaries` and is scattered as `doneStatuses.Contains(...)` across all services.

**Members:**

1. `ResolveCompletedStatuses(AppSettings settings) -> HashSet<string>` — returns the set of statuses considered "completed" under the boundary-driven definition. Logic:
   - Build ordered stage sequence: `WorkflowStages ++ DoneStatuses` (same construction as CycleTimeService.ResolveBoundaries, line 289-294)
   - Resolve end stage: `CycleTimeEndStage ?? DoneStatuses[0]` (same fallback as CycleTimeService, line 298-299)
   - Find the end stage's index in the ordered sequence
   - If end stage not found in sequence (cleared workflow stages after configuration — spec BR5): fall back to `DoneStatuses[0]` and re-resolve
   - If still not found (no done statuses at all): return empty set
   - Return all statuses from end stage index to end of sequence as a `HashSet<string>(StringComparer.OrdinalIgnoreCase)`

2. `IsCompleted(string status, HashSet<string> completedStatuses) -> bool` — `completedStatuses.Contains(status)`. This is a trivial wrapper but exists for readability and to match the old `doneStatuses.Contains(status)` call pattern.

**Fallback behavior (spec BR3, BR4, BR5):**
- No CycleTimeEndStage configured (null) AND DoneStatuses has entries: defaults to first done status — identical to pre-feature behavior
- No WorkflowStages configured (empty list): ordered sequence = DoneStatuses only — identical to pre-feature behavior
- CycleTimeEndStage not in ordered sequence: falls back to first done status

**Why a HashSet:** Every analytics service calls the completion check inside tight loops over memberships. Computing the set once per request and passing it through is the same performance pattern as the current `List<string> doneStatuses` but with O(1) lookup. The case-insensitive comparer matches the pattern used by CarryOverService and BugRatioService (which already use `StringComparer.OrdinalIgnoreCase`).

**Pattern reference:** CycleTimeService.ResolveBoundaries (line 289-302) for ordered stage construction and end stage fallback. The new utility extracts and generalizes this logic.

**Depends on:** Nothing.

### Step 2: Replace completion checks in SprintSummaryService

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs`

**Changes:**

1. At the top of `ComputeSummary` (line 97), replace `var doneStatuses = settings.DoneStatuses;` with:
   ```
   var completedStatuses = CompletionChecker.ResolveCompletedStatuses(settings);
   ```

2. Replace every `doneStatuses.Contains(m.FinalStatus)` with `completedStatuses.Contains(m.FinalStatus)` in:
   - `ComputeMetrics` — `featureCompleted` filter (line 253), `bugSpCompleted` filter (line 257), `carryOver` filter (line 272)
   - `ComputeSpCompleted` — line 289
   - `ComputeTopEpics` — sprint SP filter (line 465), epic ticket current status check (line 490)
   - `ComputeLeaderboard` — completed filter (line 519)
   - `ComputeFlags` — zero-SP devs completed check (line 605)

3. Update method signatures that pass `doneStatuses` to pass `completedStatuses` instead:
   - `ComputeMetrics(memberships, doneStatuses, defaultSpPerBug)` → `ComputeMetrics(memberships, completedStatuses, defaultSpPerBug)` (change parameter type from `List<string>` to `HashSet<string>`)
   - Same for: `ComputeSpCompleted`, `ComputeCompletionRate`, `ComputeCarryOverRate`, `BuildSparkline`, `ComputeTopEpics`, `ComputeLeaderboard`, `ComputeFlags`

4. `BuildSparkline` signature: change `List<string> doneStatuses` parameter to `HashSet<string> completedStatuses`. The sparkline lambda closures already capture the parameter — they just need the type change.

5. `ComputeCompletionRate` and `ComputeCarryOverRate` call `ComputeMetrics` internally — they pass through `completedStatuses`. No logic change, just the parameter type.

**What does NOT change:**
- `ComputeScopeDisruptionRate` and `ComputeBugDisruptionRate` — these do not reference doneStatuses at all (they compute from committed/added SP). Unchanged.
- Disruption rate and carry-over rate formulas — unchanged (denominators stay total-scope per spec BR7).

**Depends on:** Step 1.

### Step 3: Replace completion checks in ScopeChangeService

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/ScopeChangeService.cs`

**Changes:**

1. In `ComputeMultiSprint` (line 106), replace `var doneStatuses = settings.DoneStatuses;` with `var completedStatuses = CompletionChecker.ResolveCompletedStatuses(settings);`. Pass `completedStatuses` to `ComputePerSprintData` and `ComputeSprintMetrics`.

2. In `ComputeSingleSprint` (line 152), same replacement. Pass `completedStatuses` to `ComputeSprintMetrics` and `BuildBurnupData`.

3. In `ComputeSprintMetrics` — replace `doneStatuses.Contains(m.FinalStatus)` with `completedStatuses.Contains(m.FinalStatus)` in the `completedSp` filter (line 282). Change parameter type from `List<string>` to `HashSet<string>`.

4. In `ComputePerSprintData` — pass through the parameter type change.

5. **Burnup chart `BuildBurnupData`** — this method has a special pattern: it builds `doneTransitionByTicket` (line 409-412) by looking up `doneStatuses.Contains(t.ToStatus)` on StatusTransitions to find the first transition to a done status within the sprint window. This lookup determines on which day a ticket is counted as completed for the green line.

   Replace: the `doneStatuses.Contains(t.ToStatus)` filter in `doneTransitionByTicket` with `completedStatuses.Contains(t.ToStatus)`. This makes the burnup chart use the first transition to any completed status (at or after end stage) as the completion date.

   Change parameter type from `List<string> doneStatuses` to `HashSet<string> completedStatuses`. The burnup chart's daily iteration (`completedToday` and `bugCompletedToday` filters) does not directly check FinalStatus against doneStatuses — it uses `doneTransitionByTicket.TryGetValue(m.TicketId, out var ts)` to determine the day. The FinalStatus-based checks are already handled by the metric layer. No additional changes needed in the daily loop beyond the `doneTransitionByTicket` construction.

**What does NOT change:**
- `ComputeSprintMetrics` committed/added/removed SP calculations — unchanged (not completion-related)
- Classification logic — unchanged (categorizes additions, not completions)
- Event table — unchanged (shows additions/removals, not completions)
- Bug time-in-progress — unchanged (tracks active time, not completion)

**Depends on:** Step 1.

### Step 4: Replace completion checks in CarryOverService

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/CarryOverService.cs`

**Changes:**

1. In `ComputeMultiSprint` (line 119), replace `var doneStatuses = settings.DoneStatuses;` with `var completedStatuses = CompletionChecker.ResolveCompletedStatuses(settings);`. Pass to all downstream calls.

2. In `ComputeSingleSprint` (line 199), same replacement.

3. **`IsCarryOver` method** (line 296-297): change parameter from `List<string> doneStatuses` to `HashSet<string> completedStatuses`. Replace `!doneStatuses.Contains(m.FinalStatus, StringComparer.OrdinalIgnoreCase)` with `!completedStatuses.Contains(m.FinalStatus)` (the HashSet already uses OrdinalIgnoreCase from Step 1).

4. Update all callers of `IsCarryOver` to pass `completedStatuses` instead of `doneStatuses`:
   - `ComputeMultiSprint` — zombie loop (line 162), carry-over memberships (line 146)
   - `ComputeSingleSprint` — status distribution (line 234), issue type breakdown (line 240), tickets list (line 249)
   - `ComputePerSprintData` — line 358
   - `ComputeCarryOverMetrics` — line 318

5. **`BuildCarryOverDestination`** (line 474): replace `doneStatuses.Contains(cm.FinalStatus, StringComparer.OrdinalIgnoreCase)` (line 530) with `completedStatuses.Contains(cm.FinalStatus)`. Change parameter type. Also update the `IsCarryOver` call for prior memberships (line 488).

6. Update all method parameter types from `List<string> doneStatuses` to `HashSet<string> completedStatuses` for: `ComputeCarryOverMetrics`, `ComputePerSprintData`, `BuildCarryOverDestination`.

**Depends on:** Step 1.

### Step 5: Replace completion checks in DeveloperThroughputService

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/DeveloperThroughputService.cs`

**Changes:**

1. In `ComputeThroughput` (line 56), replace `var doneStatuses = settings.DoneStatuses;` with `var completedStatuses = CompletionChecker.ResolveCompletedStatuses(settings);`.

2. Replace all `doneStatuses.Contains(m.FinalStatus)` with `completedStatuses.Contains(m.FinalStatus)` in:
   - `spCompleted` filter (line 105)
   - `ticketsDone` filter (line 111)
   - `ticketsCarriedOver` filter (line 114)
   - Prior sprint `priorSpCompleted` (line 145), `priorTicketsDone` (line 151), `priorTicketsCarriedOver` (line 154)

3. **`ComputeRollingAverage`** (line 247): change `List<string> doneStatuses` parameter to `HashSet<string> completedStatuses`. Replace `doneStatuses.Contains(m.FinalStatus)` (line 271) with `completedStatuses.Contains(m.FinalStatus)`.

4. Update the call to `ComputeRollingAverage` (line 117) to pass `completedStatuses` instead of `doneStatuses`.

**Depends on:** Step 1.

### Step 6: Replace completion checks in BugRatioService

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/BugRatioService.cs`

**Changes:**

1. In `ComputeMultiSprint` (line 130), replace `var doneStatuses = settings.DoneStatuses;` with `var completedStatuses = CompletionChecker.ResolveCompletedStatuses(settings);`.

2. In `ComputeSingleSprint` (line 235), same replacement.

3. **`CompletedMemberships`** (line 383): change `List<string> doneStatuses` parameter to `HashSet<string> completedStatuses`. Replace `doneStatuses.Contains(m.FinalStatus, StringComparer.OrdinalIgnoreCase)` with `completedStatuses.Contains(m.FinalStatus)` (HashSet already uses OrdinalIgnoreCase).

4. Update all callers of `CompletedMemberships` to pass `completedStatuses`.

5. **`EvaluateAlert`** (line 336): change `List<string> doneStatuses` parameter to `HashSet<string> completedStatuses`. The internal call to `CompletedMemberships` passes it through.

6. Update all calls to `EvaluateAlert` to pass `completedStatuses`.

**Depends on:** Step 1.

### Step 7: Replace completion checks in LeaderboardService

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/LeaderboardService.cs`

**Changes:**

1. In `ComputeMultiSprint` (line 88), replace `var doneStatuses = settings.DoneStatuses;` with `var completedStatuses = CompletionChecker.ResolveCompletedStatuses(settings);`.

2. In `ComputeSingleSprint` (line 163), same replacement.

3. **`CompletedMemberships`** (line 252): change `List<string> doneStatuses` parameter to `HashSet<string> completedStatuses`. Replace `doneStatuses.Contains(m.FinalStatus, StringComparer.OrdinalIgnoreCase)` with `completedStatuses.Contains(m.FinalStatus)`.

4. Update all callers of `CompletedMemberships` to pass `completedStatuses`.

**Depends on:** Step 1.

### Step 8: Replace completion checks in EpicProgressService

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/EpicProgressService.cs`

**Changes:**

1. In `ComputeEpicProgress` (line 60), replace `var doneStatuses = settings.DoneStatuses;` with `var completedStatuses = CompletionChecker.ResolveCompletedStatuses(settings);`.

2. Replace all `doneStatuses.Contains(t.CurrentStatus)` with `completedStatuses.Contains(t.CurrentStatus)` in:
   - `doneTickets` filter (line 84)
   - `remainingTickets` filter (line 85)
   - `isCompleted` check (line 179)
   - `ticketEntries` IsDone flag (line 192)

3. Replace `doneStatuses.Contains(sm.FinalStatus)` with `completedStatuses.Contains(sm.FinalStatus)` in:
   - Velocity calculation — sprint completed SP filter (line 141)

**Spec note (BR12):** EpicProgressService uses `Ticket.CurrentStatus` for progress tracking and `SprintMembership.FinalStatus` for velocity. The boundary-driven check applies identically to both paths — the status (whichever one applies) must appear in the completed set.

**Depends on:** Step 1.

### Step 9: Replace completion checks in CycleTimeService and ExcludedDeveloperFilter

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/CycleTimeService.cs`
- `src/Services/Fokus/Fokus.API/Features/Analytics/ExcludedDeveloperFilter.cs`

**CycleTimeService changes:**

1. The existing `ResolveBoundaries` method (line 289-302) already builds the ordered stage sequence and resolves the end stage. It stays as-is for stage boundary resolution (start/end for cycle time duration measurement).

2. In `ComputeSingleSprint` and `ComputeMultiSprint`: after calling `ResolveBoundaries`, also compute `var completedStatuses = CompletionChecker.ResolveCompletedStatuses(settings);`.

3. In `ComputeTicketCycleTime` (line 306): change parameter `List<string> doneStatuses` to `HashSet<string> completedStatuses`. Replace:
   - `completedMemberships` filter (line 318): `doneStatuses.Contains(m.FinalStatus)` → `completedStatuses.Contains(m.FinalStatus)`
   - `doneTransition` lookup (line 343): `doneStatuses.Contains(t.ToStatus)` → `completedStatuses.Contains(t.ToStatus)` — the "earliest done transition" now means "earliest transition to a completed status" per spec BR13

4. Update both callers of `ComputeTicketCycleTime` to pass `completedStatuses` instead of `settings.DoneStatuses`.

**Spec note (BR13):** The cycle time end stage serves two purposes after this feature: (a) the completion pre-filter (which tickets are included in cycle time calculations) and (b) the measurement endpoint (where the cycle time duration stops). The pre-filter now uses the same boundary-driven definition as all other analytics — a ticket is included when its FinalStatus is at or after the end stage. The measurement boundary (start/end stages for duration accumulation) continues using `ResolveBoundaries` as before. Both use the same end stage value.

**ExcludedDeveloperFilter changes:**

5. In `GetExcludedDeveloperIds` (line 9): change parameter `List<string> doneStatuses` to `HashSet<string> completedStatuses`. Replace `doneStatuses.Contains(m.FinalStatus)` (line 33) with `completedStatuses.Contains(m.FinalStatus)`.

6. Update all callers of `GetExcludedDeveloperIds` across all endpoints to pass `completedStatuses` instead of `settings.DoneStatuses`. Grep for `GetExcludedDeveloperIds` to find all call sites — these are in the endpoint `HandleAsync` methods that call into the analytics services.

**Depends on:** Step 1.

### Step 10: Update Knowledge Base entries

**Files to modify:**
- `docs/kb/domain/settings.md` — Update the DoneStatuses row: change "All analytics — determines ticket completion" to "Contributes tail of ordered stage sequence. No longer directly used for completion checks (see cross-cutting.md)." Add note to CycleTimeEndStage row: "Also defines the completion threshold for all analytics (boundary-driven completion)."
- `docs/kb/cross-cutting.md` — Add section "Boundary-Driven Completion (BoundaryDrivenCompletion)" documenting: the completion rule (position-based against ordered stage sequence), the CompletionChecker utility, fallback behavior, and which setting now drives completion.
- `docs/kb/analytics/health-score.md` — Update "Completed = `FinalStatus IN doneStatuses`" to "Completed = `FinalStatus IN completedStatuses` (boundary-driven, see cross-cutting.md)."
- `docs/kb/analytics/scope-change.md` — Same completion definition update in `completedSp` formula and burnup chart section.
- `docs/kb/analytics/carry-over.md` — Update "A ticket is carried over if: `FinalStatus NOT IN doneStatuses`" to use completedStatuses.
- `docs/kb/analytics/throughput.md` — Same completion definition update.
- `docs/kb/analytics/cycle-time.md` — Update "Only completed tickets: `FinalStatus IN doneStatuses`" to use completedStatuses. Note that pre-filter and measurement boundary now use the same CycleTimeEndStage.
- `docs/kb/analytics/epic-progress.md` — Same completion definition update.
- `docs/kb/analytics/bug-ratio.md` — Same completion definition update.
- `docs/kb/analytics/leaderboard.md` — Same completion definition update.

**Depends on:** Steps 2-9.

## Cross-Service Changes

None. Single-service system.

## Migration Notes

None. No data model changes. No EF migrations needed.

## Testing Strategy

1. **Boundary-driven completion (core rule):** Configure WorkflowStages = [To Do, In Progress, Testing, Done, Closed] and CycleTimeEndStage = "Testing". A ticket with FinalStatus "Testing" must be counted as completed. A ticket with FinalStatus "In Progress" must be carry-over.
2. **End stage = "Done":** Same workflow. Ticket in "Testing" is carry-over. Ticket in "Done" or "Closed" is completed.
3. **End stage = "Closed":** Only "Closed" tickets are completed.
4. **No CycleTimeEndStage configured (null):** Defaults to first done status ("Done"). Behavior identical to pre-feature.
5. **No WorkflowStages configured (empty):** Ordered sequence = DoneStatuses only. Completion = DoneStatuses match. Behavior identical to pre-feature.
6. **CycleTimeEndStage not in ordered sequence:** Falls back to first done status.
7. **Unrecognized status:** Ticket with FinalStatus not in ordered sequence at all — not completed.
8. **Dashboard consistency:** SP Completed, Completion %, Health Score, deltas, sparklines all reflect boundary-driven definition.
9. **Sprints page consistency:** Multi-sprint bar chart completedSp uses boundary-driven definition (total-scope). Single-sprint burnup completedSp uses boundary-driven definition (feature-only per F21).
10. **Developer Throughput:** SP Completed, ticketsDone, ticketsCarriedOver, rolling average all use boundary-driven definition.
11. **Carry-Over:** carry-over = NOT completed by boundary-driven definition. Carry-over destination "Completed" bucket uses boundary-driven definition.
12. **Bug Ratio:** "Completed" bugs and stories filtered by boundary-driven definition. Alert evaluation uses boundary-driven definition.
13. **Epic Progress:** Done tickets use CurrentStatus against boundary-driven set. Velocity uses FinalStatus against boundary-driven set.
14. **Cycle Time:** Pre-filter includes tickets whose FinalStatus is in the completed set. Done-transition lookup finds the first transition to a completed status.
15. **Leaderboard:** Completed filter uses boundary-driven definition.
16. **ExcludedDeveloperFilter:** "completed any ticket" check uses boundary-driven definition.
17. **Case-insensitive matching:** Status "testing" (lowercase) matches CycleTimeEndStage "Testing" in the completed set.
18. **Changing end stage updates all analytics:** Change CycleTimeEndStage from "Testing" to "Done". Reload any analytics page. Verify completion counts decrease (fewer statuses qualify).

## Open Questions

None.
