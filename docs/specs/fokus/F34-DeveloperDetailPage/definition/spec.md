# Developer Detail Page

**Traces to:** `docs/product/v1.md` §5.1 (Developer Throughput — per-developer analytics). Extends the Developers page with an individual drill-down that surfaces cross-sprint trends, work type allocation, and current sprint ticket-level detail.
**Source:** Scratch
**Dependencies:** F9 (Developer Throughput — velocity data, rolling averages), F19 (Leaderboard Breakdown — bug/feature SP split per sprint), F32 (Daily Developer Progress — current sprint daily breakdown, stall detection), F33 (Daily Progress Enhancements — bug/feature SP split on progress data), F15 (Team Management — developer profiles, capacity, sub-team)
**Status:** Done
**Plan:** `docs/specs/F34-DeveloperDetailPage/delivery/plan.md`

---

## Purpose

The Developers page shows team-level analytics across tabs (Throughput, Bug Ratio, Leaderboard, Quality, QA Workload, Daily Progress). Each tab aggregates data across all developers, with individual data appearing in tables or card grids. But a Scrum Master preparing for a 1:1 or investigating why a developer is struggling has no single place to see that developer's full picture: how their velocity trends across sprints, whether they're sinking into bug work, and what exactly they're working on right now.

The Developer Detail Page provides this individual lens — accessible by clicking a developer on the Daily Progress tab. It composes data from existing analytics (Throughput, Leaderboard, Daily Progress) into a focused view for coaching conversations and mid-sprint course correction.

## Entities

No new domain entities. This feature introduces one new setting:

**Bug ratio target threshold** (decimal, percentage, 0-100):
- Stored as a new field on the application settings entity
- Default value: 30%
- Displayed as a horizontal reference line on the work type allocation chart
- Configurable in the Settings page under a new "Analytics Targets" section

## User Flows

```
Flow 1: Navigate to Developer Detail
1. User is on the Developers page, Daily Progress tab
2. User clicks a developer's name or avatar on a progress card
3. Browser navigates to the Developer Detail page for that developer
4. URL updates to /developers/{accountId}
5. Back button or breadcrumb returns to the Daily Progress tab
```

```
Flow 2: View Cross-Sprint Velocity Trends
1. The page header shows the developer's avatar, display name, sub-team, and role
2. The "Sprint Trends" section shows the last 10 sprints by default
3. A stacked bar chart displays feature SP (blue) and bug SP (red) per sprint
4. A completion % trend line overlays the chart (secondary Y-axis, 0-100%)
5. A 3-sprint rolling average line shows the developer's velocity trend
6. Hovering a sprint bar shows: sprint name, feature SP, bug SP, total SP, completion %, capacity %
7. Sprints where the developer had 0% capacity (was not part of the team) are excluded
```

```
Flow 3: View Work Type Allocation
1. The "Work Allocation" section shows a line chart of bug % per sprint
2. Bug % = bugSp / totalSp x 100 for each sprint
3. A horizontal dashed reference line marks the configured bug ratio target (default 30%)
4. Sprints where bug % exceeds the target are highlighted
5. Hovering a point shows: sprint name, bug SP, feature SP, bug %, target %
6. Below the chart, a summary line shows: average bug % across the displayed sprints, and how many sprints exceeded the target
```

```
Flow 4: View Current Sprint Detail
1. If the developer has an active sprint, the "Current Sprint" section is visible
2. A larger burnup chart shows the same data as the Daily Progress card but at full width — actual vs expected pace lines are clearly distinguishable
3. Below the chart, the SP summary shows: completed / assigned SP, completion %, feature/bug split
4. A ticket table lists all assigned tickets in the current sprint, grouped by state:
   a. Stalled — tickets with no status transition in 2+ business days (warning indicator)
   b. In Progress — started but not completed, not stalled
   c. Done — completed tickets
   d. Not Started — not yet transitioned to the start stage
5. Each ticket row shows: ticket key (as a link to Jira), summary, status, story points, issue type, days in current status
6. Stalled tickets appear first with a visual warning, showing days since last transition
7. The ticket table is sorted: Stalled first (most days stalled first), then In Progress, then Not Started, then Done
```

```
Flow 5: No Active Sprint
1. If no active sprint exists, the "Current Sprint" section shows: "No active sprint"
2. The Sprint Trends and Work Allocation sections remain visible with historical data
```

```
Flow 6: Sprint Range Selection
1. The Sprint Trends and Work Allocation sections default to the last 10 sprints
2. A sprint range selector offers: "Last 5", "Last 10", "All"
3. Changing the range updates both charts simultaneously
```

