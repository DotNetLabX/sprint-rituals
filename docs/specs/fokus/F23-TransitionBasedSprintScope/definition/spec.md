# Transition-Based Sprint Scope

**Traces to:** `docs/product/v1.md` §4.4 (Rev 2 — transition-based sprint attribution), §5.1, §5.2, §5.3, §5.5, §5.6, §5.7
**Source:** Scratch
**Dependencies:** F3 (Settings System), F6 (Workflow Auto-Detection), F12 (Cycle Time — defines boundary settings), F22 (Boundary-Driven Completion — superseded for completion mechanism)
**Status:** Done
**Plan:** `docs/plans/TransitionBasedSprintScope/plan.md`

---

## Purpose

The cycle time boundaries — CycleTimeStartStage and CycleTimeEndStage — define where work starts and ends in the workflow. After F22, only the end boundary drives analytics (completion via FinalStatus snapshot). The start boundary is used only for cycle time duration measurement. Meanwhile, sprint scope metrics (Committed SP, Added SP, Completed SP) use snapshot-based attribution: the WasCommitted flag determines committed scope, and FinalStatus position determines completion. This double-counts carry-over tickets — a ticket In Progress in Sprint 26 that completes in Sprint 27 adds its SP to both sprints' committed totals and shows as completed in both.

This feature makes both cycle time boundaries the single universal control for all sprint scope attribution through StatusTransition timestamps. A ticket's "started" event belongs to the sprint where the transition to CycleTimeStartStage occurred. A ticket's "completed" event belongs to the sprint where the transition to CycleTimeEndStage occurred. Each transition timestamp falls in exactly one sprint — no double-counting.

The boundaries are the flexibility mechanism:
- CycleTimeStartStage = "To Do", CycleTimeEndStage = "Done" → traditional sprint tracking (widest scope)
- CycleTimeStartStage = "In Progress", CycleTimeEndStage = "Testing" → dev throughput (narrow scope)
- Any combination in between

## Entities

No new entities. This feature changes how existing StatusTransition data and cycle time settings are interpreted for sprint scope attribution.

**Settings reinterpretation:**

- **Cycle time start stage** (existing, F12) — currently used only for cycle time duration measurement. After this feature, it also defines the "started/active" threshold. A ticket is started/active in the sprint where it first transitions to this stage or any stage after it in the ordered stage sequence.
- **Cycle time end stage** (existing, F12) — currently used for cycle time measurement and position-based completion (F22). After this feature, completion becomes transition-based: a ticket is completed in the sprint where it first transitions to this stage or any stage after it. This supersedes F22's FinalStatus snapshot approach.
- **WasCommitted flag** (existing, SprintMembership) — retained as a stored field. No longer drives committed scope attribution.
- **FinalStatus** (existing, SprintMembership) — retained as a stored field. No longer drives completion attribution.

**Attribution rule change:**

- **Before (F22):** Committed = WasCommitted flag. Completed = FinalStatus at/after CycleTimeEndStage (snapshot at sprint end).
- **After:** Committed/Active = first transition to CycleTimeStartStage (or beyond) during sprint date range. Completed = first transition to CycleTimeEndStage (or beyond) during sprint date range.

Example with CycleTimeStartStage = "In Progress", CycleTimeEndStage = "Testing":

| Ticket | In Progress transition | Testing transition | Sprint 26 | Sprint 27 |
|---|---|---|---|---|
| PROJ-100 | Sprint 26 Day 3 | Sprint 26 Day 10 | Active + Completed | — |
| PROJ-101 | Sprint 26 Day 5 | Sprint 27 Day 4 | Active, carry-over | Completed only |
| PROJ-102 | Sprint 27 Day 2 | Sprint 27 Day 8 | — | Active + Completed |
| PROJ-103 | Never | Never | — | — (not counted) |

## Attribution Logic

Given: orderedStages = WorkflowStages + DoneStatuses (e.g., [To Do, In Progress, Testing, Done, Closed])

### Is ticket T "started/active" in sprint S?

1. Resolve startStage = CycleTimeStartStage ?? orderedStages[0]
2. Find startIndex = position of startStage in orderedStages
3. Collect all StatusTransitions for T where:
   - Timestamp >= S.StartDate AND Timestamp <= S.EndDate
   - position of ToStatus in orderedStages >= startIndex
4. If any qualifying transition exists → T is active/committed in S
5. The earliest qualifying transition determines the day T started (used for burnup scope line)

### Is ticket T "completed" in sprint S?

