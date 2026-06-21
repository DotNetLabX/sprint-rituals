# Cycle Time

**Traces to:** `docs/product/v1.md` §5.4 (Cycle Time)
**Source:** Scratch
**Dependencies:** F2 (Domain Model & Persistence), F3 (Settings System), F5 (Sprint Sync), F6 (Workflow Auto-Detection), F7 (App Shell & Navigation), F8 (Sprint Summary Card — establishes cross-cutting patterns)
**Status:** Done
**Plan:** `docs/plans/CycleTime/plan.md`

---

## Purpose

Scrum Masters need to answer "how long does work actually take?" with evidence — not gut feel. Jira shows individual ticket timelines but cannot aggregate cycle time across sprints, break it down by workflow stage, or identify bottlenecks. This feature measures how long completed tickets spend in each stage of the workflow pipeline, surfaces the team's bottleneck stage, flags outliers for retro discussion, and gives stakeholders a reliable "85% of our work completes within X days" commitment number. It owns a dedicated "Cycle Time" page in the sidebar navigation.

## Entities

This feature introduces no new domain entities. It reads existing status transition data and computes all metrics on the fly.

**Settings extensions (two new fields on app settings):**
- Cycle time start stage — which workflow stage begins the cycle time measurement. Defaults to the second configured workflow stage (first non-backlog stage). Must reference a stage in the configured workflow stages list.
- Cycle time end stage — which workflow stage ends the cycle time measurement. Defaults to the first configured done status. Must reference a stage in the configured workflow stages list or a done status.

**Existing data queried:**
- Status transitions (from-status, to-status, timestamp, ticket association)
- Tickets (issue type, assignee, summary, key, story points)
- Sprint memberships (committed/added, final status, story points, removed-at)
- Sprints (start date, end date, state)
- Developers (display name, sub-team, avatar)
- App settings (workflow stages, done statuses, cycle time boundaries)

## User Flows

```
Flow 1: View Single-Sprint Cycle Time (Drill-Down)
1. User navigates to "Cycle Time" in the sidebar
2. The sprint selector defaults to the most recent closed sprint
3. Four metric cards appear at the top:
   - Median Cycle Time (days) with delta vs prior sprint
   - P85 Cycle Time (days) with delta vs prior sprint
   - Throughput (count of tickets that completed the full cycle) with delta
   - Outliers (count of tickets exceeding 2x the sprint median) with delta
4. A scatter plot shows one dot per completed ticket:
   - X-axis: completion date
   - Y-axis: cycle time in days
   - Dots are colored by issue type (bug, story, task, etc.)
   - Horizontal reference lines at P50 (dashed) and P85 (solid)
   - Hovering a dot shows: ticket key, summary, total cycle time, and per-stage breakdown
   - Tickets with rework show a small rework badge (e.g., "2 re-entries")
5. A stage funnel appears below the scatter plot:
   - Horizontal stacked bar showing average time in each workflow stage
     (only stages within the configured boundaries)
   - The widest segment is the bottleneck — visually obvious
   - Each segment is labeled with the stage name and average days
6. An issue type breakdown table shows: issue type, ticket count, median
   cycle time, P85 cycle time
7. A per-developer breakdown table shows: developer name, tickets completed,
   median cycle time, P85 cycle time, dominant stage (the stage where they
   spend the most time). Sorted alphabetically — not ranked
8. An outlier table lists tickets exceeding 2x the sprint median, sorted by
   cycle time descending. Columns: ticket key, summary, issue type, cycle time,
   per-stage breakdown, rework count
```

```
Flow 2: View Multi-Sprint Cycle Time Trend (Landing)
1. User changes the sprint selector to "Last 3", "Last 5", or "All"
2. Metric cards update to show averaged values across the selected sprints.
   Delta compares the most recent sprint to the one before it
3. A P85 trend line chart appears: one point per sprint, showing P85 cycle
   time over time. A downward trend means the team is getting faster
4. A stage funnel shows average time per stage across all selected sprints,
   revealing the systemic bottleneck
5. A per-sprint summary table shows one row per sprint: sprint name, tickets
   completed, median cycle time, P85 cycle time, outlier count. Clicking a
   row navigates to the single-sprint view for that sprint
```

