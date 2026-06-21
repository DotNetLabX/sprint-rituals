# Test Execution Timeline

**Traces to:** `docs/product/v2.md` §5.6
**Source:** Scratch
**Dependencies:** F25 (Xray Integration & Ticket Test Enrichment), F26 (Sprint Test Coverage — QA metric cards, health score Quality sub-score), F10 (Scope Change & Disruption — Sprints page, burnup chart, event timeline), F12 (Cycle Time — stage boundary timestamps for dev-done-to-tested gap)
**Status:** Done
**Plan:** `docs/specs/F30-TestExecutionTimeline/delivery/plan.md`

---

## Purpose

Fokus surfaces sprint-level test coverage and pass rates (F26) but not when those tests were executed relative to the sprint lifecycle. A sprint where all tests pass on day 2 is healthier than one where all tests pass on the last day. This feature adds a test execution burnup chart, a testing crunch flag, a post-sprint testing indicator, and a dev-done-to-tested gap metric to the Sprints page single-sprint detail view. It answers "when did testing happen?" and correlates testing activity with scope disruption events from F10 — making the relationship between delivery instability and testing delays visible in a single view.

## Entities

No new entities. F30 reads from entities introduced by F25 and extended by F26:

- **TestRun** — individual pass/fail result per test execution. F30 uses the `FinishedAt` timestamp (falling back to `StartedAt` when `FinishedAt` is null) to determine when each test run completed. Also uses `Status` to filter terminal-status runs (PASS or FAIL).
- **TestExecution** — Xray test execution issue, linked to sprint tickets via TestExecutionLink. F30 uses the sprint derivation rule (TE belongs to the sprint of its linked stories).
- **TestExecutionLink** — pivot between TEs and tickets (linkType = Tests for coverage).
- **Sprint** — provides `StartDate` and `EndDate` for sprint phase boundaries.
- **SprintMembership** — provides `AddedAt` and `RemovedAt` for scope change event dates used in the disruption correlation overlay.
- **StatusTransition** — provides the timestamp when a ticket reached the cycle time end boundary, used to compute the dev-done-to-tested gap.

### Settings Extensions

No new settings fields. F30 reuses:
- **PlanningWindowDays** (existing, default 2) — defines the planning phase boundary on the burnup chart
- **Xray Enabled** (existing) — gates the entire feature
- **CycleTimeEndStage** (existing) — defines "dev done" for the gap metric

## User Flows

```
Flow 1: View Test Execution Burnup on Sprints Page
1. User navigates to the Sprints page and selects a single sprint (entering single-sprint detail view)
2. Below the existing scope burnup chart and event table, system displays a "Test Execution Timeline" section (visible only when Xray is enabled and the sprint has QA data)
3. A burnup chart shows cumulative test runs completed per sprint day:
   - X-axis: sprint days (day 1 through sprint end, matching the scope burnup's axis)
   - Y-axis: cumulative test runs completed (count)
   - Line series colored by status: green line for cumulative PASS runs, red line for cumulative FAIL runs, gray line for cumulative total (PASS + FAIL)
   - Phase shading in the background: "planning" zone (days 1 through PlanningWindowDays), "execution" zone (remaining days), "testing crunch" zone (last 2 days before sprint end) highlighted with a distinct background shade
4. Scope disruption events appear as vertical markers on the chart: each day that had tickets added or removed mid-sprint shows a marker with the net SP change (+3 SP, -2 SP), visually connecting testing activity with delivery disruptions
5. If any test runs completed after the sprint end date, a post-sprint trailing section appears to the right of the sprint end boundary, visually separated
```

```
Flow 2: View Testing Crunch Flag on Dashboard
1. User navigates to Dashboard with a sprint selected
2. When Xray is enabled and the sprint has QA data, the flags section includes a "Testing crunch" flag (alongside existing zombie tickets, mid-sprint disruption, and zero-SP developer flags)
3. The testing crunch flag fires when more than 50% of terminal-status test runs within the sprint boundary completed in the last 2 calendar days before sprint end
4. Flag content shows: percentage of test runs in the last 2 days, run count ("18 of 24 test runs completed in the last 2 days")
5. When the flag does not fire, it is absent from the flags section (same pattern as other flags)
```

