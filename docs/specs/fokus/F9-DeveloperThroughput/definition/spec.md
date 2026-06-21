# Developer Throughput

**Traces to:** `docs/product/v1.md` §5.1 (Developer Throughput)
**Source:** Scratch
**Dependencies:** F2 (Domain Model & Persistence), F5 (Sprint Sync)
**Status:** Done
**Plan:** `docs/plans/DeveloperThroughput/plan.md`

---

## Purpose

Scrum Masters need to know how much each developer delivers per sprint — not to rank people, but to spot capacity problems, set realistic commitments, and have data-backed conversations about workload. Jira shows ticket counts but not per-developer SP trends across sprints. This feature fills that gap with a table for the current sprint and a trend chart across sprints.

## Entities

This feature introduces one new concept and queries existing data.

**New: Developer Sprint Capacity**
- Represents a developer's availability for a specific sprint as a percentage (0–100, default 100)
- Set per developer per sprint — e.g., a developer on vacation for half the sprint has 50% capacity, fully absent = 0%
- A developer with 0% capacity in a sprint is excluded from rolling average calculations for that sprint
- Managed on the Developers page, per sprint

**Existing data queried:**
- Sprint memberships (committed tickets, story points, final status)
- Tickets (assignee, issue type)
- Developers (display name, sub-team, active status)
- App settings (done statuses list)

## User Flows

```
Flow 1: View Developer Throughput for a Sprint
1. User navigates to the Developers section
2. The page loads with the most recent closed sprint selected
3. A table displays one row per developer with: name, sub-team, SP assigned, SP completed, completion %, tickets done, tickets carried over
4. Each metric shows a delta indicator (↑/↓ with signed number) comparing to the prior sprint
5. Delta indicators are colored: green for improvement, red for regression (polarity-aware — higher completion % is green, higher carry-over is red). SP assigned delta is neutral (no color) — more assigned is neither inherently good nor bad
6. Developers with zero tickets in the selected sprint appear with all-zero values — they are not hidden
7. Only active developers are shown
```

```
Flow 2: View Throughput Trend Across Sprints
1. User changes the sprint selector to "Last 3", "Last 5", or "All"
2. The table updates to show averaged values across the selected range — no delta indicators in multi-sprint view
3. A multi-line chart appears below the table showing SP completed per developer across the selected sprints
4. Each developer is a separate line on the chart
5. Lines show a 3-sprint rolling average (smoothed trend), not raw per-sprint values
6. Hovering a data point on the chart shows the exact rolling average value for that developer and sprint
```

```
Flow 3: Set Developer Sprint Capacity
1. User navigates to the Developers page
2. User selects a sprint from the sprint selector
3. Each developer row shows a capacity field (percentage, default 100)
4. User adjusts capacity for a developer (e.g., 50% for half-sprint vacation, 0% for fully absent)
5. Change saves immediately
6. Throughput metrics and rolling averages update to reflect the new capacity setting
```

```
Flow 4: Filter by Sub-Team
1. User selects a sub-team from the sub-team filter
2. The table filters to show only developers in that sub-team
3. The chart updates to show only those developers' trend lines
4. Metrics recalculate scoped to the filtered developers only
5. Selecting "All" removes the filter
```

## API Surface

| Method | Route | Auth | Request | Response | Status codes |
|--------|-------|------|---------|----------|--------------|
| GET | /api/analytics/developer-throughput | None | Query params (see below) | Developer throughput data | 200, 400 |
| PUT | /api/developers/{accountId}/capacity | None | { sprintId, capacityPercent } | Updated capacity | 200, 400, 404 |
| GET | /api/developers/{accountId}/capacity | None | ?sprintId={id} (optional) | Capacity entries | 200, 404 |

**Query parameters:**
- `sprintId` (int, optional) — single sprint. Mutually exclusive with `last`.
- `last` (int, optional) — last N closed sprints (e.g., `last=3`). Mutually exclusive with `sprintId`.
- `subTeam` (string, optional) — filter developers by sub-team name. Omit or pass empty for all.