```
Flow 3: Filter by Sub-Team
1. User selects a sub-team from the sub-team filter in the page toolbar
2. All content recalculates scoped to tickets assigned to developers in that
   sub-team: metric cards, scatter plot, stage funnel, breakdowns, outlier table
3. Percentile lines on the scatter plot recalculate for the filtered set
4. Selecting "All" removes the filter
```

```
Flow 4: Change Sprint Range
1. User changes the sprint selector to a range (Last 3/5/All) or a specific sprint
2. Selecting a range shows the multi-sprint trend view (Flow 2)
3. Selecting a single sprint shows the single-sprint detail view (Flow 1)
4. The URL updates to reflect the selection
```

```
Flow 5: Configure Cycle Time Boundaries
1. User navigates to Settings
2. The Workflow Stages section (from F6) shows the configured stage list
3. Below the stage list, two dropdowns appear:
   - "Cycle starts at" — lists all configured workflow stages. Defaults to
     the second stage (first non-backlog stage)
   - "Cycle ends at" — lists all configured workflow stages plus done
     statuses. Defaults to the first done status
4. User selects different boundaries (e.g., "In Progress" to "Done")
5. User saves the boundary settings (saved via a dedicated endpoint, not
   the main settings save — prevents full-replacement conflicts)
6. The Cycle Time page recalculates using the new boundaries on next visit
7. The stage funnel shows only stages within the selected boundaries
```

```
Flow 6: Percentile Toggle
1. On the single-sprint scatter plot, a toggle control offers: P50, P75, P85, P90
2. The default is P85
3. Switching the toggle changes the highlighted reference line on the scatter plot
   and updates the P85 metric card to show the selected percentile instead
4. P50 line always remains visible (dashed) regardless of toggle selection
```

## API Surface

| Method | Route | Auth | Request | Response | Status codes |
|--------|-------|------|---------|----------|--------------|
| GET | /api/analytics/cycle-time | None | Query params (see below) | Cycle time data | 200, 400 |
| GET | /api/settings/cycle-time-boundaries | None | -- | Boundary settings | 200 |
| PUT | /api/settings/cycle-time-boundaries | None | { startStage, endStage } | Updated boundaries | 200, 400 |

**Cycle time boundaries endpoints:**

GET /api/settings/cycle-time-boundaries:
- Returns the configured start stage and end stage, plus the full list of available stages (from workflow stages + done statuses) for populating the dropdowns.
- 200: Always succeeds. When no boundaries are configured, returns the defaults (second workflow stage, first done status). When no workflow stages are configured, returns empty available stages.

PUT /api/settings/cycle-time-boundaries:
- Saves the start stage and end stage to app settings. This is a dedicated endpoint following the same pattern as excluded-from-scope statuses — it does not go through the main settings save, avoiding full-replacement conflicts.
- 200: Boundaries saved.
- 400: Start stage or end stage does not match a configured workflow stage or done status. Start stage must precede end stage in the configured order.

**Query parameters:**
- `sprintId` (int, optional) — single sprint. Mutually exclusive with `last`.
- `last` (int, optional) — last N closed sprints (e.g., `last=5`). Mutually exclusive with `sprintId`.
- `subTeam` (string, optional) — filter to developers in this sub-team. Omit or pass empty for all.

**Default behavior:** When neither `sprintId` nor `last` is provided, returns data for all closed sprints (multi-sprint mode). The frontend controls the default experience — it pre-selects the most recent closed sprint on page load and sends `sprintId`.

**Single-sprint response shape:**

