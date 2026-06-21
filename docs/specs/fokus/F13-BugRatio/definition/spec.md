# Bug Ratio

**Traces to:** `docs/product/v1.md` §5.5 (Bug Ratio Per Developer)
**Source:** Scratch
**Dependencies:** F2 (Domain Model & Persistence), F3 (Settings System), F5 (Sprint Sync), F7 (App Shell & Navigation), F8 (Sprint Summary Card — cross-cutting patterns), F9 (Developer Throughput — Developers page structure)
**Status:** Done
**Plan:** `docs/plans/BugRatio/plan.md`

---

## Purpose

Scrum Masters need to know how much developer capacity goes to bug fixing vs. planned feature work. A sustained high bug ratio signals quality problems upstream, misallocation, or mounting technical debt. Jira shows individual ticket types but not the cross-sprint trend of bug capacity drain per developer. This feature adds a Bug Ratio tab to the Developers page, complementing throughput data with work type composition analysis.

## Entities

This feature introduces two new settings and computes all other data from existing entities.

**New: Bug ratio alert settings (Settings extension)**
- Bug ratio alert threshold: a percentage (0–100, default 50). When a developer's completed-work bug ratio exceeds this threshold for consecutive sprints, an alert flag appears on the Developers page.
- Bug ratio consecutive sprint count: an integer (1–10, default 2). How many consecutive sprints a developer must exceed the threshold before the alert activates.
- Both properties are added to the app settings entity alongside existing health thresholds. Managed through the existing settings read/save endpoints — no new settings endpoints needed.

**Existing data queried:**
- Sprint memberships (FinalStatus, StoryPoints, RemovedAt)
- Tickets (IssueType, Key, Summary, AssigneeId)
- Sprints (Id, Name, StartDate, EndDate, State)
- Developers (DisplayName, SubTeam, AvatarUrl, IsActive)
- App settings (DoneStatuses, ExcludedFromScopeStatuses)

## User Flows

```
Flow 1: View Multi-Sprint Bug Ratio (Landing)
1. User navigates to "Developers" in the sidebar
2. The page shows two tabs: Throughput (existing F9 content) and Bug Ratio
3. User clicks the Bug Ratio tab
4. The sprint selector defaults to "Last 5" closed sprints
5. Summary metric cards appear at the top: Team Bug Ratio %, Total Bug SP, Total Non-Bug SP
6. A team-level bug ratio trend line chart shows the bug ratio percentage per sprint across the selected range
7. Below the trend chart, a per-developer stacked bar chart shows Bug SP (red) vs Non-Bug SP (blue) per sprint for each developer — making capacity allocation patterns visible across sprints
8. Below the chart, a per-developer table shows: name, sub-team, bug SP, non-bug SP, bug ratio %, bug ticket count, non-bug ticket count (totaled across selected sprints, ratio computed from totals)
9. Developers with an active alert (bug ratio above threshold for N+ consecutive sprints) show a warning badge in the table
10. An issue type breakdown section shows completed ticket counts grouped by raw Jira issue type (Story, Bug, Task, etc.) totaled across the selected sprints — giving visibility into work composition beyond the binary split
11. Only active developers are shown
```

```
Flow 2: Single-Sprint Bug Ratio Detail
1. User selects a single sprint from the sprint selector
2. The view changes to single-sprint detail mode:
   a. Summary metric cards: Team Bug Ratio %, Bug SP, Non-Bug SP — each with delta vs prior sprint
   b. Per-developer table with: name, sub-team, bug SP, non-bug SP, bug ratio %, bug ticket count, non-bug ticket count — each with delta vs prior sprint
   c. Alert badges still visible on developers who meet the consecutive sprint threshold (computed from full sprint history, not just the selected sprint)
   d. Issue type breakdown for this sprint (completed tickets by raw Jira issue type)
3. Delta polarity: lower bug ratio is better (green down, red up). Bug SP: lower is better. Non-Bug SP: higher is better.
```