```
Flow 3: View Testing Crunch Detail on Sprints Page
1. In single-sprint detail view, below the test execution burnup chart, an expandable "Testing crunch" section appears when the crunch flag fires
2. Section header shows: "Testing crunch: X% of test runs in the last 2 days (N of M runs)"
3. Expanding the section reveals a ticket list: ticket key, summary, assignee, SP for each ticket whose test runs completed in the last 2 days
4. List sorted by number of late test runs descending (tickets with the most late testing first)
```

```
Flow 4: View Post-Sprint Testing
1. In single-sprint detail view, below the testing crunch section, a "Post-sprint testing" section appears when any test runs completed after the sprint end date
2. Section header shows: "Post-sprint testing: X% of test runs (N of M runs completed after sprint close)"
3. Expanding the section reveals a ticket list: ticket key, summary, assignee, SP, count of post-sprint runs for each affected ticket
4. List sorted by post-sprint run count descending
5. When no test runs completed after sprint end, the section is hidden
```

```
Flow 5: View Completed-but-Untested Tickets
1. In single-sprint detail view, below the post-sprint testing section, a "Completed but untested" section appears when any tickets reached the cycle time end boundary before the sprint EndDate but had no terminal-status test runs by sprint close
2. Section header shows: "Completed but untested by sprint close: N tickets"
3. Expanding the section reveals a ticket list: ticket key, summary, assignee, SP, date ticket reached cycle time end boundary
4. List sorted by dev-done date ascending (earliest completed tickets first — they had the most time available for testing)
5. When all dev-complete tickets have at least one terminal test run, the section is hidden
```

```
Flow 6: View Dev-Done-to-Tested Gap
1. In single-sprint detail view, below the completed-but-untested section, a "Dev-to-test gap" section appears
2. A summary metric card shows: median gap in calendar days across all eligible tickets in the sprint (with delta vs prior sprint, direction arrow, polarity: lower is better)
3. Below the metric card, a per-ticket table shows: ticket key, summary, assignee, date ticket reached cycle time end boundary, date of first terminal-status test run, gap in days (decimal, one decimal place)
4. Table sorted by gap descending (longest waits first)
5. Tickets are only included when they have both a cycle time end timestamp AND at least one terminal-status test run with a non-null FinishedAt
6. When no eligible tickets exist, the section shows "No tickets with both completion and test data"
```

```
Flow 7: Filter by Sub-Team
1. User selects a sub-team from the page toolbar filter
2. All F30 content recalculates scoped to tickets assigned to developers in that sub-team:
   - Burnup chart counts only test runs on filtered tickets
   - Scope disruption markers reflect only filtered tickets' add/remove events
   - Testing crunch flag, post-sprint testing, completed-but-untested, and dev-to-test gap scope to filtered tickets
3. Selecting "All" removes the filter
```

```
Flow 8: Empty States
8a. Xray disabled: entire "Test Execution Timeline" section not rendered
8b. Xray enabled, sprint not synced with QA data: section shows "Sync sprint to load QA data" prompt (matching F26 pattern)
8c. Xray enabled, synced, zero test runs: burnup chart empty (no lines), testing crunch, post-sprint, and completed-but-untested sections hidden, dev-to-test gap shows "No tickets with both completion and test data"
```

## API Surface

| Method | Route | Auth | Request body | Response | Status codes |
|--------|-------|------|-------------|----------|--------------|
| GET | /api/sprints/{sprintId}/test-timeline | Authenticated | -- | TestTimelineResponse | 200, 404 |

Accepts optional `?subTeam=X` query parameter.

**TestTimelineResponse:**
- hasQaData: boolean (false when Xray disabled or sprint not synced)
- sprintStartDate: date
- sprintEndDate: date
- planningWindowDays: integer

