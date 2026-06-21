# Feature-Only Delivery Metrics

**Traces to:** `docs/product/v1.md` §5.1 (Developer Throughput), §5.2 (Scope Change & Disruption), §5.7 (Sprint Summary Card)
**Source:** Scratch
**Dependencies:** F8 (Sprint Summary Card), F9 (Developer Throughput), F10 (Scope Change & Disruption), F16 (Bug Cost & Disruption Split), F17 (Burnup Bug Overlay), F19 (Leaderboard Breakdown)
**Status:** Done
**Plan:** `docs/plans/FeatureOnlyMetrics/plan.md`

---

## Purpose

Delivery metrics currently blend feature work and bug fixes into the same numbers. The Dashboard shows 199 SP completed when only ~109 are features — the rest are bugs that already have dedicated tracking surfaces (Bug Disruption Rate card, Bug SP burnup area, Bug Ratio tab, Leaderboard Bugs toggle). This double-representation inflates apparent delivery, distorts completion percentages, and makes it impossible to answer the core stakeholder question: "how many story points of features did we deliver?"

This feature separates delivery metrics from bug metrics across the Dashboard, burnup chart, and throughput table. The principle: **delivery surfaces show features, bug surfaces show bugs, the leaderboard bridges both.** Bug work remains fully visible — it is not hidden, just moved to the surfaces that are purpose-built for it.

Industry context: Jira includes bugs in all velocity metrics with no separation. Azure DevOps makes it configurable. Engineering intelligence platforms (LinearB, Swarmia, Pluralsight Flow) include bugs in raw velocity but show an investment distribution breakdown alongside. Scrum.org's Evidence-Based Management framework identifies bug-inflated velocity as erosion of Innovation Rate. Our approach — feature-primary with bug annotation on cards — gives stakeholders clean delivery numbers while preserving capacity visibility.

## Entities

No new entities. This feature changes how existing data is filtered and displayed across analytics surfaces. The bug classification rule is unchanged: issue type "Bug" = bug, everything else = non-bug (feature). Established by Bug Ratio (F13), used by Leaderboard Breakdown (F19) and Bug Cost & Disruption Split (F16).

## User Flows

```
Flow 1: View Dashboard with Feature-Only Metrics
1. User opens the Dashboard
2. The SP Completed card shows feature SP as the primary number
3. Below the primary number, a secondary annotation reads "(+X bug SP)" showing how much bug work was also completed
4. Completion % shows feature-only completion: feature SP completed / feature SP committed x 100
5. Health score badge computes its Completion sub-score from the feature-only completion %
6. Sparklines on SP Completed and Completion % cards reflect feature-only values across the trailing 4-sprint window
7. Deltas on SP Completed and Completion % compare feature-only values to the prior sprint
8. Scope Disruption Rate, Bug Disruption Rate, and Carry-Over Rate cards are unchanged
```

```
Flow 2: View Burnup Chart with Feature-Only Lines
1. User navigates to Sprints and selects a single sprint
2. The Scope Burnup chart renders:
   - Orange line (Scope SP): feature-only total scope — committed feature SP plus feature additions minus feature removals per day
   - Green line (Completed SP): feature-only completed SP — only non-bug tickets reaching done status
   - Red shaded area (Bug SP): remaining bug SP (unchanged from F17)
3. The legend shows: "Scope SP", "Completed SP", "Bug SP"
4. Tooltip on hover shows all three values for that day
5. The gap between orange and green represents unfinished feature work only — no bug noise
```

```
Flow 3: View Developer Throughput with Feature-Only Metrics
1. User navigates to Developers > Throughput tab
2. The table shows feature-only metrics per developer: feature SP assigned, feature SP completed, feature completion %, feature tickets done, feature tickets carried over
3. Deltas compare feature-only values to the prior sprint
4. The multi-sprint trend chart shows feature SP completed per developer (rolling 3-sprint average)
5. Developers who spent most of their sprint on bugs show lower feature throughput — their bug work is visible in the Bug Ratio tab and Leaderboard tab
```

```
Flow 4: Sub-Team Filter Interaction
1. User selects a sub-team from the toolbar filter
2. All feature-only metrics recalculate scoped to developers in that sub-team
3. Dashboard cards, burnup chart, and throughput table all filter consistently
4. Bug annotation on SP Completed card also scopes to the sub-team
```

```
Flow 5: Sprint with No Bugs
1. User selects a sprint with zero bug tickets
2. All metrics display identically to today — no annotation appears (or shows "+0 bug SP")
3. The burnup chart shows no red area, and the orange/green lines represent total scope (which equals feature scope when there are no bugs)
```