```
Flow 7: Configure Bug Ratio Target
1. User navigates to Settings
2. Under a new "Analytics Targets" section, the user sees "Bug Ratio Target (%)" with the current value
3. User changes the value and saves
4. The reference line on the Developer Detail Page's Work Allocation chart updates to reflect the new target
5. The target applies globally — same threshold for all developers
```

## API Surface

| Method | Route | Auth | Request body | Response | Status codes |
|--------|-------|------|-------------|----------|-------------|
| GET | /api/analytics/developer-detail/{accountId} | Authenticated | — | DeveloperDetailResponse | 200, 404 |

**Query parameters:**
- `last` (integer, optional) — number of recent sprints to include. Default: 10. Values: 5, 10, or 0 for all sprints (matching the existing sprint range convention used by Throughput and Leaderboard endpoints).

**Response shape:**

Top level:
- `developer` — developer identity (display name, sub-team, avatar URL, account ID, role, default capacity %)
- `sprintTrends` — list of per-sprint breakdowns, ordered chronologically (oldest first)
- `workAllocation` — summary statistics for work type distribution
- `currentSprint` — current sprint detail (null if no active sprint)
- `bugRatioTarget` — the configured bug ratio target percentage

Per sprint trend entry:
- `sprintId`, `sprintName`, `startDate`, `endDate`
- `featureSp` — SP completed on non-bug tickets (all types except Bug)
- `bugSp` — SP completed on bug tickets (issue type == "Bug", case-sensitive)
- `totalSp` — featureSp + bugSp
- `assignedSp` — total SP assigned in the sprint (all ticket types, all non-removed memberships — same all-types scope as Daily Progress, not the feature-only scope of Throughput)
- `completionPercent` — totalSp / assignedSp x 100. This is an all-types completion % that includes both features and bugs in the numerator and denominator. It intentionally diverges from the Throughput tab's feature-only completion %. The chart labels this as "(all types)" to signal the difference.
- `capacityPercent` — developer's effective capacity for this sprint
- `rollingAverageSp` — 3-sprint rolling average of totalSp (null for the first 2 sprints)
- `bugPercent` — bugSp / totalSp x 100 (0 when totalSp = 0)

Work allocation summary:
- `averageBugPercent` — average of bugPercent across all displayed sprints (excluding sprints with 0 total SP)
- `sprintsAboveTarget` — count of sprints where bugPercent exceeds the configured target
- `totalSprints` — count of displayed sprints (for context: "3 of 10 sprints above target")

Current sprint detail (if active sprint exists):
- `sprint` — sprint summary (id, name, start date, end date)
- `currentDay`, `totalDays` — same as F32
- `assignedSp`, `completedSp`, `completionPercent` — same as F32
- `featureCompletedSp`, `bugCompletedSp` — same as F33
- `dailyPace`, `isBehindPace`, `paceGapSp` — same as F32
- `dailyBreakdown` — same array of day entries as F32 (cumulative SP, expected SP, completed tickets per day)
- `tickets` — list of all assigned tickets in the current sprint

Per ticket entry:
- `key` — Jira ticket key (e.g., "FOK-123")
- `summary` — ticket title
- `currentStatus` — current status name
- `storyPoints` — effective SP (may be null)
- `issueType` — issue type name
- `daysInCurrentStatus` — business days since last status transition (Mon-Fri)
- `state` — computed grouping: `stalled` | `in-progress` | `done` | `not-started`
- `isStalled` — boolean (same definition as F32: no transition in 2+ business days, started but not completed)

**Error conditions:**
- 404 when accountId does not match any known developer
- 200 with `currentSprint: null` when no active sprint exists

**Settings endpoints:**

The existing GET settings endpoint returns the new field alongside all other settings:
- `bugRatioTarget` (decimal, 0-100, default 30) — the team-wide bug ratio target percentage

A new save endpoint follows the per-section pattern used by all other settings sections:

| Method | Route | Auth | Request body | Response | Status codes |
|--------|-------|------|-------------|----------|-------------|
| PUT | /api/settings/analytics-targets | Authenticated (Admin) | `{ bugRatioTarget: decimal }` | 204 | 204, 400 |

Validation: `bugRatioTarget` required, must be between 0 and 100. Returns 400 if invalid.

## Business Rules

### Cross-Sprint Trends