**Burnup data** (list, one entry per day from sprint start through the last day with test activity, including post-sprint days):
- dayNumber: integer (1-based, days after sprint end continue incrementing)
- calendarDate: date
- isWithinSprint: boolean (false for post-sprint days)
- cumulativePass: integer
- cumulativeFail: integer
- cumulativeTotal: integer
- dailyPass: integer (runs completed on this specific day)
- dailyFail: integer

**Scope change overlay** (list, only days with scope events):
- dayNumber: integer
- calendarDate: date
- addedSp: decimal
- removedSp: decimal
- netSp: decimal (addedSp - removedSp)

**Testing crunch:**
- isCrunchFlagged: boolean
- crunchPercentage: decimal (nullable, null when no terminal runs exist)
- crunchRunCount: integer
- totalRunCount: integer
- crunchTickets: list of { ticketKey, summary, assigneeName, storyPoints, lateRunCount }

**Post-sprint testing:**
- hasPostSprintTesting: boolean
- postSprintPercentage: decimal (nullable)
- postSprintRunCount: integer
- postSprintTickets: list of { ticketKey, summary, assigneeName, storyPoints, postSprintRunCount }

**Completed but untested:**
- hasUntestedAtClose: boolean
- untestedAtCloseCount: integer
- untestedAtCloseTickets: list of { ticketKey, summary, assigneeName, storyPoints, devDoneDate }

**Dev-to-test gap:**
- medianGapDays: decimal (nullable, null when no eligible tickets)
- medianGapDelta: decimal (nullable, delta vs prior sprint)
- medianGapDirection: string (up|down|flat|null)
- gapTickets: list of { ticketKey, summary, assigneeName, devDoneDate, firstTestDate, gapDays }

**Existing endpoint extension:**

| Method | Route | Change |
|--------|-------|--------|
| GET | /api/analytics/sprint-summary | Flags object gains optional `testingCrunch` field: `{ crunchPercentage, crunchRunCount, totalRunCount }`. Null when Xray is disabled, sprint has no QA data, or crunch flag does not fire. |

**Error conditions:**
- 404 when sprintId does not exist
- 200 with hasQaData=false and null/empty fields when Xray is disabled or no QA data

## Business Rules

### Burnup Chart

1. **Test run dating uses FinishedAt with StartedAt fallback.** Each test run is assigned a date from its FinishedAt timestamp. When FinishedAt is null, StartedAt is used. When both are null, the run is excluded from the burnup chart (it has no temporal signal).

2. **Only terminal-status test runs are plotted.** Runs with status PASS or FAIL are included in the burnup. TODO, EXECUTING, and ABORTED runs are excluded — they represent incomplete testing with no result date.

3. **Cancelled Test Executions are excluded.** Consistent with F26 rule 7, TEs marked as cancelled are excluded from all F30 computations.

4. **Sprint day assignment.** A test run's sprint day is computed from its effective date (FinishedAt or StartedAt) relative to the sprint's StartDate. Day 1 is the sprint start date. Days after the sprint EndDate continue incrementing but are marked as post-sprint (isWithinSprint = false).

5. **Phase zones on the burnup chart.** Three background zones: planning (day 1 through PlanningWindowDays, matching the configurable setting), execution (PlanningWindowDays + 1 through sprint end minus 2 days), and testing crunch (last 2 calendar days before sprint EndDate). The testing crunch zone uses a fixed 2-day window.

6. **No ideal line.** Unlike the Zephyr burndown pattern, the burnup chart has no ideal/expected execution line. Test run counts are not predictable in advance — Fokus discovers TEs via issue links after sync, not from a pre-planned test suite. The chart's value is in revealing the shape of execution: front-loaded (healthy), back-loaded (crunch), or trailing (post-sprint).

7. **Cumulative lines.** The green line accumulates PASS runs day-over-day. The red line accumulates FAIL runs. The gray total line is the sum of both. Lines only increase (or stay flat) — they never decrease.