```
Flow 3: Filter by Sub-Team
1. User selects a sub-team from the sub-team filter
2. All content recalculates scoped to developers in that sub-team:
   - Metric cards, charts, table, and issue type breakdown filter to that sub-team's developers
   - Team-level bug ratio uses only that sub-team's completed SP
3. Selecting "All" removes the filter
```

```
Flow 4: Change Sprint Range
1. User changes the sprint selector to "Last 3", "Last 5", "All", or a specific sprint
2. Selecting a range (Last 3/5/All) shows the multi-sprint trend view (Flow 1)
3. Selecting a single sprint shows the single-sprint detail view (Flow 2)
4. All charts and tables update accordingly
```

## API Surface

| Method | Route | Auth | Request | Response | Status codes |
|--------|-------|------|---------|----------|--------------|
| GET | /api/analytics/bug-ratio | None | Query params (see below) | Bug ratio data | 200, 400 |

**Query parameters:**
- `sprintId` (int, optional) — single sprint. Mutually exclusive with `last`.
- `last` (int, optional) — last N closed sprints. Mutually exclusive with `sprintId`.
- `subTeam` (string, optional) — filter developers by sub-team name. Omit or pass empty for all.

**Default behavior:** When neither `sprintId` nor `last` is provided, defaults to `last=5`.

**Response shape — multi-sprint mode (when `last` or default):**

Top level:
- `sprints` — ordered list of sprint summaries (id, name, start date, end date)
- `teamMetrics` — team-level aggregates: bug ratio % (computed from totals, not averaged), total bug SP, total non-bug SP, total completed SP, and per-sprint trend data (one entry per sprint: bug ratio %, bug SP, non-bug SP, completed SP)
- `issueTypeBreakdown` — completed ticket counts by raw Jira issue type, totaled across all selected sprints (type name, ticket count, SP total)
- `developers` — list of per-developer bug ratio entries (see below)

Per developer entry (multi-sprint):
- Developer identity: display name, sub-team, avatar URL
- Totals across the selected range: bug SP, non-bug SP, total completed SP, bug ratio %, bug ticket count, non-bug ticket count
- Per-sprint breakdown (one entry per sprint): sprint ID, bug SP, non-bug SP, total completed SP, bug ratio %, bug ticket count, non-bug ticket count
- Alert status: whether the alert is active, current consecutive sprint count above threshold

**Response shape — single-sprint mode (when `sprintId`):**

Top level:
- `sprint` — sprint metadata (id, name, start date, end date)
- `teamMetrics` — team-level: bug ratio %, bug SP, non-bug SP, completed SP, each with delta vs prior sprint (value, direction, polarity)
- `issueTypeBreakdown` — completed ticket counts by raw Jira issue type for this sprint
- `developers` — list of per-developer entries (see below)

Per developer entry (single sprint):
- Developer identity: display name, sub-team, avatar URL
- Metrics: bug SP, non-bug SP, total completed SP, bug ratio %, bug ticket count, non-bug ticket count
- Delta values (current minus prior sprint): bug SP delta, non-bug SP delta, bug ratio % delta, bug ticket count delta, non-bug ticket count delta, each with direction and polarity
- Alert status: whether the alert is active, current consecutive sprint count above threshold

**Error conditions:**
- 400: Both `sprintId` and `last` provided simultaneously
- 400: `sprintId` does not match any synced closed sprint
- 400: `last` is less than 1
- 200 with empty results: No closed sprints exist (not an error — just no data yet)

## Business Rules

1. **"Bug" is determined by issue type.** A ticket is a bug if its issue type is "Bug" (exact match, case-sensitive — Jira consistently returns this casing). All other issue types (Story, Task, Sub-task, Improvement, custom types) are non-bug. This matches the existing bug detection pattern used by F10 (Scope Change). No configurable classification rules in v1 — the binary split uses the raw Jira issue type.