1. Resolve endStage = CycleTimeEndStage ?? first done status
2. Find endIndex = position of endStage in orderedStages
3. Collect all StatusTransitions for T where:
   - Timestamp >= S.StartDate AND Timestamp <= S.EndDate
   - position of ToStatus in orderedStages >= endIndex
4. If any qualifying transition exists → T is completed in S
5. The earliest qualifying transition determines the day T completed (used for burnup completed line)

### Is ticket T "added" in sprint S?

1. Check T's SprintMembership: AddedAt > S.StartDate (mid-sprint addition)
2. AND T is active/committed in S (passes the "started" check above)
3. Both conditions must be true — a mid-sprint addition that never enters the cycle is not a scope change

### Is ticket T "carry-over" in sprint S?

1. T is active/committed in S (started check passes)
2. AND T is NOT completed in S (completed check fails)
3. Carry-over = started but not finished in the same sprint

### Stage-skipping

A ticket transitioning from To Do directly to Testing (skipping In Progress) produces a single StatusTransition with ToStatus = Testing. If CycleTimeStartStage = In Progress, Testing's position (3) >= In Progress's position (2), so the transition satisfies both the started AND completed checks. The ticket is both active and completed in the sprint where this transition occurred.

### Unrecognized statuses

If ToStatus does not appear in orderedStages, its position is undefined. The transition does not satisfy either check. A ticket whose only transitions are to unrecognized statuses is neither started nor completed.

## User Flows

```
Flow 1: Sprint scope reflects transition-based attribution
1. User has CycleTimeStartStage = "In Progress", CycleTimeEndStage = "Testing"
2. User opens Sprints page for Sprint 26
3. Committed SP (Active) shows feature tickets that first transitioned to In Progress (or beyond) during Sprint 26
4. Completed SP shows feature tickets that first transitioned to Testing (or beyond) during Sprint 26
5. Tickets that stayed in To Do the entire sprint are not counted in any scope metric
6. All scope cards are feature-only
```

```
Flow 2: Carry-over completes in next sprint without double-counting
1. Ticket PROJ-101 transitions to In Progress during Sprint 26 but does not reach Testing
2. Sprint 26: PROJ-101 counted as active/committed, listed as carry-over
3. PROJ-101 reaches Testing during Sprint 27
4. Sprint 27: PROJ-101 counted as completed, NOT as active/committed
5. PROJ-101 SP counted once for active (Sprint 26) and once for completed (Sprint 27) — no double-counting
```

```
Flow 3: Added ticket follows cycle time logic
1. A ticket is added to Sprint 26 mid-sprint (after sprint start date)
2. The ticket transitions to In Progress during Sprint 26
3. It counts as "added" scope (mid-sprint start)
4. A different ticket is also added mid-sprint but stays in To Do
5. That ticket does NOT count as "added" — it never entered the cycle
```

```
Flow 4: User widens boundaries for traditional tracking
1. User changes CycleTimeStartStage to "To Do" and CycleTimeEndStage to "Done"
2. Active scope widens: tickets entering To Do during the sprint count as started
3. Completed scope widens: tickets reaching Done count as completed
4. Behavior approximates traditional sprint tracking
```

```
Flow 5: Multi-sprint bar chart with separate bug bars
1. User selects "Last 3 Sprints"
2. Bar chart shows feature-only committed (gray) and completed (green) bars
3. Separate bug bars show bug SP completed per sprint
4. Feature and bug scopes are visually distinct
```

```
Flow 6: Ticket skips stages
1. A ticket transitions directly from To Do to Testing (skipping In Progress)
2. CycleTimeStartStage = "In Progress"
3. The Testing transition is "at or after" In Progress in the ordered sequence
4. The ticket counts as both started AND completed in the sprint where the Testing transition occurred
```

## API Surface

No new endpoints. Response shapes for scope metrics change to reflect transition-based values. All scope metrics become feature-only.

| Method | Route | Auth | Changes | Status codes |
|--------|-------|------|---------|--------------|
| GET | /api/analytics/sprint-summary | Authenticated | SP Completed, Completion %, health score use transition-based attribution. Feature-only. | 200, 400 |
| GET | /api/analytics/scope-change | Authenticated | Committed, Completed, Added SP use transition-based attribution. Feature-only scope cards. Bar chart feature-only with separate bug bars. | 200, 400 |
| GET | /api/analytics/developer-throughput | Authenticated | SP Completed, Tickets Done use transition-based attribution | 200, 400 |
| GET | /api/analytics/carry-over | Authenticated | Carry-over = started this sprint, not completed this sprint | 200, 400 |
| GET | /api/analytics/bug-ratio | Authenticated | Completed bugs/features use transition-based attribution | 200, 400 |
| GET | /api/analytics/epic-progress | Authenticated | Velocity tracking uses transition-based attribution. Progress tracking unchanged (current status position-based). | 200, 400 |
| GET | /api/analytics/cycle-time | Authenticated | Pre-filter uses transition-based attribution | 200, 400 |

