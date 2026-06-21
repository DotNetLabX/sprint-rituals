# Planning-Gated Disruption

**Traces to:** `docs/product/v1.md` §5.2 (Sprint Scope Change & Disruption Analysis)
**Source:** Scratch
**Dependencies:** F10 (ScopeChangeDisruption), F23 (TransitionBasedSprintScope)
**Status:** Done
**Plan:** `docs/plans/PlanningGatedDisruption/plan.md`

---

## Purpose

The Committed SP cards and the Scope Burnup chart tell inconsistent stories. The Total card uses `activeSp + removedSp` — a synthetic number that doesn't answer a real-world question and contradicts F23 BR12, which specifies it should match the burnup's dashed line at planning close. Added SP and Removed SP count activity from sprint start, mixing planning-window adjustments with genuine mid-sprint disruption. A ticket reshuffled during planning, or added to the sprint but never started, inflates disruption metrics without reflecting actual team disruption.

This feature aligns the metrics into a coherent system:

- **Total** answers "what did we commit to?" — membership at planning close
- **Active** answers "what did we work on?" — transition-based, unchanged
- **Added / Removed** answer "what disrupted us?" — post-planning, cycle-filtered
- **Disruption Rate** answers "what fraction of our execution was unplanned?" — derived

The result is a three-level funnel — Total → Active → Completed — where each gap tells a distinct story, and the disruption metrics capture real disruption rather than sprint administration noise.

## Entities

No new entities. This feature changes how existing data is interpreted for metric computation.

**Settings used:**

- **planningWindowDays** (existing, AppSettings) — determines the planning cutoff: `sprint.StartDate.AddDays(planningWindowDays)`. Default: 2. Configurable per team.
- **CycleTimeStartStage** (existing, AppSettings) — determines when a ticket "enters the cycle." Unchanged.
- **CycleTimeEndStage** (existing, AppSettings) — determines when a ticket "completes." Unchanged.
- **Excluded-from-scope statuses** (existing, AppSettings) — tickets with matching FinalStatus excluded from scope metrics. Unchanged.

**Existing data queried:**

- Sprint memberships (WasCommitted, AddedAt, RemovedAt, StoryPoints, IssueType, FinalStatus)
- Status transitions (for cycle-entry checks)
- Sprints (StartDate, EndDate)
- App settings (planningWindowDays, CycleTimeStartStage, WorkflowStages, excluded statuses, DefaultSpPerBug)

## User Flows

```
Flow 1: View coherent sprint metrics
1. User opens Sprints page for a single sprint
2. Committed SP (Total) shows the feature SP in the sprint at the planning cutoff
3. Committed SP (Active) shows feature SP that entered the cycle during the sprint (unchanged)
4. Added SP shows only feature tickets added AFTER the planning window AND that entered the cycle
5. Removed SP shows only feature tickets removed AFTER the planning window AND that had entered the cycle
6. Net Scope Change and Disruption Rate derive from the filtered Added/Removed values
7. The Total card value matches the dashed line's value on the planning cutoff day in the burnup chart
8. The Active card value matches the final value of the orange scope line in the burnup chart
```

```
Flow 2: Compare funnel gaps
1. User sees Total: 175, Active: 132, Completed: 110
2. Total → Active gap (43 SP) = committed but never started — over-commitment signal
3. Active → Completed gap (22 SP) = started but not finished — carry-over signal
4. Added SP shows what unplanned work entered the cycle after planning
5. User uses these three gaps to drive three distinct retro conversations
```

```
Flow 3: Classification without Planning Overflow
1. User drills into single-sprint classification breakdown
2. Categories are: Unplanned Bug, Priority Escalation, Scope Injection
3. Planning Overflow no longer appears — tickets added during the planning window are not disruptions
4. All classified tickets are post-planning additions that entered the cycle
```

```
Flow 4: Team with different planning window
1. Team has planningWindowDays = 3 (longer planning phase)
2. Tickets added in the first 3 days of the sprint are not counted as Added SP
3. Tickets removed in the first 3 days are not counted as Removed SP
4. Total card reflects membership at day 3 (the team's planning cutoff)
5. The planning window length adapts disruption metrics to the team's actual planning cadence
```

```
Flow 5: Phantom scope changes visible only on chart
1. A ticket is added to the sprint after planning, never started, then removed
2. It does NOT appear in Added SP (never entered the cycle)
3. It does NOT appear in Removed SP (never entered the cycle)
4. The dashed line in the burnup chart bumps up when added, down when removed
5. The metric cards measure disruption; the chart provides the full membership picture
```

## API Surface

No new endpoints. Response shapes change for the existing scope change endpoint.