2. **Only completed tickets count.** Bug ratio measures completed work only — tickets whose final status in the sprint matches a value in the done statuses list. Assigned-but-unfinished tickets do not contribute to the ratio. This measures capacity actually consumed, not planned allocation.

3. **Bug ratio = Bug SP / Total Completed SP x 100.** Per developer per sprint: sum of SP on completed bug tickets divided by sum of SP on all completed tickets. At the team level: sum of all developers' completed bug SP divided by sum of all developers' total completed SP.

4. **Removed tickets are excluded.** Tickets removed from the sprint (removal timestamp present) do not count toward any bug ratio metrics, matching the throughput pattern.

5. **Excluded-from-scope statuses apply.** Tickets whose final status matches an excluded-from-scope status are excluded from all bug ratio calculations, consistent with the cross-cutting behavior introduced by F10.

6. **Tickets with no story points are excluded from SP metrics.** They count in ticket-based metrics (bug ticket count, non-bug ticket count, issue type breakdown counts) but are absent from SP calculations (bug SP, non-bug SP, bug ratio %).

7. **Zero completed SP produces zero bug ratio.** When a developer has no completed tickets with story points in a sprint, their bug ratio is 0% — not undefined or null.

8. **Multi-sprint aggregation uses ratio of totals.** The bug ratio across multiple sprints is total bug SP across all selected sprints divided by total completed SP across all selected sprints. This applies to both team-level and per-developer aggregated ratios. This prevents high-SP sprints from skewing an average-of-ratios calculation. Individual per-sprint breakdown data is still provided for the trend chart.

9. **Alert uses full sprint history.** The consecutive sprint threshold is evaluated against all available sprint data, not just the sprints in the current view. If viewing "Last 3" and a developer has been above threshold for 4 consecutive sprints (including one outside the view), the alert still fires.

10. **Alert evaluates from the most recent sprint backward.** Starting from the most recent closed sprint, count backward how many consecutive sprints the developer's bug ratio exceeds the threshold. If this count meets or exceeds the configured consecutive sprint count, the alert is active. A gap (one sprint below threshold) resets the count.

11. **Developers with zero completed SP in a sprint break the consecutive streak.** A sprint with no completed SP-bearing tickets results in 0% bug ratio, which is below any positive threshold, resetting the consecutive count.

12. **Only active developers.** Developers marked as inactive are excluded from all bug ratio results.

13. **Zero-ticket developers are visible.** Active developers with zero completed tickets in the selected sprints appear in the table with all-zero values. They are never hidden — consistent with the Throughput tab behavior (F9).

14. **Delta is single-sprint only.** When viewing a single sprint, each metric includes a delta comparing to the prior closed sprint. When viewing multiple sprints, no delta is provided — the trend charts serve this purpose.

15. **Delta polarity.** Bug ratio %: lower is better (green down, red up). Bug SP: lower is better. Non-bug SP: higher is better. Bug ticket count: lower is better. Non-bug ticket count: higher is better.

16. **Sub-team filter applies to developers, not tickets.** Filtering by sub-team shows developers in that sub-team with all their tickets. Team-level metrics recalculate using only those developers' data.

17. **Sprint ordering.** Sprints are ordered by start date ascending. "Most recent closed sprint" means the closed sprint with the latest start date. "Prior sprint" for delta means the closed sprint with the next-earlier start date.

18. **Issue type breakdown shows completed tickets only.** The breakdown counts tickets by raw Jira issue type (case-preserved as-is from Jira). Only tickets with a final status in the done statuses list are included. This provides granularity within the non-bug category — e.g., how much of the "non-bug" work is Stories vs Tasks vs Sub-tasks.

19. **Tab structure on the Developers page.** Bug Ratio is the second tab on the Developers page, after Throughput. The sprint selector and sub-team filter persist across tabs. Throughput remains the default active tab.

## Acceptance Criteria