1. **Data source composition and scope model.** Sprint trend data follows the **Leaderboard computation model** for bug/feature SP split and ticket scope. Specifically:
   - Bug classification: case-sensitive exact match (issue type == "Bug"), same as Leaderboard, Bug Ratio, and Throughput.
   - ExcludedFromScopeStatuses: applied — tickets whose final status is in the excluded list are filtered out, matching the Leaderboard convention.
   - Completion: transition-based, using the cycle time end stage boundary.
   - Assigned SP: all non-removed memberships, all ticket types (not the feature-only, start-gated scope of Throughput). This page shows total developer output including bugs.
   - Rolling average: 3-sprint rolling average of totalSp, computed the same way as Throughput but over all-types SP instead of feature-only.
   - Capacity: developer's effective capacity for each sprint (sprint override or developer default), same source as Throughput.

   The Developer Detail page intentionally uses an all-types scope. It diverges from the Throughput tab (which is feature-only) because the purpose is coaching — a Scrum Master needs to see total output including bugs to understand where time went. The stacked bar chart makes the feature/bug split visible, so the all-types total is self-documenting.

2. **Sprint inclusion.** Only sprints where the developer had at least one non-removed ticket (any ticket type) are included. Sprints where the developer had 0% capacity and 0 completed SP are excluded (same exclusion rule as other analytics tabs).

3. **Sprint ordering.** Sprints are returned in chronological order (oldest first) to match chart x-axis convention.

4. **Rolling average.** 3-sprint rolling average of totalSp (feature + bug). Null for the first 2 sprints in the series. Same computation as the Throughput feature's rolling average.

5. **Default sprint range.** Last 10 sprints. The range counts only sprints that pass the inclusion rule — if the developer was absent from 3 of the last 13 sprints, those 3 are excluded and the next 3 earlier sprints fill in.

### Work Type Allocation

6. **Bug percentage computation.** Per sprint: `bugPercent = bugSp / totalSp x 100`. When totalSp = 0, bugPercent = 0 (not undefined — a sprint with no completed work is 0% bugs).

7. **Average bug percentage.** Computed across all displayed sprints, excluding sprints with 0 total SP (to avoid diluting the average with zero-work sprints).

8. **Bug ratio target.** A single global threshold stored in application settings. Default: 30%. Displayed as a reference line on the work allocation chart. The target is informational — no alerts or enforcement. Configurable in Settings under "Analytics Targets."

9. **Target applies equally.** The same target applies to all developers. Per-developer targets are out of scope.

### Current Sprint Detail

10. **Ticket state derivation.** Each ticket is assigned a display state based on its status transitions and the cycle time boundaries:
    - `done` — has transitioned to the cycle time end stage or beyond
    - `stalled` — has started (transitioned to cycle time start stage or beyond), not completed, and no status transition in 2+ business days
    - `in-progress` — has started, not completed, not stalled
    - `not-started` — has not transitioned to the cycle time start stage

11. **Days in current status.** Business days (Mon-Fri) since the most recent status transition. For tickets that have never transitioned, counts from sprint start date.

12. **Ticket ordering.** Stalled first (most days stalled descending), then In Progress, then Not Started, then Done. Within each group, ordered by story points descending (largest first).

13. **Jira link.** Ticket keys link to the ticket in Jira using the Jira instance URL from the Jira connection configuration (the same URL used for sync API calls), not from the AppSettings database entity.

14. **Current sprint data consistency.** The burnup chart and SP summary use the same data source as the Daily Progress card (F32/F33). The Developer Detail page shows a larger version of the same chart, not a different computation. SP values in the current sprint section carry the "(all types)" label, consistent with F32's convention.

15a. **F33 prerequisite.** F33 must be implemented before F34. The current sprint section depends on F33's `featureCompletedSp` and `bugCompletedSp` fields. The Developer Detail endpoint does not independently recompute the bug/feature split for the active sprint — it consumes the same computation as the Daily Progress endpoint.

### Navigation

16. **Entry point.** The developer's name and avatar on each Daily Progress card are clickable links to the Developer Detail page.

17. **URL structure.** `/developers/{accountId}` — uses the Jira account ID as the URL parameter.

18. **Breadcrumb.** The page shows a breadcrumb: "Developers > {Developer Name}" linking back to the Developers page with the Daily Progress tab active.

19. **Deep link support.** The URL is directly navigable — bookmarking and sharing a developer's detail page works without requiring navigation through the Daily Progress tab first.

20. **Developer not found.** If the accountId does not match any developer, show a "Developer not found" message with a link back to the Developers page. Excluded developers (0% capacity + 0 completed tickets) also return a "Developer not found" response — they are hidden from all analytics views.

### Settings