### Scope Change Overlay

8. **Lightweight scope data.** The overlay includes only the day number, date, and SP added/removed/net for each day that had scope change events. This is derived from the same SprintMembership data that F10 uses, but reduced to per-day aggregates rather than per-ticket events.

9. **Vertical markers, not a secondary axis.** Scope change events appear as discrete markers at the relevant day positions on the burnup chart, annotated with the net SP change. They do not use a secondary Y-axis — their purpose is positional correlation ("testing crunch started the same day 3 tickets were added"), not magnitude comparison.

### Testing Crunch Flag

10. **Threshold: >50% of terminal runs in the last 2 calendar days before sprint EndDate.** The denominator is all terminal-status (PASS + FAIL) test runs within the sprint boundary (between StartDate and EndDate inclusive). The numerator is terminal-status runs whose effective date falls within the last 2 calendar days before (and including) EndDate. The flag fires when crunchPercentage > 50.

11. **Post-sprint runs are excluded from the crunch calculation.** Runs completed after the sprint EndDate are a separate signal (post-sprint testing) and do not contribute to either the numerator or denominator of the testing crunch percentage.

12. **Testing crunch flag appears on both Dashboard and Sprints page.** The Dashboard flags section (F8) gains a testing crunch flag gated behind Xray enabled and QA data presence. The Sprints page shows the full detail including the affected ticket list.

13. **Testing crunch threshold is fixed at 2 days.** This mirrors the planning window approach — a simple, fixed threshold. Making it configurable is deferred.

### Post-Sprint Testing

14. **Post-sprint runs are those with effective date after sprint EndDate.** The sprint EndDate is the boundary. Runs finishing on EndDate are within the sprint; runs finishing after are post-sprint.

15. **Post-sprint percentage denominator is all terminal runs for the sprint.** This includes both within-sprint and post-sprint runs. The metric answers "what fraction of this sprint's testing happened after the sprint closed?"

16. **Post-sprint section is hidden when no post-sprint runs exist.** No empty state message — the section simply does not render.

### Completed but Untested at Sprint Close

17. **Identifies dev-complete tickets with no test results by sprint end.** A ticket qualifies when: (a) it reached CycleTimeEndStage before the sprint EndDate (development completed within the sprint), AND (b) it has zero terminal-status (PASS or FAIL) test runs with an effective date on or before sprint EndDate. The ticket may have linked TEs — what matters is whether any test run actually completed, not whether TEs exist.

18. **Distinct from F26's untested tickets list.** F26's "untested tickets" are tickets with no linked TEs at all (zero coverage). This flag catches a different gap: tickets that have linked TEs but those TEs were not executed in time. A ticket can appear in F26's untested list (no TEs) or in F30's completed-but-untested list (has TEs, no results by close), but not both.

19. **Section hidden when all dev-complete tickets have test results.** No empty state message — the section simply does not render, same pattern as post-sprint testing.

### Dev-Done-to-Tested Gap

20. **Gap computation: first terminal test run FinishedAt minus cycle time end timestamp.** For each ticket in the sprint's active scope, the "dev done" timestamp is when the ticket first transitioned to the CycleTimeEndStage (from F12's StatusTransition data). The "tested" timestamp is the earliest FinishedAt among all terminal-status (PASS or FAIL) test runs linked to that ticket via TestExecutionLinks (linkType = Tests). The gap is the calendar day difference between these two timestamps.

21. **Tickets must have both timestamps to be eligible.** A ticket is excluded from the gap metric when: (a) it has not reached CycleTimeEndStage (still in development), (b) it has no linked test runs with terminal status, or (c) all linked terminal test runs have null FinishedAt. The gap is undefined for these tickets.

22. **Negative gaps are valid.** A ticket can be tested before it reaches the cycle time end boundary (testing started during development). Negative gap values are preserved and contribute to the median — they indicate proactive testing (shift-left behavior).