Top level:
- `sprint` — sprint metadata (id, name, start date, end date). Null when no closed sprints exist.
- `metricCards` — four metric card objects (see below). Null when sprint is null.
- `scatterPlot` — list of ticket data points (see below). Empty when no completed tickets.
- `stageFunnel` — list of stage duration entries (see below). Empty when no completed tickets.
- `issueTypeBreakdown` — list of issue type summary entries. Empty when no completed tickets.
- `developerBreakdown` — list of developer summary entries. Empty when no completed tickets.
- `outliers` — list of outlier ticket entries. Empty when none exceed the threshold.
- `boundaries` — the configured cycle time boundaries (start stage, end stage).

Metric card object (follows established pattern):
- Name
- Value (decimal)
- Display value (formatted string — e.g., "3.2 days", "5 tickets", "2 outliers")
- Delta vs prior sprint (decimal, nullable)
- Delta direction (up, down, flat, or null)
- Delta polarity (positive, negative, neutral)

Scatter plot data point:
- Ticket key
- Ticket summary
- Issue type
- Cycle time in days (decimal)
- Completion date
- Per-stage breakdown (list of stage name + duration in days)
- Rework count (number of times the ticket re-entered any measured stage; 0 if no rework)

Stage funnel entry:
- Stage name
- Average duration in days (decimal)
- Percentage of total cycle time

Issue type breakdown entry:
- Issue type name
- Ticket count
- Median cycle time (days)
- P85 cycle time (days)

