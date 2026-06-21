# QA Workload & Throughput

**Traces to:** `docs/product/v2.md` §5.5
**Source:** Scratch
**Dependencies:** F25 (Xray Integration & Ticket Test Enrichment), F26 (Sprint Test Coverage — reuses pass rate RAG thresholds)
**Status:** Done

---

## Purpose

Surfaces per-person QA workload distribution as a tab on the Developers page. Answers "How is testing work distributed across the team?" by breaking down test execution ownership, run throughput, and pass/fail split per team member. Adds a workload balance flag to surface concentration anti-patterns — when one person handles the majority of test executions across consecutive sprints. The Dashboard already shows team-level QA health (F26's Coverage/Execution/Pass Rate cards); this tab is the per-person drill-down.

## Entities

No new entities. F29 reads from entities introduced by F25:
- **TestExecution** — AssigneeId identifies TE owner
- **TestRun** — ExecutedById identifies who ran each test, Status gives pass/fail
- **TestExecutionLink** — Tests linkType connects TEs to sprint tickets, Blocks linkType connects TEs to discovered bugs
- **Developer** — DisplayName, SubTeam, AvatarUrl for the per-person table

## Placement

New tab on the Developers page. When both F28 (Quality) and F29 (QA Workload) are implemented, the full tab order is:

```
Throughput | Bug Ratio | Leaderboard | Quality | QA Workload
```

If F29 is implemented before F28, QA Workload is the 4th tab; when F28 lands later, it inserts before QA Workload as the 4th and QA Workload becomes the 5th.

- Same shared sprint selector, sub-team filter, multi/single sprint mode
- API default: `last=5` when no sprint params provided (same server-side fallback as Bug Ratio and Leaderboard). The UI follows the page-level sprint selector state — it does not override the selector to multi-sprint mode on tab switch.
- URL slug: `?tab=qa-workload`
- Feature-flag gated: tab hidden when Xray is disabled

## User Flows

```
Flow 1: View QA Workload (multi-sprint)
1. User navigates to Developers page
2. User clicks "QA Workload" tab
3. System loads last 5 sprints by default
4. Top: 3 team-level metric cards — Total TEs, Total Runs Completed, Team Pass Rate
5. Below cards: workload distribution chart — stacked horizontal bar per person, PASS (green) / FAIL (red), sorted by total runs descending
6. Below distribution chart: throughput trend chart — line chart showing each person's run count per sprint over the viewed range
7. Below trend chart: per-person table with columns: Person, Sub-Team, TEs Owned, Runs Completed, Pass, Fail, Pass Rate, Stories Covered, Bugs Found
8. Persons with workload balance flag show warning badge next to name
9. User clicks column header to sort ascending/descending
```

```
Flow 2: View QA Workload (single-sprint)
1. User selects a single sprint from the sprint selector
2. Team metric cards show delta vs prior sprint (arrow + signed number) and 4-sprint sparkline
3. Per-person table shows delta indicators on all numeric columns
4. Chart shows single-sprint data only
```

```
Flow 3: Sub-team filter
1. User selects a sub-team from the toolbar filter
2. All metrics scope to QA persons whose Developer.SubTeam matches
3. Note: this filters by the QA executor's sub-team, not the ticket assignee's sub-team (different from F26's sub-team scoping)
```

```
Flow 4: Empty states
4a. Xray disabled → QA Workload tab hidden entirely from tab bar
4b. Xray enabled, no QA data for viewed sprints → tab shows "Sync sprint to load QA data" prompt
4c. Xray enabled, QA data exists but zero persons with attributed runs → empty table with "No test execution data for the selected sprint(s)" message
```

## API Surface

| Method | Route | Auth | Request params | Response | Status codes |
|--------|-------|------|---------------|----------|-------------|
| GET | /api/analytics/qa-workload | Authenticated | sprintId, last, subTeam (query) | QaWorkloadResponse | 200, 404 |

Query parameter rules follow the existing analytics pattern:
- `sprintId` — single sprint (single mode)
- `last` — N most recent closed sprints (multi mode)
- Neither — all closed sprints (multi mode)
- `sprintId` and `last` are mutually exclusive
- Default when no params: `last=5`
- `subTeam` — optional, filters by QA executor's sub-team

**QaWorkloadResponse:**

```
hasQaData: boolean (false when Xray disabled or no QA data — UI uses this to choose empty state, same as F26)
mode: 'multi' | 'single'

multiSprint:
  sprints: SprintSummaryItem[]
  teamMetrics:
    totalTes: integer
    totalRunsCompleted: integer
    passCount: integer
    failCount: integer
    teamPassRate: decimal
    (passCount and failCount are included for chart tooltip use — not displayed as standalone cards)
  developers: QaWorkloadEntry[]
    accountId: string
    displayName: string
    subTeam: string (nullable)
    avatarUrl: string (nullable)
    tesOwned: integer
    runsCompleted: integer
    passCount: integer
    failCount: integer
    passRate: decimal
    storiesCovered: integer
    bugsFound: integer
    sprintBreakdowns: QaWorkloadSprintBreakdown[]
      sprintId: integer
      sprintName: string
      tesOwned: integer
      runsCompleted: integer
      passCount: integer
      failCount: integer
      passRate: decimal
      storiesCovered: integer
      bugsFound: integer
    workloadAlert: WorkloadAlert
      isActive: boolean
      consecutiveSprintCount: integer
      thresholdPercent: integer (always 50)

singleSprint:
  sprint: SprintSummaryItem
  teamMetrics:
    totalTes: MetricCard (value, delta, direction, sparklinePoints[])
    totalRunsCompleted: MetricCard
    teamPassRate: MetricCard
  developers: QaWorkloadSingleEntry[]
    (same fields as QaWorkloadEntry, plus:)
    tesOwnedDelta, tesOwnedDirection: delta pair
    runsCompletedDelta, runsCompletedDirection: delta pair
    passCountDelta, passCountDirection: delta pair
    failCountDelta, failCountDirection: delta pair
    passRateDelta, passRateDirection: delta pair
    storiesCoveredDelta, storiesCoveredDirection: delta pair
    bugsFoundDelta, bugsFoundDirection: delta pair
    workloadAlert: WorkloadAlert
```

MetricCard follows the existing pattern: value (decimal), delta (decimal, nullable), direction (up|down|flat|null), sparklinePoints (list of {sprintName, value}, up to 4 trailing sprints).

Error conditions:
- 404 when sprintId does not exist
- 200 with empty developers array when Xray is disabled or no QA data

## Business Rules

### Attribution

1. **Primary attribution:** TestRun.ExecutedById — who actually executed the run.

2. **Fallback:** When ExecutedById is null, fall back to the parent TestExecution.AssigneeId.

3. **Double null:** When both ExecutedById and AssigneeId are null, attribute to an "Unassigned" synthetic row (accountId = null, displayName = "Unassigned").

4. **Inclusion rule:** A person appears in the QA workload table if they have at least one attributed terminal run (PASS or FAIL) across the viewed sprint range.

### Metric Formulas

5. **TEs Owned** = count of non-Cancelled TestExecutions where AssigneeId = this person, linked to tickets in the sprint(s) via TestExecutionLink (Tests type). This counts ownership, not execution — a person owns TEs regardless of who ran the tests inside them. A person can appear in the table with zero TEs Owned if they only executed runs on TEs owned by others (they meet the inclusion rule via BR 4 but own no TEs). This is expected behavior, not a bug. For the "Unassigned" synthetic row, TEs Owned counts TEs where AssigneeId is null.

6. **Runs Completed** = count of TestRuns with terminal status (PASS or FAIL) attributed to this person via the attribution rules (BR 1-3).

7. **Pass Count** = count of TestRuns with PASS status attributed to this person.

8. **Fail Count** = count of TestRuns with FAIL status attributed to this person.

9. **Pass Rate** = passCount / (passCount + failCount) x 100. When denominator is 0, Pass Rate = 0%.

10. **Stories Covered** = count of unique TicketKeys linked via Tests from TEs where this person has at least one attributed terminal run. Counts how many stories this person helped test.

11. **Bugs Found** = count of unique bug TicketKeys linked via Blocks from TEs where AssigneeId = this person. Bug discovery credit goes to the TE owner (AssigneeId), not the run executor — the TE owner is responsible for the execution that surfaced the bug. For the "Unassigned" row, this counts bugs from TEs where AssigneeId is null. Note: Stories Covered (BR 10) uses the execution attribution model (who ran the test) while Bugs Found uses ownership (who owns the TE) — these are deliberately different lenses.

### Aggregation Rules

12. **Cancelled TEs** (IsCancelled = true) excluded from all computations — TEs Owned, run counts, stories covered, bugs found.

13. **Non-terminal runs** (TODO, EXECUTING, ABORTED) excluded from Runs Completed, Pass Count, Fail Count, and Pass Rate. They do not count toward workload throughput.

14. **Sprint scoping:** Same as F26 — a TE belongs to a sprint through its linked tickets' sprint membership. When a TE links to tickets in multiple sprints, it is attributed to the most recent sprint.

15. **Multi-sprint aggregation:** In multi-sprint mode, top-level per-person metrics are sums (TEs Owned, Runs Completed, Pass, Fail, Stories Covered, Bugs Found) or weighted averages (Pass Rate = total pass / total terminal runs). sprintBreakdowns provide per-sprint detail.

### Workload Balance Flag

16. **Per-sprint concentration:** A person "dominates" a sprint when their attributed terminal runs / total attributed terminal runs in that sprint > 50%.

17. **Consecutive-sprint rule:** Alert fires when a person dominates for 2+ consecutive sprints in the full closed sprint history (not just the viewed range).

18. **Alert payload:** `{ isActive: boolean, consecutiveSprintCount: integer, thresholdPercent: 50 }`. consecutiveSprintCount is the current streak length (0 when not active).

19. **Hardcoded threshold:** 50% is not configurable. Consistent with Bug Ratio alert pattern (hardcoded 50%, 2+ consecutive sprints). With small teams (2-3 QA executors), the 50% threshold will produce a permanent alert for the majority runner — this is by design, surfacing the concentration pattern. As the team grows, the threshold becomes more discriminating.

### Developer Exclusion

20. The QA Workload tab does NOT apply the delivery exclusion filter (0% capacity + 0 completed tickets = hidden). A developer with 0% delivery capacity who executes test runs should appear in the QA workload table. Inclusion is governed solely by BR 4 (at least one attributed terminal run). Inactive developers (IsActive = false) who have QA execution data in the viewed range still appear.

### Sub-team Filter

21. When `subTeam` is specified, scope to persons (QA executors) whose `Developer.SubTeam` matches. All metrics recalculate within that sub-team's population. This filters by the QA person's team, not the ticket assignee's team — workload is about who does the testing, not whose stories are being tested.

### Team-Level Metrics

21. **Total TEs** = count of unique non-Cancelled TEs in the sprint(s), deduplicated by TE ID.

22. **Total Runs Completed** = sum of all terminal runs across all persons.

23. **Team Pass Rate** = total PASS runs / total terminal runs x 100.

24. In single-sprint mode, team metrics use MetricCard with delta vs prior sprint, direction, and 4-sprint sparkline. In multi-sprint mode, team metrics are plain aggregates (no deltas).

### Division by Zero

25. Zero persons with attributed runs → empty developers array, team metrics show 0 for counts and 0% for pass rate.

26. Zero terminal runs for a person → Pass Rate = 0%.

## UI & Display

### Team Metric Cards

27. Three cards in a row: **Total TEs** | **Total Runs Completed** | **Team Pass Rate**

28. **Total TEs** — neutral polarity (no color on delta). Count of test executions in the sprint(s).

29. **Total Runs Completed** — neutral polarity. Volume of individual test runs completed.

30. **Team Pass Rate** — higher is better (green arrow up, red arrow down). RAG-colored using F26's pass rate thresholds (reused, not new thresholds).

31. Single-sprint mode: delta vs prior sprint, 4-sprint sparkline (same MetricCard component as F26).

32. Multi-sprint mode: aggregate values, no deltas or sparklines.

### Workload Distribution Chart

33. Stacked horizontal bar chart (ApexCharts `type: 'bar'`, `plotOptions.bar.horizontal: true`).

34. One bar per person, sorted by total terminal runs descending.

35. Two stacked segments: PASS (green / `text-status-success`) and FAIL (red / `text-status-danger`).

36. X-axis: run count. Y-axis: person name.

37. Shows in both multi-sprint (aggregated across range) and single-sprint modes.

38. Wrapped in a BaseCard with title "Workload Distribution".

39. "Unassigned" bar (if any) pinned at bottom.

### Throughput Trend Chart (multi-sprint only)

40. Line chart (ApexCharts `type: 'line'`, `stroke.curve: 'smooth'`, height 300). Same pattern as Throughput tab's SP Completed Trend chart.

41. X-axis: sprint names. Y-axis: "Runs Completed". Each person = one series using their `sprintBreakdowns.runsCompleted`.

42. Tooltip `theme: 'dark'`. Wrapped in a BaseCard with title "Execution Throughput Trend".

43. Hidden in single-sprint mode (only one data point — no trend to show).

44. "Unassigned" series included if present (gray color, dashed line).

### Per-Person Table

45. **Multi-sprint columns:** Person | Sub-Team | TEs Owned | Runs Completed | Pass | Fail | Pass Rate | Stories Covered | Bugs Found

46. **Single-sprint columns:** Same columns with inline delta indicators (arrow + signed number) on all numeric columns.

47. **Person column:** Avatar + display name (same rendering pattern as Throughput table). Workload alert badge (warning icon) appears next to name when `workloadAlert.isActive`. Badge tooltip: "Handles >50% of test executions for N consecutive sprints."

48. **Pass Rate column:** RAG-colored using F26's pass rate thresholds (green >= 90%, amber >= 70%, red < 70% at defaults).

49. **All numeric columns sortable** (click header to toggle asc/desc, same pattern as Throughput table).

50. **"Unassigned" row** (if any) pinned at bottom of table, no avatar, italic display name.

### Delta Polarities (single-sprint mode)

51. TEs Owned: neutral (no color)
52. Runs Completed: neutral
53. Pass: higher is better
54. Fail: lower is better
55. Pass Rate: higher is better
56. Stories Covered: higher is better
57. Bugs Found: neutral (finding bugs is good; having bugs to find is bad)

### Empty States

58. Xray disabled → QA Workload tab hidden entirely from the tab bar.

59. Xray enabled, no QA data for viewed sprints → tab content shows "Sync sprint to load QA data" prompt (same styling as F26 Dashboard empty state).

60. No persons with attributed runs → empty table with "No test execution data for the selected sprint(s)" message. Team metric cards show 0. Charts hidden.

## Acceptance Criteria

### Tab & Navigation

- [ ] "QA Workload" tab appears on Developers page when Xray is enabled (5th tab when F28 Quality exists, 4th otherwise)
- [ ] Tab hidden when Xray is disabled
- [ ] URL updates to `?tab=qa-workload` when selected
- [ ] Sprint selector and sub-team filter affect QA Workload data
- [ ] Default view is `last=5` sprints (multi-sprint mode)

### Team Metric Cards

- [ ] Three cards displayed: Total TEs, Total Runs Completed, Team Pass Rate
- [ ] Single-sprint mode: each card shows delta vs prior sprint and 4-sprint sparkline
- [ ] Multi-sprint mode: cards show aggregated values, no deltas
- [ ] Team Pass Rate card RAG-colored using F26's pass rate thresholds

### Workload Distribution Chart

- [ ] Stacked horizontal bar chart shows one bar per person
- [ ] Bars stacked by PASS (green) and FAIL (red) segments
- [ ] Sorted by total runs descending
- [ ] Chart visible in both multi-sprint and single-sprint modes
- [ ] "Unassigned" bar pinned at bottom when present

### Throughput Trend Chart

- [ ] Line chart shows each person's runs completed per sprint over the viewed range
- [ ] One series per person, smooth curve
- [ ] Hidden in single-sprint mode
- [ ] Wrapped in BaseCard with title "Execution Throughput Trend"

### Per-Person Table

- [ ] Table shows all persons with at least one attributed terminal run
- [ ] Columns: Person, Sub-Team, TEs Owned, Runs Completed, Pass, Fail, Pass Rate, Stories Covered, Bugs Found
- [ ] Single-sprint mode adds delta indicators on all numeric columns
- [ ] All numeric columns sortable
- [ ] "Unassigned" row pinned at bottom when present

### Attribution

- [ ] Runs attributed to TestRun.ExecutedById when available
- [ ] Runs fall back to TestExecution.AssigneeId when ExecutedById is null
- [ ] Runs with both null go to "Unassigned" row

### Workload Balance Flag

- [ ] Warning badge appears on persons handling >50% of executions for 2+ consecutive sprints
- [ ] Badge tooltip shows threshold and consecutive sprint count
- [ ] Alert computed from full sprint history, not just viewed range

### Cross-Cutting

- [ ] Delivery exclusion filter (0% capacity + 0 tickets) does NOT apply — QA executors appear based on attributed runs only
- [ ] Cancelled TEs excluded from all computations
- [ ] Sub-team filter scopes by QA executor's sub-team (not ticket assignee's)
- [ ] Delta is null when no prior sprint has QA data
- [ ] TODO, EXECUTING, ABORTED runs excluded from all throughput metrics

### API

- [ ] `GET /api/analytics/qa-workload` returns correct data for multi-sprint and single-sprint modes
- [ ] Accepts `sprintId`, `last`, `subTeam` query params
- [ ] Default is `last=5` when no sprint params provided
- [ ] Returns 404 for non-existent sprint
- [ ] Returns 200 with hasQaData=false and empty developers array when Xray disabled or no QA data

## Out of Scope

- **Per-developer story quality** — F28 scope (test coverage of a developer's stories, not QA throughput)
- **Test execution timeline** — F30 scope (when tests ran relative to sprint lifecycle)
- **Cross-sprint QA trends page** — F27 scope (trend lines as standalone view)
- **Avg execution time per person** — deferred until StartedAt/FinishedAt data quality verified on live Xray data. Can be added as a table column later.
- **Configurable workload balance threshold** — hardcoded at 50% for consistency with Bug Ratio
- **Per-test-run detail view** — individual test steps, failure screenshots, execution logs
- **Test Set-based workload grouping** — TestSets are context only, not analytics entities
- **QA health score contribution** — workload distribution is a separate lens, not part of the composite health score

## Open Questions

None — all resolved during discussion.