## Business Rules

1. **Attribution is transition-based.** A ticket's "started" and "completed" events are attributed to the sprint during whose date range the relevant StatusTransition occurred. The WasCommitted flag and FinalStatus snapshot are retained as stored fields but no longer drive scope attribution.

2. **Active/Committed = first transition to CycleTimeStartStage or beyond.** A ticket is active/committed in the sprint where it first transitions to CycleTimeStartStage (or any stage at/after it in the ordered stage sequence) within the sprint's start-to-end date range. "Or beyond" handles stage-skipping: a ticket going directly from To Do to Testing (skipping In Progress) counts as started when CycleTimeStartStage = In Progress.

3. **Completed = first transition to CycleTimeEndStage or beyond.** A ticket is completed in the sprint where it first transitions to CycleTimeEndStage (or any stage at/after it) within the sprint's date range. This supersedes F22 BR1 (position-based FinalStatus completion).

4. **No double-counting.** Each StatusTransition timestamp falls in exactly one sprint's date range. A carry-over ticket's start transition belongs to one sprint and its completion transition belongs to another. SP are counted once for active (in the start sprint) and once for completed (in the completion sprint).

5. **Added follows cycle time logic.** A ticket is an "added" scope change when it is added to the sprint after the sprint start date (AddedAt > Sprint.StartDate) AND has a transition to CycleTimeStartStage (or beyond) during the sprint. Tickets added mid-sprint that never enter the cycle are not counted as scope changes.

6. **Removed SP unchanged.** Removed tickets are those with RemovedAt set. Removal only happens for tickets in To Do or Blocked — never for tickets at/past CycleTimeStartStage. No cycle time logic needed for removal.

7. **Unstarted tickets are not counted.** Tickets in the sprint that have no transition to CycleTimeStartStage (or beyond) during the sprint's date range do not appear in active, committed, added, or completed metrics. They are backlog items present in the sprint but not engaged with.

8. **Boundary flexibility is the universal control.** CycleTimeStartStage and CycleTimeEndStage together control all sprint scope attribution. Widening boundaries (To Do → Done) approximates traditional tracking. Narrowing them (In Progress → Testing) measures dev throughput. The same two settings that control cycle time measurement now control every sprint metric.

9. **All scope surfaces are feature-only.** Committed SP (Active/Total), Added SP, Removed SP, Net Scope Change, Disruption Rate, Carry-Over Rate, Completed SP — all show feature tickets only. Bug metrics use dedicated surfaces (Bug Count card, bug bars in chart, Bug Ratio page). This supersedes F21's explicit deferral of feature-only Sprints page scope cards.

10. **Multi-sprint bar chart is feature-only with separate bug bars.** The bar chart shows feature-only committed and completed bars. Bug SP completed per sprint shown as separate bars alongside. This supersedes F22 BR7 (total-scope bar chart) and F21's explicit deferral of the feature-only multi-sprint bar chart.

11. **Burnup scope and completed lines are cumulative transitions, not plan-based.** The single-sprint burnup scope line shows cumulative feature tickets that have transitioned to CycleTimeStartStage (or beyond), tracked day by day. It starts near zero and grows as tickets enter the pipeline — this is an execution-oriented line, not the old plan-oriented line (which started at committed SP and stepped with additions/removals). Addition and removal events no longer drive the scope line directly; a started ticket appears in the scope line regardless of whether it was committed at sprint start or added mid-sprint. The completed line shows cumulative feature tickets that transitioned to CycleTimeEndStage (or beyond), tracked day by day. Bug area unchanged.

12. **Committed Total line is membership-based, not transition-based.** The burnup chart includes a dashed "Committed Total" line showing the total feature SP currently in the sprint each day. This line does NOT use cycle time settings or status transitions — it is purely membership-based. On each day, it sums the effective SP of all non-bug tickets present in the sprint (added on or before that day, not yet removed). WasCommitted tickets (those added during or before the planning window) are counted from day 1 regardless of their actual AddedAt timestamp. Mid-sprint additions appear from their AddedAt day. Removals reduce the line on the day they occur. No excluded-status filter is applied. This line typically starts high (all committed features), drops during planning as the sprint is cleaned up, and then tracks ongoing additions and removals during execution. The value at the end of the planning window corresponds to the Committed SP (Total) metric card.