- [ ] Developers page shows tabs: Throughput and Bug Ratio
- [ ] Throughput tab contains existing F9 developer throughput content
- [ ] Throughput remains the default active tab
- [ ] Sprint selector and sub-team filter persist across tabs
- [ ] Bug Ratio tab loads with "Last 5" as default sprint range
- [ ] Team-level summary cards display: Bug Ratio %, Total Bug SP, Total Non-Bug SP
- [ ] Team bug ratio trend line chart shows bug ratio % per sprint across the selected range
- [ ] Per-developer stacked bar chart shows Bug SP (red) vs Non-Bug SP (blue) per sprint
- [ ] Per-developer table shows: name, sub-team, bug SP, non-bug SP, bug ratio %, bug ticket count, non-bug ticket count
- [ ] Issue type breakdown shows completed ticket counts by raw Jira issue type
- [ ] Single-sprint view shows per-developer metrics with delta vs prior sprint
- [ ] Delta polarity: lower bug ratio / bug SP is green; higher non-bug SP is green
- [ ] Alert badge appears on developers whose bug ratio exceeds the configured threshold for the configured number of consecutive sprints
- [ ] Alert threshold is configurable in Settings (default 50%)
- [ ] Consecutive sprint count threshold is configurable in Settings (default 2)
- [ ] Alert evaluates against full sprint history, not just the viewed range
- [ ] Only completed tickets (final status in done statuses list) count toward bug ratio
- [ ] Bug classification: issue type "Bug" (exact match, case-sensitive) = bug; everything else = non-bug
- [ ] Removed tickets (removal timestamp present) are excluded from all metrics
- [ ] Excluded-from-scope statuses are respected in all calculations
- [ ] Tickets with no story points are excluded from SP metrics but included in ticket counts
- [ ] Zero completed SP produces 0% bug ratio (not null or error)
- [ ] Multi-sprint bug ratio uses ratio of totals (total bug SP / total completed SP), not average of per-sprint ratios
- [ ] Multi-sprint per-developer response includes per-sprint breakdown for chart rendering
- [ ] Sub-team filter restricts all content to the selected sub-team's developers
- [ ] Only active developers appear in results
- [ ] Active developers with zero completed tickets appear in the table with all-zero values (not hidden)
- [ ] GET /api/analytics/bug-ratio with no params defaults to last 5 closed sprints
- [ ] GET /api/analytics/bug-ratio with `sprintId` returns single-sprint detail data
- [ ] GET /api/analytics/bug-ratio with `last=N` returns multi-sprint trend data
- [ ] 400 returned when both `sprintId` and `last` are provided
- [ ] 400 returned when `sprintId` does not match a synced closed sprint
- [ ] 400 returned when `last` is less than 1
- [ ] Inactive developers are excluded from the response

## Out of Scope

- **Configurable bug classification rules** — no rules engine for mapping issue types to categories. The binary Bug vs Non-Bug split uses the raw Jira issue type. A LinearB-style investment profile with configurable categories is deferred to v2.
- **Bug origin tracking** — no analysis of where bugs come from (which epic, which sprint introduced them). Root cause analysis is a separate feature.
- **Bug severity breakdown** — no split by priority (P1 vs P2 vs P3). The ratio treats all bugs equally regardless of priority.
- **Defect leakage metric** — tracking bugs escaping to production requires deployment integration, deferred per v1 scope.
- **Per-developer drill-down page** — no individual developer page showing their full ticket list filtered by type. The table shows aggregates only.
- **Rolling average for bug ratio** — unlike throughput (F9), no 3-sprint rolling average. The trend chart and consecutive-sprint alert provide trend context without the added complexity.
- **Export (PDF/PNG)** — deferred to v2 per the product spec.
- **Real-time updates** — no SignalR push when sync completes. User refreshes after syncing.
- **Retrofitting F9 excluded-from-scope statuses** — F9 (Developer Throughput) was implemented before F10 introduced excluded-from-scope statuses and does not apply them. This means the Throughput tab and Bug Ratio tab may show different total ticket counts for the same sprint when exclusions are configured. Aligning F9 is a separate backlog item, not a BugRatio requirement.