**Default behavior:** When neither `sprintId` nor `last` is provided, returns all closed sprints. The frontend controls the default experience — it pre-selects the most recent closed sprint on page load and sends `sprintId`. When the user selects "All", the frontend sends no sprint parameters.

**Response shape:**

Top level:
- `sprints` — ordered list of sprint summaries (id, name, start date, end date) included in this response
- `developers` — list of developer throughput entries (see below)

Per developer entry:
- Developer identity: display name, sub-team, avatar URL
- Per-sprint breakdown (one entry per sprint in the range):
  - Sprint ID
  - SP assigned (total SP on non-removed tickets assigned to this developer)
  - SP completed (SP on tickets whose final status is in the done statuses list)
  - Completion % (SP completed / SP assigned × 100, or 0 if no SP assigned)
  - Tickets done (count of tickets with final status in done list)
  - Tickets carried over (count of non-removed tickets with final status not in done list)
  - Capacity % (developer's availability for this sprint, default 100)
  - Rolling average SP completed (average of this sprint and up to 2 prior sprints where capacity > 0%; null if fewer than 3 qualifying data points available)
- Delta values (only present when a single sprint is selected):
  - SP assigned delta (current − prior sprint, neutral — no polarity)
  - SP completed delta (current − prior sprint)
  - Completion % delta
  - Tickets done delta
  - Tickets carried over delta
  - Delta direction for each (up, down, or flat; SP assigned uses "neutral" instead of up/down polarity)

**Error conditions:**
- 400: Both `sprintId` and `last` provided simultaneously
- 400: `sprintId` does not match any synced sprint
- 400: `last` is less than 1
- 200 with empty results: No closed sprints exist (not an error — just no data yet)

**Capacity endpoints:**

PUT /api/developers/{accountId}/capacity:
- 200: Capacity saved
- 400: `capacityPercent` outside 0–100 range, or `sprintId` missing
- 404: Developer with given accountId not found

GET /api/developers/{accountId}/capacity:
- 200: Returns capacity entries (all sprints if `sprintId` omitted, single sprint if provided). Sprints with no explicit entry are not returned — the consumer assumes 100% default.
- 404: Developer with given accountId not found

## Business Rules

1. **"Assigned" excludes removed tickets.** A developer's assigned SP for a sprint counts only tickets where the developer is the assignee and the ticket was not removed from the sprint (removal timestamp is absent). Removed tickets do not affect the developer's metrics for that sprint.

2. **"Completed" uses the done statuses list.** A ticket is completed in a sprint if its final status matches any value in the app settings done statuses list. This list is user-configurable (default: "Done", "Closed").

3. **Assignee at sync time.** Attribution uses the assignee recorded when the sprint was synced. Mid-sprint reassignment history is not tracked — the person holding the ticket at sync time gets credit.

4. **Zero-ticket developers are visible.** If a developer has no tickets in a sprint, they appear in the table with all metrics at zero. They are never hidden — hiding them would mask capacity issues.

5. **Only active developers.** Developers marked as inactive are excluded from all throughput results. They do not appear in the table or chart.

6. **Delta is single-sprint only.** When viewing a single sprint, each metric includes a delta comparing to the prior closed sprint. When viewing multiple sprints (via `last`), no delta is provided — the chart serves the trend purpose instead.

7. **Delta polarity.** Delta direction is polarity-aware per metric:
   - SP assigned: neutral (no color — more assigned is neither good nor bad)
   - SP completed: higher is better (green up, red down)
   - Completion %: higher is better
   - Tickets done: higher is better
   - Tickets carried over: lower is better (green down, red up)

8. **Rolling average window is 3 sprints, capacity-aware.** The rolling average for SP completed uses a fixed 3-sprint window. Sprints where the developer has 0% capacity are excluded from the window — they don't count as data points. The window looks backward from each sprint and collects the 3 most recent sprints where capacity > 0%. If fewer than 3 qualifying sprints exist, the rolling average is null.

9. **Developer sprint capacity.** Each developer has a capacity percentage (0–100) per sprint, defaulting to 100. This represents their availability — 50 means half the sprint on vacation, 0 means fully absent. Capacity is set manually on the Developers page. A developer with 0% capacity in a sprint still appears in the table (with their actual metrics), but that sprint is excluded from their rolling average calculation.

10. **Sprint ordering.** Sprints are ordered by start date ascending. "Most recent closed sprint" means the closed sprint with the latest start date. "Prior sprint" for delta means the closed sprint with the next-earlier start date.

11. **No SP tickets count in ticket metrics only.** Tickets with no story points contribute to ticket done / ticket carried over counts but are excluded from SP assigned, SP completed, and completion %. They do not count as zero SP — they are simply absent from SP calculations.

12. **Sub-team filter applies to developers, not tickets.** Filtering by sub-team shows developers in that sub-team with all their tickets — it does not filter tickets by sub-team.

13. **"All" sprints returns all closed sprints.** When neither `sprintId` nor `last` is provided, the endpoint returns data for every closed sprint. The frontend controls the default user experience by pre-selecting the most recent closed sprint on page load.

## Acceptance Criteria

- [ ] GET /api/analytics/developer-throughput with no parameters returns data for all closed sprints
- [ ] GET /api/analytics/developer-throughput with `sprintId` returns data for that single sprint
- [ ] GET /api/analytics/developer-throughput with `last=N` returns data for the last N closed sprints
- [ ] Response includes one entry per active developer, even those with zero tickets
- [ ] Inactive developers are excluded from the response
- [ ] SP assigned excludes tickets that were removed from the sprint
- [ ] SP completed counts only tickets whose final status matches a value in the done statuses list
- [ ] Completion % is SP completed / SP assigned × 100, or 0 when SP assigned is 0
- [ ] Tickets done counts tickets with final status in done list
- [ ] Tickets carried over counts non-removed tickets with final status not in done list
- [ ] Tickets with no story points are included in ticket counts but excluded from SP metrics
- [ ] Single-sprint response includes delta values comparing to the prior closed sprint
- [ ] SP assigned delta is present but uses neutral polarity (no green/red)
- [ ] Delta direction reflects polarity for other metrics (carried over: lower is green; SP completed, completion %, tickets done: higher is green)
- [ ] Multi-sprint response (via `last` parameter or no sprint params) includes no delta values
- [ ] Multi-sprint response includes per-sprint breakdown for each developer across all requested sprints
- [ ] Developer sprint capacity defaults to 100% when not explicitly set
- [ ] Capacity can be set per developer per sprint (0–100%) on the Developers page
- [ ] Developers with 0% capacity in a sprint still appear in the table with actual metrics
- [ ] Rolling average excludes sprints where the developer has 0% capacity
- [ ] Rolling average is computed from 3 qualifying sprints (capacity > 0%); null when fewer than 3 qualifying sprints available
- [ ] `subTeam` filter returns only developers in that sub-team
- [ ] Sprints are ordered by start date ascending in the response
- [ ] 400 returned when both `sprintId` and `last` are provided
- [ ] 400 returned when `sprintId` does not match a synced sprint
- [ ] 400 returned when `last` is less than 1

## Out of Scope

- **CSV or PDF export of developer data** — export capabilities deferred to v2.
- **Developer detail drill-down page** — no per-developer page showing their individual ticket list. The table shows aggregates only. Ticket-level detail is a future enhancement.
- **Comparison mode** — no side-by-side sprint comparison for a single developer. Deferred.
- **Configurable rolling average window** — fixed at 3 sprints. Making it user-configurable adds complexity without clear value in v1.
- **Velocity forecasting** — no predictive "expected SP next sprint" based on historical throughput. Deferred.
- **Real-time updates** — no SignalR push when sync completes. User refreshes the page after syncing.