13. **Carry-over = started but not completed in the same sprint.** A ticket is carry-over for sprint N when it has a transition to CycleTimeStartStage during sprint N but no transition to CycleTimeEndStage during sprint N. Carry-over is feature-only (supersedes F21 BR6 which kept carry-over total-scope). This replaces F22 BR9.

14. **Completion from previous sprint is not carry-over.** A ticket that was started in a previous sprint and completes in the current sprint adds to the current sprint's completed SP only. It is not carry-over for the current sprint — it was carry-over for the previous sprint where it started.

15. **Steady-state balance.** Carry-over SP flowing in (completions from previous sprints) roughly balances carry-over SP flowing out (started but not completed this sprint). Completion % stays near 100% in steady state and converges as carry-over rate decreases.

15. **Feature-only filtering layers on top.** Order: excluded-from-scope filtering first, then transition-based attribution, then bug/feature split. Same layering as F22 BR6, with transition-based attribution replacing snapshot-based. Excluded-from-scope filtering continues to check FinalStatus (the ticket's status at sprint end) against the excluded statuses list — a ticket in "Cancelled" at sprint end is excluded even if it had a transition to CycleTimeStartStage during the sprint. This filtering operates on the ticket's outcome, not its transitions.

16. **Epic Progress dual mode.** Progress tracking (total done, completion %) uses position-based check on the ticket's current status — is it at/after CycleTimeEndStage? Unchanged from F22 BR12 because progress measures current state, not sprint attribution. Velocity tracking (SP completed per sprint) uses transition-based attribution — the sprint where the CycleTimeEndStage transition occurred gets the velocity credit.

17. **Health score uses transition-based completion.** The completion sub-score uses transition-based completion %. Thresholds, weights, and the composite formula are unchanged.

18. **Deltas and sparklines use transition-based values.** Every delta comparison and sparkline data point uses transition-based attribution. Historical sprints recompute on query.

19. **Fallback when boundaries not configured.** When CycleTimeStartStage is not configured, scope attribution defaults to the first workflow stage (or first done status if no workflow stages). When CycleTimeEndStage is not configured, defaults to the first done status. Note: cycle time measurement (F12) defaults CycleTimeStartStage to the second workflow stage (first non-backlog stage) to skip the queue stage. This divergence is intentional — scope attribution captures all sprint engagement (including queue entry), while cycle time measures active development duration.

20. **Unrecognized statuses.** If a ticket's transition is to a status not in the ordered stage sequence, the transition does not count as reaching CycleTimeStartStage or CycleTimeEndStage. The ticket is not started or completed by that transition.

21. **Explicit formulas under transition-based model.** All formulas use feature-only, transition-based values:
- **Completion %** = Completed SP / Active SP × 100, where Active SP = feature tickets that transitioned to CycleTimeStartStage during the sprint, and Completed SP = feature tickets that transitioned to CycleTimeEndStage during the sprint. May exceed 100% when carry-over completions from previous sprints inflate the numerator (see BR14 steady-state balance).
- **Disruption rate** = Added SP / Active SP × 100, where Added SP = feature tickets added mid-sprint that entered the cycle (BR5), and Active SP = all feature tickets that transitioned to CycleTimeStartStage during the sprint. Note: Added is a subset of Active — added tickets that started are counted in both.
- **Carry-over rate** = Carry-over SP / Active SP × 100, where Carry-over SP = feature tickets started but not completed this sprint (BR12). Active SP is the denominator because under this model it represents all work the sprint engaged with (committed-at-start + added-mid-sprint that entered the cycle).
- **Net scope change** = Added SP − Removed SP (feature-only).

22. **Carry-over destination tracking adapts to transition-based attribution.** The carry-over destination feature (F11) tracks what happened to a prior sprint's carry-over tickets in the current sprint. Under this model, a carry-over ticket from sprint N-1 is "completed in sprint N" when it has a transition to CycleTimeEndStage during sprint N (not when its FinalStatus is in done statuses). Destination categories (completed, carried again, removed, dropped) all use transition-based checks.

## Acceptance Criteria

### Core Attribution

- [ ] A ticket that transitions to In Progress during Sprint 26 is counted as active/committed in Sprint 26 only
- [ ] A ticket that transitions to Testing during Sprint 27 is counted as completed in Sprint 27 only
- [ ] A carry-over ticket (started Sprint 26, completed Sprint 27) has SP counted once for active (Sprint 26) and once for completed (Sprint 27)
- [ ] A ticket that stays in To Do the entire sprint is not counted in any scope metric
- [ ] A ticket added mid-sprint that transitions to In Progress counts as "added"
- [ ] A ticket added mid-sprint that stays in To Do does NOT count as "added"
- [ ] A ticket skipping stages (To Do → Testing directly) counts as both started and completed in that sprint
- [ ] With CycleTimeStartStage not configured, defaults to first workflow stage
- [ ] With CycleTimeEndStage not configured, defaults to first done status

### Sprints Page — Scope Cards (F10)

- [ ] Committed SP (Active) shows feature-only tickets that transitioned to CycleTimeStartStage during the sprint
- [ ] Committed SP (Total) shows Active + Removed feature tickets
- [ ] Added SP shows feature tickets added mid-sprint that entered the cycle
- [ ] Removed SP unchanged (RemovedAt-based)
- [ ] Net Scope Change and Disruption Rate use transition-based values
- [ ] All scope cards are feature-only

### Sprints Page — Bar Chart (F10)

- [ ] Committed bar is feature-only, transition-based
- [ ] Completed bar is feature-only, transition-based
- [ ] Separate bug bars show bug SP completed per sprint
- [ ] Added and Removed bars are feature-only

### Sprints Page — Burnup (F10)

- [ ] Scope line shows cumulative feature tickets that transitioned to CycleTimeStartStage, per day
- [ ] Completed line shows cumulative feature tickets that transitioned to CycleTimeEndStage, per day
- [ ] Committed Total dashed line shows daily feature SP in sprint (membership-based, no transitions)
- [ ] Committed Total counts WasCommitted tickets from day 1 regardless of AddedAt
- [ ] Committed Total counts mid-sprint additions from their AddedAt day
- [ ] Committed Total decreases when tickets are removed
- [ ] Committed Total applies no excluded-status filter
- [ ] Committed Total at end of planning matches the Committed SP (Total) metric card
- [ ] Bug area unchanged

### Dashboard (F8)

- [ ] SP Completed uses transition-based attribution (feature-only)
- [ ] Completion % uses transition-based attribution (feature-only)
- [ ] Health score completion sub-score uses transition-based completion %
- [ ] Carry-over rate uses transition-based definition (started but not completed this sprint)
- [ ] Deltas and sparklines use transition-based values
- [ ] Developer leaderboard SP completed uses transition-based attribution (both Features and Bugs toggle modes)

### Developer Throughput (F9)

- [ ] SP Completed uses transition-based attribution (feature-only per F21)
- [ ] Completion % uses transition-based values
- [ ] Tickets Done and Tickets Carried Over use transition-based attribution
- [ ] Rolling averages use transition-based values

### Carry-Over (F11)

- [ ] Carry-over = tickets started this sprint (transitioned to CycleTimeStartStage) but not completed this sprint
- [ ] Carry-over rate = Carry-over SP / Active SP × 100 (feature-only)
- [ ] Status distribution reflects carry-over tickets' current statuses
- [ ] Carry-over destination tracking uses transition-based attribution (completed = transitioned to CycleTimeEndStage during current sprint)

### Bug Ratio (F13)

- [ ] Completed bugs and features use transition-based attribution

### Epic Progress (F14)

- [ ] Progress tracking (total done, completion %) unchanged — current status position-based
- [ ] Velocity tracking (SP completed per sprint) uses transition-based attribution

### Cycle Time (F12)

- [ ] Completed ticket pre-filter uses transition-based attribution

### Settings

- [ ] No new settings UI required
- [ ] Changing CycleTimeStartStage changes active/committed scope across all analytics on next page load
- [ ] Changing CycleTimeEndStage changes completion scope across all analytics on next page load

## Out of Scope

- **Removing WasCommitted or FinalStatus fields** — retained for storage and potential future use. May be reconsidered in a cleanup.
- **New Settings UI** — cycle time boundary dropdowns already exist. No new configuration surfaces needed.
- **Sprint-overlap handling** — if sprints have overlapping date ranges, a transition falls in the first matching sprint. Edge case deferred.
- **Burnup "expected completion" line** — adding a trend/projection line to the reshaped burnup is a separate enhancement.
- **Updating F22 spec** — this spec documents the supersession of F22's completion mechanism. F22's spec is not revised; its business rules are superseded where noted.
- **Renaming "Committed SP" labels** — the label could be misleading now that commitment is transition-based, but relabeling is cosmetic and deferred.