| Method | Route | Auth | Changes | Status codes |
|--------|-------|------|---------|--------------|
| GET | /api/analytics/scope-change | Authenticated | committedSpTotal, addedSp, removedSp, netScopeChange, disruptionRate, classification breakdown use new formulas | 200, 400 |
| GET | /api/analytics/sprint-summary | Authenticated | ScopeDisruptionRate, BugDisruptionRate, mid-sprint disruption flag use planningCutoff instead of sprintStart / hardcoded 2 days | 200, 400 |

**Response shape changes (single-sprint mode):**

Metric cards:
- `committedSpTotal` — was: activeSp + removedSp. Now: membership at planning cutoff (feature tickets in sprint at planningCutoff, non-bug)
- `activeSp` — unchanged (transition-based feature tickets that entered cycle, non-excluded)
- `addedSp` — was: AddedAt > sprintStart AND cycle-entered. Now: AddedAt > planningCutoff AND cycle-entered
- `removedSp` — was: all removed feature tickets. Now: RemovedAt > planningCutoff AND had entered cycle before removal
- `netScopeChange` — derived: new addedSp - new removedSp
- `disruptionRate` — derived: new addedSp / activeSp × 100

Classification breakdown:
- Three categories: Unplanned Bug, Priority Escalation, Scope Injection
- Planning Overflow removed
- Classification only applies to tickets that qualify as Added SP

Burnup data:
- Unchanged — dashed line, scope line, completed line, bug area all stay as-is

**Response shape changes (multi-sprint mode):**

- Same formula changes for per-sprint data
- Classification breakdown totals use three categories (no Planning Overflow)
- Summary averages derive from updated per-sprint values

## Business Rules

1. **Total = membership at planning close.** Committed SP (Total) is the sum of effective SP for all feature tickets present in the sprint at the planning cutoff. "Present at planning cutoff" means: AddedAt ≤ planningCutoff AND (RemovedAt is null OR RemovedAt > planningCutoff). WasCommitted tickets count regardless of AddedAt. Non-bug filter applied. No excluded-status filter applied — Total is raw membership, matching the dashed line which also applies no excluded-status filter (F23 BR12). This supersedes the F23 acceptance criterion "Committed SP (Total) shows Active + Removed feature tickets" and aligns with F23 BR12's statement that the Total metric card corresponds to the dashed line at the planning cutoff day.

2. **Active = transition-based, unchanged.** Committed SP (Active) remains the sum of effective SP for feature tickets that transitioned to CycleTimeStartStage (or beyond) during the sprint, not removed, not excluded. No change from F23 BR2.

3. **Added = post-planning AND cycle-entered.** Added SP counts feature tickets where: (a) AddedAt > planningCutoff, (b) the ticket has a transition to CycleTimeStartStage (or beyond) during the sprint, (c) not removed, (d) non-bug, (e) non-excluded. This supersedes F10 BR2 and narrows F23 BR5 by replacing sprintStart with planningCutoff as the addition threshold.

4. **Removed = post-planning AND had entered the cycle.** Removed SP counts feature tickets where: (a) RemovedAt > planningCutoff, (b) the ticket has a transition to CycleTimeStartStage (or beyond) within [sprintStart, RemovedAt] — meaning the transition occurred during the sprint and before the ticket was removed, (c) non-bug, (d) non-excluded. This supersedes F10 BR3 ("all removed tickets") and F23 BR6 ("Removed SP unchanged — no cycle time logic needed for removal").

5. **Net Scope Change = new Added - new Removed.** Formula unchanged, inputs narrowed. Supersedes F10 BR4 inputs.

6. **Disruption Rate = new Added / Active × 100.** Denominator remains Active SP (what the team actually worked on). When Active SP is 0, disruption rate is 0. Formula unchanged, inputs narrowed. Supersedes F10 BR5 inputs.

7. **Planning Overflow classification removed.** Classification categories become: Unplanned Bug, Priority Escalation, Scope Injection. Planning Overflow is no longer meaningful because tickets added during the planning window are excluded from Added SP entirely — they are not disruptions, they are planning. This supersedes F10 BR6 (classification priority order) and F10 BR7 (overlap resolution).