```
Flow 6: Sprint with All-Bug Work
1. User selects a sprint where all completed work was bugs
2. SP Completed card shows "0" as the primary number with "(+X bug SP)" annotation
3. Completion % shows 0% (no features completed against feature commitment)
4. Health score Completion sub-score reflects the 0% feature completion
5. Burnup chart orange line is flat or low (only feature scope), green line stays near zero
6. Red bug area shows the full bug burden
7. The leaderboard (F19) and Bug Ratio tab still show the full picture of who did what
```

## API Surface

### Modified endpoints

| Method | Route | Auth | Changes | Status codes |
|--------|-------|------|---------|--------------|
| GET | /api/analytics/sprint-summary | Authenticated | SP Completed and Completion % become feature-only; bug SP annotation added | 200, 400 |
| GET | /api/analytics/scope-change?sprintId={id} | Authenticated | Burnup data points gain feature-only scope and completed fields | 200, 400 |
| GET | /api/analytics/developer-throughput | Authenticated | All SP metrics become feature-only | 200, 400 |

No new endpoints. No changes to request parameters.

**Sprint summary response changes:**

The `MetricsResult` record changes:
- `SpCompleted` — becomes feature-only. Counts only non-bug completed tickets.
- `BugSpCompleted` — new field on `MetricsResult` (not on `MetricCard`, which is a generic structure shared across all cards). Provides the bug SP total for the annotation. No delta, no sparkline — it is a static number for the selected sprint. Bug SP trend context is already available on the Bug Disruption Rate card's sparkline.
- `CompletionRate` — becomes feature-only. Formula: feature SP completed / feature committed SP x 100. The denominator (committed SP) also excludes bugs, so the ratio reflects feature delivery against feature commitment.
- `ScopeDisruptionRate` — unchanged (already feature-only per F16). Its denominator remains total committed SP (see BR4a).
- `BugDisruptionRate` — unchanged (already bug-only per F16). Its denominator remains total committed SP (see BR4a).
- `CarryOverRate` — unchanged (uses total scope, including bugs — carry-over measures all unfinished work)

The health score's `completionSubScore` now uses the feature-only completion rate. Combined disruption and carry-over sub-scores are unchanged.

**Scope change response changes (single-sprint burnup):**

Only the `BurnupDataPoint` fields in single-sprint mode change:
- `totalScopeSp` — becomes feature-only. Cumulative committed feature SP + feature additions - feature removals as of that day.
- `completedSp` — becomes feature-only. Cumulative feature tickets reaching done status as of that day.
- `bugSp` — unchanged (remaining bug SP per F17)

Multi-sprint response fields (`ScopeChangePerSprintData`: `committedSpActive`, `committedSpTotal`, `addedSp`, `removedSp`, `completedSp`) remain total-scope (all ticket types). The Sprints page's disruption analysis requires total scope context — changing these fields to feature-only would break the disruption rate formula's meaning.

**Developer throughput response changes:**

Per developer per sprint:
- `spAssigned` — becomes feature-only. Sum of SP on non-removed non-bug tickets assigned to this developer.
- `spCompleted` — becomes feature-only. SP on non-bug tickets whose final status is in the done list.
- `completionPercent` — feature SP completed / feature SP assigned x 100.
- `ticketsDone` — becomes feature-only. Count of non-bug tickets with done final status.
- `ticketsCarriedOver` — becomes feature-only. Count of non-removed non-bug tickets with non-done final status.
- `rollingAverage` — uses feature-only SP completed.
- All delta values use feature-only comparisons.

**Error conditions:** No changes to existing error handling.

## Business Rules

1. **Bug classification is unchanged.** Issue type "Bug" (exact match, case-sensitive) = bug. All other issue types = non-bug (feature). Same rule used by F13, F16, and F19.

2. **Effective SP follows F16 rules.** A bug's effective SP is its actual estimate when present, or the configured default SP per bug when unestimated. This applies to the bug SP annotation, the burnup bug area, and all bug-specific surfaces. Feature tickets with no estimate remain excluded from SP metrics.

3. **Dashboard SP Completed shows feature SP primary, bug SP secondary.** The card's primary (large) number is feature SP completed. Below it, a secondary annotation reads "(+X bug SP)" where X is the total bug SP completed in that sprint. When bug SP is 0, the annotation is hidden (not shown as "+0 bug SP").

4. **Dashboard Completion % is feature-only.** Formula: feature SP completed / feature committed SP x 100. Both numerator and denominator exclude bugs. This measures how well the team delivered against its feature commitment — bugs are unplanned recovery work, not delivery.