23. **Median is the aggregate metric.** The median gap across all eligible tickets in the sprint is the headline metric. Median is preferred over mean because it is resistant to outliers (one ticket waiting 30 days for testing would skew the average). This matches Pluralsight Flow's use of median for cycle time.

24. **Delta compares to the prior sprint's median gap.** Direction: lower is better (green arrow down, red arrow up). Delta is null when no prior sprint has eligible gap data.

25. **Per-ticket gap table shows the detailed breakdown.** Each row shows the ticket, both timestamps, and the gap in days (one decimal place). Sorted by gap descending — tickets that waited the longest for testing appear first.

### Sprint Scoping

26. **TE sprint derivation follows F25 rules.** A TE belongs to a sprint through its linked tickets' sprint membership. When a TE links to tickets in multiple sprints, it is attributed to the most recent sprint.

27. **Sub-team filter scopes all computations.** When a sub-team is selected, the burnup chart, scope change overlay, testing crunch, post-sprint testing, completed-but-untested, and dev-to-test gap all restrict to tickets assigned to developers in that sub-team.

### Feature Flag

28. **Xray disabled hides everything.** When Xray is disabled, the entire Test Execution Timeline section on the Sprints page is not rendered, the testing crunch flag is absent from Dashboard flags, and the test-timeline endpoint returns hasQaData=false.