8. **Classification rules (revised priority order).** Each ticket in Added SP is classified into exactly one category:
   - **Unplanned Bug** — IssueType = "Bug"
   - **Priority Escalation** — Ticket.CreatedDate < sprint.StartDate (pre-existing work pulled in after planning)
   - **Scope Injection** — everything else (new work that didn't exist before the sprint)
   
   Priority: Bug check runs first. A bug created before sprint start is classified as Unplanned Bug, not Priority Escalation.

9. **Burnup chart unchanged.** The dashed Committed Total line, orange Scope line, green Completed line, and red Bug area all retain their current behavior. The dashed line is membership-based (F23 BR12). The scope and completed lines are transition-based (F23 BR11). No modifications.

10. **Total card / dashed line correspondence.** The Committed SP (Total) card value equals the dashed line's value on the planningCutoff day. Both use the same membership logic — the card snapshots it at one point in time, the line shows it over all days.

11. **Active card / scope line correspondence.** The Committed SP (Active) card value equals the final value of the orange Scope line. Both use transition-based cycle-entry logic. No change from current behavior.

12. **Carry-over rate unchanged.** Carry-over Rate = Carry-over SP / Active SP × 100 (F23 BR21). This feature does not change carry-over computation or its denominator.

13. **Multi-sprint mode follows.** Per-sprint data in multi-sprint mode uses the same new formulas. Summary averages derive from the updated per-sprint values. Classification breakdown totals use three categories (no Planning Overflow).

14. **planningWindowDays = 0 edge case.** When planningWindowDays is 0, planningCutoff = sprintStart. All post-start additions and removals that enter the cycle count as disruption. Correct for teams with no formal planning window.

15. **Effective SP unchanged.** All SP sums use GetEffectiveSp(defaultSpPerBug). Tickets with null effective SP are excluded from SP-based metrics. No change.

16. **Event table adapts.** The event table continues showing all add/remove events chronologically. Classification categories only appear for tickets that qualify as Added SP (post-planning AND cycle-entered). All other events — planning-window additions, post-planning additions that never entered the cycle, and all removals — show no classification category (empty or "—"). The event table is a complete audit trail; the classification column highlights which events were genuine disruptions.

17. **Sub-team filter unchanged.** All computations respect the sub-team filter (F10 BR15). The new formulas apply within the filtered ticket set.

18. **Deltas use new formulas.** Delta comparisons to the prior closed sprint use the new formulas. Historical sprints recompute on query (consistent with F23 BR18).

19. **Dashboard disruption rates use planningCutoff.** The Dashboard's Scope Disruption Rate and Bug Disruption Rate (SprintSummaryService) currently use sprintStart as the addition threshold. After this feature, both use planningCutoff — the same threshold as the Sprints page. A ticket added during the planning window does not count as "added" on either surface. The same metric name must produce the same number everywhere.

20. **Dashboard mid-sprint disruption flag uses planningWindowDays.** The Dashboard's "Mid-Sprint Disruption" flag currently uses a hardcoded 2-day cutoff. After this feature, it uses planningCutoff (sprint.StartDate.AddDays(planningWindowDays)) — the same configurable boundary that gates all disruption metrics. Teams with planningWindowDays = 3 won't see false positives on day 3; teams with planningWindowDays = 0 will correctly flag day 1 disruptions.

21. **Bug Count unchanged.** The Bug Count metric card on the Sprints page counts bugs completed during the sprint (transition to CycleTimeEndStage), not bugs added mid-sprint. This is unchanged. Bug Count is a parallel capacity signal independent of the Added/Removed disruption pipeline.

22. **Help file supersession.** This feature's help files (`help.tooltips.md`, `help.page.md`) supersede the corresponding sections in F10's and F23's help files for overlapping surfaces: Committed SP (Total), Added SP, Removed SP, Disruption Rate, Net Scope Change, and classification categories. During implementation, wire the updated tooltip text from this feature's help files, not the older ones.

## Supersession Map

| Superseded Rule | Was | Now | Why |
|-----------------|-----|-----|-----|
| F10 BR1 (Committed uses excluded-status filter) | Total applies excluded-status filter | Total is raw membership, no excluded-status filter | Matches dashed line behavior (F23 BR12) |
| F23 AC "Committed SP (Total) shows Active + Removed" | activeSp + removedSp | Membership at planning close | Aligns card with dashed line per F23 BR12 intent |
| F10 BR2 (Added = mid-sprint, not removed) | AddedAt > sprintStart | AddedAt > planningCutoff, cycle-entered | Planning-window additions are not disruption |
| F10 BR3 (Removed = all removed tickets) | All removed, any time | Post-planningCutoff, had entered cycle | Unstarted/pre-planning removals are not disruption |
| F23 BR5 (Added follows cycle time, sprint start) | AddedAt > Sprint.StartDate AND started | AddedAt > planningCutoff AND started | Planning cutoff replaces sprint start |
| F23 BR6 (Removed SP unchanged) | No cycle time logic for removal | Cycle-entry + post-planning required | Removed now matches Added's filtering logic |
| F10 BR6 (Classification — Planning Overflow first) | 4 categories, PO wins day 1-2 | 3 categories, PO removed | Not needed when Added is post-planning only |
| F10 BR7 (Classification priority resolves overlaps) | Bug > PO for timing overlap | Bug > Escalation > Injection | Simpler priority without PO |
| Dashboard ScopeDisruptionRate (SprintSummaryService) | Uses sprintStart as addition threshold | Uses planningCutoff | Same metric, same number, both surfaces |
| Dashboard BugDisruptionRate (SprintSummaryService) | Uses sprintStart as addition threshold | Uses planningCutoff | Same metric, same number, both surfaces |
| Dashboard mid-sprint disruption flag | Hardcoded 2-day cutoff | Uses planningWindowDays | Configurable planning window is the universal disruption boundary |

## Acceptance Criteria

### Committed SP (Total) Card

- [ ] Total shows sum of feature SP in the sprint at planningCutoff, not activeSp + removedSp
- [ ] Total matches the dashed line's value on the planningCutoff day in the burnup chart
- [ ] WasCommitted tickets included regardless of AddedAt
- [ ] Tickets added after planningCutoff are not included in Total
- [ ] Tickets removed before or at planningCutoff are not included in Total
- [ ] Non-bug filter applied
- [ ] No excluded-status filter applied (matches dashed line behavior)

### Committed SP (Active) Card

- [ ] Unchanged — feature tickets that transitioned to CycleTimeStartStage during sprint, not removed, not excluded
- [ ] Matches final value of orange scope line in burnup chart

### Display Format

- [ ] Committed SP (Total) card displays in format "Active: X | Total: Y" where Active is transition-based and Total is membership at planning close

### Added SP Card

- [ ] Only counts tickets where AddedAt > planningCutoff (not > sprintStart)
- [ ] Only counts tickets that entered the cycle (transition to CycleTimeStartStage or beyond during sprint)
- [ ] Not removed, non-bug, non-excluded
- [ ] Delta vs prior sprint uses the new formula

### Removed SP Card

- [ ] Only counts tickets where RemovedAt > planningCutoff
- [ ] Only counts tickets that had entered the cycle (transition to CycleTimeStartStage or beyond with timestamp ≤ RemovedAt)
- [ ] Non-bug, non-excluded
- [ ] Delta vs prior sprint uses the new formula

### Derived Metrics

- [ ] Net Scope Change = new Added SP - new Removed SP
- [ ] Disruption Rate = new Added SP / Active SP × 100
- [ ] Disruption Rate = 0 when Active SP = 0
- [ ] Both deltas use new formulas

### Classification

- [ ] Planning Overflow category removed from all surfaces
- [ ] Three categories remain: Unplanned Bug, Priority Escalation, Scope Injection
- [ ] Classification only applies to tickets in Added SP (post-planning, cycle-entered)
- [ ] Bug check runs before creation date check
- [ ] Planning-window events in event table show no classification category
- [ ] Post-planning additions that never entered the cycle show no classification category in event table

### Burnup Chart

- [ ] Dashed Committed Total line unchanged
- [ ] Scope line unchanged
- [ ] Completed line unchanged
- [ ] Bug area unchanged
- [ ] Total card value = dashed line value at planningCutoff day (verification check)

### Multi-Sprint Mode

- [ ] Per-sprint committedSpTotal uses membership at planning close
- [ ] Per-sprint addedSp, removedSp use post-planning + cycle-filtered formulas
- [ ] Classification breakdown uses three categories (no Planning Overflow)
- [ ] Summary averages derive from updated per-sprint values

### Dashboard (SprintSummaryService)

- [ ] Scope Disruption Rate uses planningCutoff as the addition threshold, not sprintStart
- [ ] Bug Disruption Rate uses planningCutoff as the addition threshold, not sprintStart
- [ ] Dashboard disruption rates match Sprints page disruption rate for the same sprint
- [ ] Mid-Sprint Disruption flag uses planningWindowDays (not hardcoded 2 days)

### Edge Cases

- [ ] planningWindowDays = 0: planningCutoff = sprintStart, all post-start cycle-entered activity = disruption
- [ ] No tickets entered cycle: Active = 0, Added = 0, Removed = 0, Disruption Rate = 0
- [ ] All additions during planning window: Added = 0, no classification entries
- [ ] Ticket added post-planning, started, then removed post-planning: counts in Removed only (not in Added due to "not removed" condition)
- [ ] Ticket in sprint at planning close, never started, removed post-planning: not in Removed (never entered cycle), dashed line captures the removal

## Out of Scope

- **Changing the dashed line behavior** — the burnup's Committed Total (dashed) line retains its current behavior (F23 BR12). No modifications.
- **Carry-over formula changes** — carry-over rate denominator stays Active SP (F23 BR21). Not affected by this feature.
- **New settings UI** — planningWindowDays already exists and is configurable. No new configuration surfaces.
- **Per-developer disruption attribution** — remains deferred (F10 Out of Scope).
- **Churn rate composite metric** — remains deferred (F10 Out of Scope).
- **Config guardrails for cycle settings** — the cycle start/end configuration is the team's lever for defining "active work." No validation warnings for specific choices.