21. **Bug ratio target field.** Added to the Settings page under a new "Analytics Targets" section. Input: decimal percentage (0-100). Validation: required, must be between 0 and 100. Default: 30.

22. **Settings backward compatibility.** The new field has a default value. Existing installations that have never configured it will see 30% as the target.

## Acceptance Criteria

### Navigation

- [ ] Clicking a developer name or avatar on a Daily Progress card navigates to /developers/{accountId}
- [ ] Breadcrumb shows "Developers > {Developer Name}" with link back to Developers page (Daily Progress tab)
- [ ] Direct URL navigation works (deep link / bookmark)
- [ ] Unknown accountId shows "Developer not found" message
- [ ] Back button returns to Daily Progress tab

### Developer Header

- [ ] Page header shows: avatar, display name, sub-team, role, default capacity %

### Sprint Trends

- [ ] Stacked bar chart shows feature SP (blue) and bug SP (red) per sprint
- [ ] Completion % trend line on secondary Y-axis (0-100%)
- [ ] 3-sprint rolling average line visible
- [ ] Hovering a bar shows: sprint name, feature SP, bug SP, total SP, completion %, capacity %
- [ ] Default shows last 10 sprints
- [ ] Sprint range selector offers: Last 5, Last 10, All
- [ ] Sprints where developer had 0% capacity and 0 SP are excluded

### Work Allocation

- [ ] Line chart shows bug % per sprint
- [ ] Horizontal dashed reference line at the configured bug ratio target
- [ ] Sprints exceeding the target are visually highlighted
- [ ] Hover shows: sprint name, bug SP, feature SP, bug %, target %
- [ ] Summary line below chart shows: average bug %, sprints above target count
- [ ] Chart respects the sprint range selector (same range as Sprint Trends)

### Current Sprint

- [ ] Section visible only when an active sprint exists
- [ ] Full-width burnup chart shows actual vs expected pace lines clearly
- [ ] SP summary shows: completed / assigned SP, completion %, feature/bug split
- [ ] Ticket table lists all assigned tickets grouped by state: Stalled, In Progress, Not Started, Done
- [ ] Each ticket row shows: key (linked to Jira), summary, status, SP, issue type, days in current status
- [ ] Stalled tickets have a visual warning indicator
- [ ] Ticket ordering: stalled first (most days stalled), then in-progress, then not-started, then done
- [ ] No active sprint shows "No active sprint" message; historical sections remain visible

### Settings

- [ ] "Analytics Targets" section appears in Settings page
- [ ] "Bug Ratio Target (%)" field with default 30, validation 0-100
- [ ] Saving a new target updates the reference line on all Developer Detail pages
- [ ] PUT /api/settings/analytics-targets saves the bug ratio target (per-section save pattern)
- [ ] PUT /api/settings/analytics-targets returns 400 for values outside 0-100

### API

- [ ] GET /api/analytics/developer-detail/{accountId} returns 200 with cross-sprint and current sprint data
- [ ] 404 when accountId is unknown
- [ ] `?last=5`, `?last=10`, and `?last=0` (all sprints) filter sprint range correctly
- [ ] Response includes bugRatioTarget from settings
- [ ] Sprint trends featureSp/bugSp match Leaderboard values for the same developer and sprint (same scope model)
- [ ] Sprint trends completionPercent is all-types (diverges from Throughput's feature-only % by design) and labeled "(all types)"
- [ ] Current sprint data matches Daily Progress endpoint data for the same developer

## Out of Scope

- **Per-developer bug ratio targets** — The target is global (same for all developers). Per-developer customization adds settings complexity without clear value yet.
- **Git-level metrics** — PR cycle time, coding days, rework % (a la Pluralsight Flow or LinearB). Fokus is Jira-focused; git integration is deferred in v1 §8.
- **AI adoption metrics** — Jellyfish-style AI tool usage cohorts. Not relevant to this product's scope.
- **FTE-fraction allocation model** — Jellyfish/Swarmia compute fractional FTE allocations across categories. Our model uses SP, which is simpler and consistent with all other Fokus analytics.
- **Burnout indicators** — "Worked 90% of sprint days" or "too many parallel items" alerts (LinearB). These require git activity data or more granular ticket lifecycle analysis.
- **Developer comparison mode** — Side-by-side comparison of two developers (LinearB radar chart). Individual coaching is the primary use case; comparison invites ranking.
- **Cycle time per developer** — Per-developer stage-level cycle time breakdown. Could be added as a future section but adds significant scope.
- **Print / export** — PDF export for 1:1 meeting prep. Useful but not essential for v1 of this page.

## Open Questions

None — all resolved during discussion.