4a. **Two committed SP values coexist.** The implementation must maintain two committed SP computations: **feature-committed SP** (excluding bugs — used by Completion % and SP Completed) and **total-committed SP** (including bugs — used by Scope Disruption Rate, Bug Disruption Rate, and Carry-Over Rate denominators). The feature-only filter must not be applied globally to the committed SP calculation. Disruption rate formulas (F16 BR5, BR6) are unchanged and continue to divide by total committed SP.

5. **Health score Completion sub-score uses feature-only completion %.** The completion sub-score input changes from mixed completion % to feature-only completion %. Thresholds, weights, interpolation, and the composite formula are unchanged. Disruption and carry-over sub-scores are unchanged.

6. **Carry-over rate remains unchanged (includes bugs).** Carry-over measures all unfinished work regardless of type. A bug that carries over is a capacity signal worth tracking. The carry-over rate formula and its health score sub-score are not modified.

7. **Burnup chart lines are feature-only.** The orange scope line tracks cumulative feature-only scope: committed feature SP on day 1, stepping up/down as feature tickets are added/removed. The green completed line tracks cumulative feature-only completions. The red bug area (F17) is unchanged.

8. **Burnup chart legend labels update.** "Total Scope SP" becomes "Scope SP". "Completed SP" remains "Completed SP". "Bug SP" remains "Bug SP". The implicit context is clear: scope and completed track features, bug SP tracks bugs.

9. **Developer Throughput metrics are feature-only.** SP assigned, SP completed, completion %, tickets done, and tickets carried over all exclude bug tickets. Bug work per developer is visible on the Bug Ratio tab and Leaderboard tab. Note: Throughput completion % uses "assigned" as its denominator (feature SP completed / feature SP assigned), not "committed" as on the Dashboard (feature SP completed / feature committed SP). This preserves the F9 formula semantics.

10. **Throughput rolling average uses feature-only SP.** The 3-sprint rolling average for SP completed excludes bug SP. Capacity-awareness (0% capacity exclusion) is unchanged.

11. **Excluded-from-scope filtering applies before bug/feature split.** Tickets with excluded final statuses are excluded from all calculations. Among the remaining tickets, bugs are separated from features. Order: exclude-from-scope first, then split by issue type.

12. **Sub-team filter applies consistently.** When a sub-team is selected, all feature-only and bug metrics scope to that sub-team's developers. The bug SP annotation on the Dashboard SP Completed card also scopes.

13. **Division by zero.** When feature committed SP is 0, feature completion % is 0. This can happen when all committed work is bugs. The health score handles 0% completion via its existing interpolation.

14. **Surfaces NOT affected.** Carry-Over Rate and its health sub-score (unchanged — tracks all work). Scope Disruption Rate (already feature-only per F16). Bug Disruption Rate (already bug-only per F16). Bug Ratio tab (explicitly bug-focused). Leaderboard tab (has its own Features/Bugs toggle). Epic Progress (epics are feature-scoped by nature). Cycle Time (measures duration, not SP aggregates). Sprints page metric cards and event table (use total scope for disruption analysis context). Multi-sprint trend view on Sprints page (unchanged). Dashboard Top Epics section (unchanged — ranks by SP completed from all ticket types; epics rarely contain bugs, and when they do, excluding them would misrepresent epic progress). Zero-SP developers flag (unchanged — uses total completed SP including bugs; a developer who completed only bug SP is not flagged as zero-SP, because the flag measures capacity, not feature output).

15. **Delta and sparkline consistency.** All deltas and sparklines on affected cards use feature-only values. The SP Completed card's delta compares feature SP completed to the prior sprint's feature SP completed. The sparkline shows a 4-sprint trailing window of feature SP completed values. The `bugSpCompleted` annotation has no delta or sparkline — it is a static number for the selected sprint. Bug SP trend context is already available on the Bug Disruption Rate card's sparkline.

## Acceptance Criteria

### Dashboard