Developer breakdown entry:
- Developer display name
- Avatar URL (nullable)
- Sub-team (nullable)
- Tickets completed (count)
- Median cycle time (days)
- P85 cycle time (days)
- Dominant stage (stage name where the developer's tickets spend the most time on average)

Outlier ticket entry:
- Ticket key
- Ticket summary
- Issue type
- Cycle time in days
- Per-stage breakdown
- Rework count

**Multi-sprint response shape:**

Top level:
- `sprints` — ordered list of sprint summaries (id, name, start date, end date) included in the response.
- `metricCards` — four metric cards with values averaged across all selected sprints. Delta compares the most recent sprint to the one before it.
- `trend` — list of per-sprint trend entries (see below).
- `stageFunnel` — average time per stage across all selected sprints.
- `sprintSummaries` — list of per-sprint summary entries (see below).
- `boundaries` — the configured cycle time boundaries.

Trend entry (one per sprint):
- Sprint ID
- Sprint name
- P85 cycle time (days)
- Median cycle time (days)
- Tickets completed (count)

Sprint summary entry:
- Sprint ID
- Sprint name
- Start date
- Tickets completed
- Median cycle time (days)
- P85 cycle time (days)
- Outlier count

**Error conditions:**
- 400: Both `sprintId` and `last` provided simultaneously
- 400: `sprintId` does not match any synced closed sprint
- 400: `last` is less than 1
- 200 with null sprint: no closed sprints exist (not an error)

## Business Rules

1. **Cycle time measures completed tickets only.** A ticket is included in cycle time calculations only if its final status in the sprint matches a value in the done statuses list. Tickets that carried over, were removed, or are still in progress are excluded entirely. The carry-over tracker (F11) handles unfinished work.

2. **Cycle time is the total time between configured boundaries.** For each ticket, cycle time is the accumulated duration spent in workflow stages between the configured start stage and end stage (inclusive). The clock starts when the ticket first enters the start stage and stops when it first enters a stage beyond the end stage (or the done status).

3. **Time is accumulated per stage, not wall-clock.** When a ticket enters a stage, the clock for that stage starts. When it transitions out, the clock stops. If the ticket re-enters that stage later (rework), the clock restarts and the additional time is added. Total cycle time is the sum of time across all measured stages.

4. **Rework is counted, not hidden.** When a ticket transitions back into a previously visited stage, the re-entry is counted as rework. Each ticket carries a rework count — the total number of re-entries across all measured stages. Example: a ticket that goes In Progress → Code Review → In Progress → Code Review → Testing has 1 rework re-entry (it re-entered In Progress once). A ticket that goes In Progress → Code Review → In Progress → Code Review → In Progress → Testing has 2 rework re-entries. Tickets with rework are visually flagged in the scatter plot and ticket tables.

5. **Tickets that never entered the start stage are excluded.** If a ticket's status transitions show it went directly from a pre-start stage (e.g., backlog) to done without ever entering the configured start stage, it has no measurable cycle time and is excluded from all calculations. This prevents tickets closed by administrative action from distorting metrics.

6. **Outlier threshold is 2x the sprint median.** Any ticket whose cycle time exceeds twice the median cycle time of its sprint is classified as an outlier. The outlier count appears as a metric card and the individual tickets are listed in the outlier table.

7. **Percentile calculations use completed tickets only.** P50 (median), P75, P85, and P90 are computed from the set of completed tickets that have measurable cycle time. Excluded tickets (incomplete, never entered start stage) do not affect percentile calculations.

8. **Stage funnel shows only stages within boundaries.** The funnel visualization includes only workflow stages between the configured start stage and end stage. Stages before the start or after the end are omitted. Each segment shows the average time tickets spent in that stage.

9. **The dominant stage is the stage with the highest average time for a developer's tickets.** In the per-developer breakdown, the dominant stage identifies where a developer's tickets spend the most time on average. This helps managers focus coaching conversations ("your tickets sit in code review longest — is there a reviewer bottleneck?").

10. **Sub-team filter scopes everything.** When a sub-team is selected, all computations — metric cards, scatter plot, stage funnel, breakdowns, outlier detection — restrict to tickets assigned to developers in that sub-team. Percentile lines recalculate for the filtered set.

11. **Delta compares to the prior closed sprint.** "Prior" is the closed sprint with the next-earlier start date. When no prior sprint exists, delta is null. Delta polarity: median cycle time, P85 cycle time, and outlier count are positive-down (lower is better). Throughput is positive-up (more completed tickets is better).

12. **Multi-sprint metric cards average across sprints.** Median cycle time card shows the average of per-sprint medians. P85 card shows the average of per-sprint P85 values. Throughput shows the average tickets completed per sprint. Outliers shows the average outlier count per sprint. Delta compares the most recent sprint to the one before it. Per-sprint averaging gives equal weight to each sprint regardless of ticket volume, preventing large sprints from dominating the aggregate. This means the multi-sprint P85 card may differ from a pooled P85 computed across all tickets combined.

13. **Cycle time boundaries default to sensible stages.** When no boundaries are configured, the start stage defaults to the second workflow stage (first non-backlog) and the end stage defaults to the first done status. If workflow stages are not configured at all, the cycle time page shows an empty state directing the user to Settings to configure stages.

14. **Time calculation uses calendar days.** Duration is measured in calendar days (including weekends and holidays), not business days. This matches how stakeholders think about delivery timelines and avoids the complexity of business calendar configuration.

15. **Division by zero produces zero or null.** When no completed tickets exist for a sprint, all metrics are zero and percentiles are null. The stage funnel and breakdowns are empty.

16. **Sprint ordering follows start date.** Sprints are ordered by start date ascending. "Most recent closed sprint" means the closed sprint with the latest start date.

17. **Tickets with no story points are included.** Cycle time is measured by duration, not story points. Tickets without story points are included in all cycle time calculations — unlike SP-based metrics that exclude them.

18. **Stage time is clamped to the sprint window.** When a ticket is in a measured stage at the sprint start, time accumulation begins from the sprint start date (not from when the ticket originally entered that stage, which may have been before the sprint). When a ticket is still in a measured stage at sprint end, time accumulation ends at the sprint end date. This bounds the measurement to the sprint window. Note: this differs from F10's bug time-in-progress calculation, which uses raw transition timestamps without sprint-start clamping. The difference is intentional — F10 measures total capacity consumed by a bug (regardless of when it started), while cycle time measures delivery speed within a sprint context. A ticket that entered "In Progress" 5 days before the sprint shows 0 pre-sprint days in cycle time but would show all 5 in F10's calculation.

19. **Completion date is derived from status transitions.** A ticket's completion date is the timestamp of its earliest status transition to a done status within the sprint window. This is used for the scatter plot X-axis. If a ticket's done-status transitions all predate the sprint window (ticket was already done when added to the sprint), it is excluded — it has no in-sprint cycle to measure.

20. **Excluded-from-scope statuses do not affect cycle time.** The excluded-from-scope statuses setting (from F10) is designed for scope metrics — it excludes tickets from committed/completion rate calculations. Cycle time is not a scope metric. A ticket that completed the full workflow has a valid cycle time regardless of scope exclusion. Cycle time inclusion is governed solely by done status and start-stage entry (rules 1 and 5).

21. **Cycle time uses the current workflow stage configuration.** The stage funnel and per-stage breakdowns use the workflow stages configured at query time, not the stages that were configured when the sprint was active. If a user renames or reorders stages in Settings, historical sprints reflect the new configuration. Stage names from status transitions are matched against current stage names — unrecognized stage names (from a removed or renamed stage) are grouped under an "Other" segment in the funnel.

22. **Percentile toggle applies to single-sprint mode only.** In multi-sprint mode, the trend line always shows P85 (and optionally median). The percentile toggle on the scatter plot is a single-sprint feature — multi-sprint mode has no scatter plot.

## Acceptance Criteria

- [ ] "Cycle Time" appears as a sidebar navigation entry
- [ ] Opening the Cycle Time page loads with the most recent closed sprint selected
- [ ] Sprint selector lists closed sprints and supports single sprint, Last 3, Last 5, and All
- [ ] Four metric cards display: Median Cycle Time, P85 Cycle Time, Throughput, Outliers
- [ ] Metric cards show delta vs prior sprint with correct polarity (lower cycle time = green, higher throughput = green)
- [ ] Delta is absent when no prior closed sprint exists
- [ ] Scatter plot displays one dot per completed ticket with cycle time on Y-axis and completion date on X-axis
- [ ] Scatter plot dots are colored by issue type
- [ ] Scatter plot shows horizontal reference lines at P50 (dashed) and P85 (solid)
- [ ] Hovering a scatter plot dot shows ticket key, summary, cycle time, and per-stage breakdown
- [ ] Tickets with rework show a rework badge on the scatter plot and in ticket tables
- [ ] Percentile toggle (P50/P75/P85/P90) changes the highlighted reference line and the corresponding metric card
- [ ] P85 is the default percentile selection
- [ ] P50 line remains visible regardless of percentile toggle selection
- [ ] Stage funnel displays a horizontal stacked bar with average time per stage
- [ ] Stage funnel only includes stages within the configured cycle time boundaries
- [ ] The widest stage funnel segment is visually identifiable as the bottleneck
- [ ] Issue type breakdown table shows: issue type, ticket count, median, P85
- [ ] Per-developer breakdown table shows: name, tickets completed, median, P85, dominant stage
- [ ] Per-developer table is sorted alphabetically, not by performance
- [ ] Outlier table lists tickets exceeding 2x the sprint median, sorted by cycle time descending
- [ ] Outlier table shows: ticket key, summary, issue type, cycle time, stage breakdown, rework count
- [ ] Multi-sprint view shows a P85 trend line chart with one point per sprint
- [ ] Multi-sprint view shows a stage funnel averaged across all selected sprints
- [ ] Multi-sprint view shows a per-sprint summary table with clickable rows
- [ ] Clicking a sprint row in the summary table navigates to single-sprint view
- [ ] Sub-team filter in the page toolbar filters all page content to that sub-team's developers
- [ ] Percentile lines recalculate when sub-team filter is applied
- [ ] Cycle time boundaries are configurable in Settings (start stage and end stage dropdowns)
- [ ] Boundary dropdowns populate from the configured workflow stages list
- [ ] Default boundaries: start = second workflow stage, end = first done status
- [ ] Cycle time page shows an empty state when no workflow stages are configured
- [ ] Tickets that never entered the configured start stage are excluded from all calculations
- [ ] Cycle time accumulates time per stage (not wall-clock), including rework re-entries
- [ ] Tickets with no story points are included in cycle time calculations
- [ ] URL updates to reflect sprint selection and is linkable
- [ ] GET /api/analytics/cycle-time with `sprintId` returns single-sprint data
- [ ] GET /api/analytics/cycle-time with `last=N` returns multi-sprint data
- [ ] GET /api/analytics/cycle-time with no sprint params returns all closed sprints (multi-sprint mode)
- [ ] GET /api/analytics/cycle-time with `subTeam` filters to that sub-team
- [ ] 400 returned when both `sprintId` and `last` are provided
- [ ] 400 returned when `sprintId` does not match a closed sprint
- [ ] 200 with null sprint data when no closed sprints exist
- [ ] Completion date on scatter plot is derived from the earliest done-status transition within the sprint window
- [ ] Tickets already done before the sprint window are excluded
- [ ] Excluded-from-scope statuses do not affect cycle time inclusion
- [ ] Stage funnel uses the current workflow stage configuration, not historical
- [ ] Unrecognized stage names from renamed/removed stages appear as "Other" in the funnel
- [ ] Percentile toggle is available in single-sprint mode only (not in multi-sprint)
- [ ] Multi-sprint trend line shows P85 regardless of any prior single-sprint toggle selection
- [ ] Sprint-start clamping: time accumulation begins at sprint start for tickets already in a measured stage
- [ ] Sprint-end clamping: time accumulation ends at sprint end for tickets still in a measured stage
- [ ] Rework count reflects total re-entries across all measured stages (e.g., 2 re-entries for a ticket entering a stage 3 times)
- [ ] GET /api/settings/cycle-time-boundaries returns current boundaries and available stages
- [ ] PUT /api/settings/cycle-time-boundaries saves start and end stage without affecting other settings
- [ ] PUT /api/settings/cycle-time-boundaries returns 400 when start stage does not precede end stage
- [ ] PUT /api/settings/cycle-time-boundaries returns 400 when a stage name does not match configured stages

## Out of Scope

- **Business day calculations** — cycle time uses calendar days. Business calendar configuration (weekends, holidays, team schedules) adds significant complexity with limited value for retrospective analysis. Deferred to v2.
- **PR-level cycle time** — tools like LinearB and Hatica break cycle time into coding/pickup/review/deploy phases using git and PR data. This feature measures Jira workflow stages only. PR integration is a future enhancement.
- **Real-time updates** — no SignalR push when sync completes. User refreshes the page after syncing a sprint.
- **Export (PDF/CSV)** — deferred to v2 per the product spec.
- **Per-issue-type workflow pipelines** — one shared pipeline for all issue types (established by F6). Different types may follow different paths, but cycle time measures all types against the same stage sequence.
- **Cycle time SLA configuration** — configurable thresholds for "green/amber/red" cycle time ranges. The P85 and outlier metrics provide equivalent signal without adding settings complexity. Deferred to v2.
- **Cycle time contribution to health score** — the composite health score (F8) currently uses completion, disruption, and carry-over rates. Adding cycle time as a fourth component changes the health score formula and weights. Deferred.
- **Flow efficiency** — the ratio of active work time to total elapsed time (Swarmia's model). Requires distinguishing "actively worked on" from "waiting" within a stage, which status transitions alone cannot determine. Deferred.
- **WIP-over-time chart** — showing how many tickets are in-flight at any point during the sprint. Useful but separable — the in-flight concept is orthogonal to cycle time measurement. Deferred.
- **Scatter plot click-through to Jira** — clicking a dot to open the Jira ticket in a new tab. Requires storing and surfacing Jira instance URL alongside ticket keys. Deferred.
- **Multi-sprint per-developer and per-issue-type breakdowns** — the multi-sprint view shows trend and summary data, not diagnostic breakdowns. Per-developer and per-issue-type tables are single-sprint tools for retro preparation and coaching conversations. The trend view serves a different purpose (longitudinal performance tracking).
- **Per-developer trend sparklines** — v1.md references per-developer sparklines. The team-level P85 trend line covers the longitudinal trend need; per-developer sparklines add chart density without proportional value for a Scrum Master audience. Deferred.