29. **Sprint with no QA data shows sync prompt.** When Xray is enabled but the sprint has not been synced with QA data (zero test runs for the sprint's tickets), the section shows "Sync sprint to load QA data" matching F26's empty state pattern.

### Division by Zero

30. **Zero terminal runs in sprint: crunch percentage is null, flag does not fire.** The burnup chart is empty. Post-sprint percentage is null.

31. **Zero eligible tickets for gap: median gap is null.** The metric card shows "No data" and the ticket table shows "No tickets with both completion and test data."

## Acceptance Criteria

### Burnup Chart

- [ ] "Test Execution Timeline" section appears on Sprints page single-sprint detail view when Xray is enabled and sprint has QA data
- [ ] Burnup chart X-axis shows sprint days (day 1 through sprint end, extending to post-sprint days when applicable)
- [ ] Burnup chart Y-axis shows cumulative test run count
- [ ] Green line shows cumulative PASS runs, red line shows cumulative FAIL runs, gray line shows cumulative total
- [ ] Background shading distinguishes planning zone, execution zone, and testing crunch zone (last 2 days)
- [ ] Post-sprint test runs appear to the right of the sprint end boundary, visually separated
- [ ] Only terminal-status runs (PASS, FAIL) are plotted; TODO, EXECUTING, ABORTED excluded
- [ ] Cancelled TEs excluded from all chart data
- [ ] Runs with both FinishedAt and StartedAt null are excluded from the chart
- [ ] Chart has no ideal/expected line

### Scope Change Overlay

- [ ] Vertical markers appear on burnup chart days that had scope change events
- [ ] Each marker shows net SP change (e.g., "+3 SP", "-2 SP")
- [ ] Overlay data derived from sprint membership AddedAt/RemovedAt timestamps
- [ ] Sub-team filter scopes overlay to the filtered team's tickets

### Testing Crunch — Dashboard Flag

- [ ] Testing crunch flag appears in Dashboard flags section when Xray enabled, QA data exists, and >50% of terminal runs completed in the last 2 days
- [ ] Flag shows percentage and run count ("73% of test runs completed in the last 2 days (18 of 24 runs)")
- [ ] Flag absent when crunch threshold not met
- [ ] Flag absent when Xray disabled or no QA data
- [ ] Post-sprint runs excluded from crunch calculation

### Testing Crunch — Sprints Page Detail

- [ ] Expandable "Testing crunch" section appears below burnup chart when flag fires
- [ ] Section header shows percentage and counts
- [ ] Expanded view shows ticket list: ticket key, summary, assignee, SP, late run count
- [ ] Ticket list sorted by late run count descending
- [ ] Section hidden when crunch flag does not fire

### Post-Sprint Testing

- [ ] "Post-sprint testing" section appears when any terminal runs completed after sprint EndDate
- [ ] Section header shows percentage and counts
- [ ] Expanded view shows ticket list with post-sprint run counts
- [ ] Section hidden when no post-sprint runs exist
- [ ] Post-sprint runs are distinct from testing crunch runs (no overlap)

### Completed but Untested

- [ ] "Completed but untested by sprint close" section appears when any dev-complete tickets lack terminal test runs by sprint EndDate
- [ ] Section header shows count of affected tickets
- [ ] Expanded view shows ticket list: ticket key, summary, assignee, SP, dev-done date
- [ ] Ticket list sorted by dev-done date ascending (earliest completed first)
- [ ] Section hidden when all dev-complete tickets have test results by sprint close
- [ ] Distinct from F26 untested tickets (F26 = no linked TEs; F30 = linked TEs but no results by close)

### Dev-to-Test Gap

- [ ] "Dev-to-test gap" section shows median gap metric card with delta vs prior sprint
- [ ] Delta polarity: lower is better (green down, red up)
- [ ] Per-ticket table shows: ticket key, summary, assignee, dev-done date, first-test date, gap in days
- [ ] Table sorted by gap descending
- [ ] Tickets excluded when they lack cycle time end timestamp or terminal test run FinishedAt
- [ ] Negative gaps preserved (testing before dev completion)
- [ ] Section shows "No tickets with both completion and test data" when no eligible tickets exist
- [ ] Gap computation uses earliest terminal-status (PASS/FAIL) FinishedAt, not latest

### API

- [ ] GET /api/sprints/{sprintId}/test-timeline returns 200 with full response when data exists
- [ ] GET /api/sprints/{sprintId}/test-timeline returns 200 with hasQaData=false when Xray disabled
- [ ] GET /api/sprints/{sprintId}/test-timeline returns 404 for non-existent sprint
- [ ] GET /api/sprints/{sprintId}/test-timeline accepts ?subTeam=X and scopes all data
- [ ] GET /api/analytics/sprint-summary flags object gains testingCrunch field when applicable
- [ ] testingCrunch is null when Xray disabled, no QA data, or crunch flag does not fire

### Cross-Cutting

- [ ] Sub-team filter scopes all F30 computations
- [ ] Cancelled TEs excluded from all computations
- [ ] Delta is null when no prior sprint has applicable data

### Empty States

- [ ] Xray disabled: entire section hidden
- [ ] Xray enabled, not synced: "Sync sprint to load QA data" prompt
- [ ] Xray enabled, zero test runs: chart empty, crunch/post-sprint hidden, gap shows "No data"

## Out of Scope

- **Multi-sprint test execution trends** -- deferred to F27 (Cross-Sprint QA Trends). F30 is single-sprint with delta only.
- **Per-developer testing timing breakdown** -- who executed tests late vs. early. Deferred to F29 (QA Workload & Throughput) which owns per-person QA analytics.
- **Configurable testing crunch threshold** -- fixed at 2 days, mirroring the planning window pattern. Configurable if user feedback demands it.
- **Configurable crunch percentage threshold** -- fixed at 50%. Could become a setting alongside QA health thresholds if needed.
- **Testing crunch as a health sub-score** -- the crunch flag is diagnostic, not scored. No tool in the industry scores testing distribution as a health dimension. If the signal proves valuable, it could feed into the Quality sub-score in a future revision.
- **Test execution burndown (remaining tests)** -- the burnup pattern was chosen because Fokus discovers test runs after sync, not from a pre-planned test suite. A burndown requires a known total upfront.
- **Ideal/expected execution line** -- same reason as above. No predictable baseline exists.
- **Per-test-run detail drill-down** -- clicking individual chart points to see which specific test runs completed. The ticket-level lists provide sufficient detail.
- **Gantt-style per-TE timeline** -- one row per Test Execution showing start-to-finish bars. Adds visual complexity without proportional insight over the aggregate burnup.

## Open Questions

None -- all resolved during discussion.