- [ ] SP Completed card shows feature-only SP as the primary number
- [ ] SP Completed card shows "(+X bug SP)" annotation below the primary number
- [ ] Bug SP annotation is hidden when bug SP completed is 0
- [ ] Completion % card shows feature-only completion: feature SP completed / feature committed SP x 100
- [ ] Health score Completion sub-score uses feature-only completion %
- [ ] Health score composite and RAG status reflect the feature-only completion input
- [ ] SP Completed delta compares feature-only values to the prior sprint
- [ ] Completion % delta compares feature-only values to the prior sprint
- [ ] SP Completed sparkline shows feature-only values across the trailing 4-sprint window
- [ ] Completion % sparkline shows feature-only values across the trailing 4-sprint window
- [ ] Scope Disruption Rate card is unchanged
- [ ] Bug Disruption Rate card is unchanged
- [ ] Carry-Over Rate card is unchanged (includes bugs in both numerator and denominator)
- [ ] Sub-team filter scopes feature-only metrics and bug SP annotation to the selected sub-team
- [ ] Dashboard leaderboard (F19) is unchanged — retains its own Features/Bugs toggle
- [ ] Dashboard Top Epics section is unchanged (uses total SP including bugs)
- [ ] Zero-SP developers flag uses total completed SP (including bugs) — a developer with only bug SP is not flagged
- [ ] Bug SP annotation has no delta or sparkline

### Burnup Chart

- [ ] Orange line (Scope SP) tracks feature-only total scope per day
- [ ] Green line (Completed SP) tracks feature-only completed SP per day
- [ ] Red shaded area (Bug SP) is unchanged from F17
- [ ] Legend reads: "Scope SP", "Completed SP", "Bug SP"
- [ ] Tooltip shows all three values on hover
- [ ] Burnup chart with zero bugs displays identically to before (features = total scope)
- [ ] Sub-team filter scopes all three series consistently
- [ ] Multi-sprint trend view is unaffected

### Developer Throughput

- [ ] SP Assigned column shows feature-only assigned SP per developer
- [ ] SP Completed column shows feature-only completed SP per developer
- [ ] Completion % is feature SP completed / feature SP assigned x 100
- [ ] Tickets Done column counts feature tickets only
- [ ] Tickets Carried Over column counts feature tickets only
- [ ] All delta values compare feature-only metrics to the prior sprint
- [ ] Multi-sprint trend chart shows feature-only SP completed per developer
- [ ] Rolling average uses feature-only SP completed
- [ ] 0% capacity exclusion from rolling average is unchanged
- [ ] Sub-team filter scopes all feature-only metrics to the selected sub-team

### API

- [ ] GET /api/analytics/sprint-summary returns feature-only `SpCompleted` and `CompletionRate`
- [ ] GET /api/analytics/sprint-summary returns `bugSpCompleted` field on `MetricsResult` (not on `MetricCard`)
- [ ] `bugSpCompleted` is a static value — no delta, no sparkline
- [ ] GET /api/analytics/scope-change?sprintId={id} burnup data points use feature-only `totalScopeSp` and `completedSp`
- [ ] GET /api/analytics/scope-change?sprintId={id} burnup `bugSp` field is unchanged
- [ ] GET /api/analytics/developer-throughput returns feature-only SP and ticket metrics
- [ ] All delta and sparkline values use feature-only computations
- [ ] Division by zero (0 feature committed SP) produces 0% completion
- [ ] Scope-change multi-sprint response fields (`committedSpActive`, `completedSp`, etc.) remain total-scope
- [ ] Disruption rate denominators use total committed SP (not feature-only)

### Cross-Cutting

- [ ] Bug classification rule unchanged: IssueType "Bug" = bug, all others = feature
- [ ] Effective SP (F16) applies to bug SP annotation and bug-specific surfaces
- [ ] Excluded-from-scope filtering applies before bug/feature split
- [ ] Carry-Over Rate unaffected (includes all ticket types)
- [ ] Bug Ratio tab unaffected
- [ ] Leaderboard tab unaffected
- [ ] Epic Progress unaffected
- [ ] Cycle Time unaffected

## Out of Scope

- **Carry-over rate bug/feature split** — carry-over measures all unfinished work. Splitting it would require a separate "feature carry-over" concept and duplicate the pattern without clear stakeholder demand. If bugs carry over, that's a capacity signal worth keeping in the combined number.
- **Throughput bug toggle** — no Features/Bugs toggle on the Throughput tab. The Leaderboard tab already provides this breakdown. Adding a toggle to Throughput would duplicate the Leaderboard's purpose.
- **Sprints page metric cards feature-only** — the Sprints page disruption analysis needs total scope context (committed SP denominator includes all work). Changing it to feature-only would break the disruption rate formula's meaning.
- **Historical comparison mode** — no "before/after" view showing how metrics changed with bug exclusion. The change applies uniformly going forward.
- **Configurable bug inclusion toggle** — no global setting to switch between "include bugs" and "exclude bugs" modes. The separation is a design decision, not a user preference. Teams that want total throughput can see it on the Leaderboard tab.
