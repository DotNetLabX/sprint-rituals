# Scope Change & Disruption

**Traces to:** `docs/product/v1.md` §5.2 (Sprint Scope Change & Disruption Analysis)
**Source:** Scratch
**Dependencies:** F2 (Domain Model & Persistence), F3 (Settings System), F5 (Sprint Sync), F6 (Workflow Auto-Detection), F7 (App Shell & Navigation), F8 (Sprint Summary Card — establishes cross-cutting patterns)
**Status:** Done
**Plan:** `docs/plans/ScopeChangeDisruption/plan.md`

---

## Purpose

Scrum Masters need to understand why sprints miss their targets. The Sprint Summary Card (F8) shows a single disruption rate number — this feature provides the analytical depth: what was added, what was removed, when during the sprint it happened, and what category of disruption it was. It answers "why didn't we finish everything?" with evidence for retrospectives and stakeholder conversations.

This feature owns the Sprints page in the sidebar navigation. It builds the page structure that F11 (Carry-Over Tracker) will later extend with carryover-specific sections.

## Entities

This feature introduces one new setting concept and computes all other data from existing entities.

**New: Excluded-from-scope statuses (Settings extension)**
- A configurable list of statuses (e.g., "To Do", "Blocked") stored in app settings
- Default: empty list (all tickets count toward scope metrics)
- When a ticket's final status at sprint end matches any value in this list, it is excluded from all scope metric calculations (committed SP, completion rate denominator, disruption rate denominator, carry-over)
- System-wide — affects all analytics features (F8, F9, F10, F11)

**Existing data queried:**
- Sprint memberships (WasCommitted, AddedAt, RemovedAt, StoryPoints, FinalStatus)
- Tickets (IssueType, CreatedDate, Key, Summary, assignee)
- Status transitions (for bug time-in-progress calculation)
- Sprints (start date, end date, state)
- Developers (sub-team, active status)
- App settings (done statuses, health thresholds, excluded-from-scope statuses)

## User Flows

```
Flow 1: View Multi-Sprint Scope Change Trend (Landing)
1. User navigates to "Sprints" in the sidebar
2. The sprint selector defaults to "Last 5" (showing the 5 most recent closed sprints)
3. Summary metric cards appear at the top: Average Disruption Rate, Average Net Scope Change, Total Bugs Added (count across all selected sprints)
4. A stacked/grouped bar chart shows per-sprint: committed SP (baseline), added SP, removed SP, and completed SP
5. A disruption rate trend line overlays the chart (or appears as a secondary chart)
6. Below the chart, a classification breakdown table shows totals by category: planning overflow, unplanned bugs, scope injection, priority escalation
7. Each category row shows: ticket count, SP total (where available), and percentage of total disruptions
8. The bug count trend is visible as a separate sparkline or inline metric showing bug additions per sprint
```

```
Flow 2: Drill Down to Single-Sprint Detail
1. User clicks a specific sprint bar in the chart (or selects a single sprint from the sprint selector)
2. The URL updates to reflect the selected sprint (route-based, linkable)
3. The view changes to single-sprint detail:
   a. Summary cards: Committed SP, Added SP, Removed SP, Net Scope Change, Disruption Rate, Bug Count — each with delta vs prior sprint
   b. A scope burnup chart showing: committed SP as a flat baseline, total scope line stepping up/down as items are added/removed over the sprint days, completed SP climbing over time
   c. The burnup chart background is shaded into phases: planning phase (days 1-2) and execution phase (day 3+)
   d. A classification breakdown for this sprint (same categories as the trend view but for one sprint)
   e. An event table listing every add/remove event chronologically
4. The event table shows for each event: date (sprint day number + calendar date), ticket key, ticket summary, SP (or "—" for unestimated), issue type, action (added/removed), and classification category
5. Bug time-in-progress appears as a section below the event table: for each bug added mid-sprint, show time spent in active work statuses (In Progress equivalent) — total bug-days consumed
```

```
Flow 3: Filter by Sub-Team
1. User selects a sub-team from the sub-team filter in the page toolbar
2. All content recalculates scoped to tickets assigned to developers in that sub-team:
   - Metric cards, charts, breakdown tables, and event table filter to that sub-team's developers
   - Disruption rate denominators use only that sub-team's committed SP
3. Selecting "All" removes the filter
```

```
Flow 4: Change Sprint Range
1. User changes the sprint selector to "Last 3", "Last 5", "All", or a specific sprint
2. Selecting a range (Last 3/5/All) shows the multi-sprint trend view (Flow 1)
3. Selecting a single sprint shows the single-sprint detail view (Flow 2)
4. All charts and tables update accordingly
```

```
Flow 5: View with Excluded Statuses Applied
1. User has configured excluded-from-scope statuses in Settings (e.g., "To Do", "Blocked")
2. The Sprints page displays both numbers in the committed SP card: "Active: 30 SP | Total: 50 SP"
3. All metric calculations (disruption rate, completion rate, net scope change) use the active committed SP (excluding tickets whose final status matches an excluded status)
4. The event table still shows all events including excluded items, but they are visually dimmed or tagged as "excluded from metrics"
```

## API Surface

| Method | Route | Auth | Request | Response | Status codes |
|--------|-------|------|---------|----------|--------------|
| GET | /api/analytics/scope-change | None | Query params (see below) | Scope change data | 200, 400 |
| GET | /api/settings/excluded-statuses | None | — | List of excluded status strings | 200 |
| PUT | /api/settings/excluded-statuses | None | { statuses: string[] } | Updated list | 200, 400 |

**Scope change query parameters:**
- `sprintId` (int, optional) — single sprint detail mode. Mutually exclusive with `last`.
- `last` (int, optional) — last N closed sprints (e.g., `last=5`). Mutually exclusive with `sprintId`.
- `subTeam` (string, optional) — filter to developers in this sub-team. Omit or pass empty for all.

**Default behavior:** When neither `sprintId` nor `last` is provided, defaults to `last=5`.

**Response shape — multi-sprint mode (when `last` or default):**

Top level:
- `sprints` — ordered list of sprint summaries (id, name, start date, end date) included in this response
- `summaryMetrics` — aggregate cards: average disruption rate (with delta), average net scope change, total bugs added (count)
- `perSprintData` — list of per-sprint scope change entries (see below)
- `classificationBreakdown` — totals across all selected sprints by disruption category

Per-sprint entry:
- Sprint ID
- Committed SP (active — excludes items with excluded final statuses)
- Committed SP total (before exclusion — for "Active vs Total" display)
- Added SP (sum of SP on items added mid-sprint, not removed, not excluded)
- Removed SP (sum of SP on items that were removed from the sprint)
- Net scope change (Added SP - Removed SP)
- Completed SP
- Disruption rate (Added SP / Active Committed SP x 100)
- Bug count (number of bug-type tickets added mid-sprint)

Classification breakdown entry:
- Category name (planning overflow, unplanned bug, scope injection, priority escalation)
- Ticket count
- SP total (sum of story points where available; null for bugs without SP)
- Percentage of total mid-sprint additions (by count)

**Response shape — single-sprint mode (when `sprintId`):**

Top level:
- `sprint` — sprint metadata (id, name, start date, end date, duration)
- `metrics` — metric card objects (committed SP active/total, added SP, removed SP, net scope change, disruption rate, bug count) each with delta vs prior sprint, direction, polarity
- `burnupData` — daily data points for the scope burnup chart (see below)
- `classificationBreakdown` — breakdown for this sprint
- `events` — chronological event list (see below)
- `bugTimeInProgress` — per-bug time-in-progress data (see below)

Burnup data point (one per sprint day):
- Day number (1-based)
- Calendar date
- Total scope SP (cumulative committed + added - removed as of that day)
- Completed SP (cumulative as of that day)
- Phase label ("planning" for days 1-2, "execution" for day 3+)

Event entry:
- Date (calendar date + sprint day number)
- Ticket key
- Ticket summary
- Story points (nullable — absent for unestimated items)
- Issue type
- Action ("added" or "removed")
- Classification category
- Is excluded from metrics (boolean — true when ticket's final status matches an excluded status)

Bug time-in-progress entry:
- Ticket key
- Ticket summary
- Time in active statuses (decimal, in days — fractional days allowed)
- Current status

**Error conditions:**
- 400: Both `sprintId` and `last` provided simultaneously
- 400: `sprintId` does not match any synced closed sprint
- 400: `last` is less than 1

**Excluded statuses endpoints:**
- GET returns the current list (empty array if none configured)
- PUT replaces the list. 400 if any status string is empty or blank.

## Business Rules

1. **"Committed" uses the active commitment.** Committed SP counts tickets present at sprint start whose final status is NOT in the excluded-from-scope statuses list. This is the denominator for disruption rate and completion rate. The total committed SP (before exclusion) is also returned for display purposes.

2. **"Added" means mid-sprint, not removed, not excluded.** Added SP counts tickets where WasCommitted is false, not removed, with story points, and whose final status is not in the excluded-from-scope list. Tickets added mid-sprint that are subsequently removed count as removed, not added.

3. **"Removed" means explicitly taken out of the sprint.** Removed SP counts tickets where removal timestamp is present, regardless of when they were added. A ticket can be committed and then removed, or added and then removed.

4. **Net scope change = Added SP - Removed SP.** Positive means the sprint grew. Negative means it shrank. Zero means scope was stable (or swaps occurred). This is a first-class metric, not derived from disruption rate.

5. **Disruption rate = Added SP / Active Committed SP x 100.** Denominator uses active committed SP (after exclusion). When active committed SP is 0, disruption rate is 0.

6. **Classification is automatic and derived from Jira data.** Each mid-sprint addition is classified into exactly one category using this priority order:
   - **Planning overflow** — added within the first 2 days of the sprint (day 1-2 based on sprint start date). Represents items missed during planning, not real disruption.
   - **Unplanned bug** — issue type is "Bug" AND added after day 2.
   - **Priority escalation** — ticket's creation date is before the sprint start date AND added after day 2. The work already existed but wasn't planned for this sprint — it was pulled in due to changed priorities.
   - **Scope injection** — any remaining mid-sprint addition after day 2. New work that didn't exist before and wasn't planned.

7. **Classification priority resolves overlaps.** A bug created before sprint start that was added on day 5 is classified as "unplanned bug" (issue type check runs before creation date check). Planning overflow wins over all other categories for day 1-2 additions.

8. **Bug count is a parallel disruption signal independent of story points.** Bug count tracks the number of bug-type tickets added mid-sprint regardless of whether they have story points. This captures disruption that SP-based metrics miss when bugs are unestimated.

9. **Bug time-in-progress measures capacity consumed by bugs.** For each bug added mid-sprint, compute the total calendar time spent in active work statuses (statuses between the workflow start and end boundaries from app settings). Expressed in days (fractional). Available only in single-sprint detail view.

10. **Burnup chart uses daily granularity.** Each day of the sprint is a data point. The chart has four series: Scope SP (transition-based cumulative), Completed SP (transition-based cumulative), Committed Total (membership-based daily snapshot, dashed), and Bug SP (area). Scope and completed lines are governed by F23 BR11. The Committed Total line is governed by F23 BR12. Bug area is unchanged from original design.

11. **Phase shading on burnup chart.** Days 1-2 are the "planning" phase. Day 3 onward is the "execution" phase. This provides visual context for when scope changes occurred without requiring a separate timeline chart.

12. **Delta compares to the prior closed sprint.** Single-sprint mode includes delta for each metric card comparing to the closed sprint with the next-earlier start date. When no prior sprint exists, delta is null.

13. **Delta polarity is metric-specific.** Disruption rate and added SP: lower is better (green down, red up). Completed SP and net scope change trending toward zero: neutral. Removed SP: neutral (removal can be good or bad depending on context). Bug count: lower is better.

14. **Multi-sprint mode shows no deltas, uses averages.** Summary metrics in multi-sprint mode show averages (disruption rate, net scope change) or totals (bug count) across the selected sprints. No delta indicators in multi-sprint mode — the trend chart serves this purpose.

15. **Sub-team filter scopes everything.** When a sub-team is selected, all computations restrict to tickets assigned to developers in that sub-team. Committed SP, added SP, removed SP, classification breakdown, event table, and bug metrics all scope to that sub-team.

16. **Excluded-from-scope statuses are case-insensitive.** Matching a ticket's final status against the exclusion list ignores case.

17. **Tickets with no story points.** Unestimated tickets are excluded from all SP-based metrics (committed SP, added SP, removed SP, net scope change). They DO count in ticket-based metrics (bug count, classification breakdown ticket count, event table entries).

18. **Event table includes all events regardless of exclusion.** Items whose final status matches an excluded status still appear in the event table (for complete audit trail) but are visually marked as excluded. They do not contribute to metric calculations.

19. **The Sprints page is the home for sprint-level analytics.** F10 builds the page structure (toolbar, layout, routing for trend vs detail views). F11 (Carry-Over Tracker) will add sections to this same page — carry-over data is sprint-level context that belongs alongside scope change data.

20. **Division by zero produces zero.** When active committed SP is 0, disruption rate is 0. When no bugs exist, bug time-in-progress section is hidden.

## Acceptance Criteria

- [ ] Navigating to "Sprints" in the sidebar shows the multi-sprint scope change trend view
- [ ] Default sprint selection is "Last 5" closed sprints
- [ ] Summary metric cards display: Average Disruption Rate, Average Net Scope Change, Total Bugs Added
- [ ] Stacked/grouped bar chart shows committed, added, removed, and completed SP per sprint
- [ ] Disruption rate trend is visible across selected sprints
- [ ] Classification breakdown table shows: planning overflow, unplanned bug, scope injection, priority escalation with counts and SP
- [ ] Clicking a sprint bar (or selecting a single sprint) navigates to single-sprint detail view
- [ ] Single-sprint detail URL is route-based and linkable
- [ ] Single-sprint metric cards show: Committed SP (active/total), Added SP, Removed SP, Net Scope Change, Disruption Rate, Bug Count
- [ ] Each single-sprint metric card shows delta vs prior sprint with polarity-aware coloring
- [ ] Scope burnup chart shows daily data points with total scope line and completed SP line
- [ ] Burnup chart background distinguishes planning phase (days 1-2) from execution phase (day 3+)
- [ ] Event table lists all add/remove events chronologically with: date, ticket key, summary, SP, issue type, action, classification
- [ ] Bug time-in-progress section shows time spent in active statuses for each mid-sprint bug
- [ ] Bug time-in-progress section is hidden when no mid-sprint bugs exist
- [ ] Sub-team filter restricts all content to the selected sub-team's developers
- [ ] Sprint selector supports "Last 3", "Last 5", "All", and individual sprint selection
- [ ] Selecting a range shows multi-sprint trend; selecting a single sprint shows detail view
- [ ] GET /api/analytics/scope-change with no params defaults to last 5 closed sprints
- [ ] GET /api/analytics/scope-change with `sprintId` returns single-sprint detail data
- [ ] GET /api/analytics/scope-change with `last=N` returns multi-sprint trend data
- [ ] 400 returned when both `sprintId` and `last` are provided
- [ ] 400 returned when `sprintId` does not match a synced closed sprint
- [ ] Classification categories are assigned automatically using the priority rules (planning overflow > unplanned bug > priority escalation > scope injection)
- [ ] Planning overflow classification applies to items added within 2 days of sprint start
- [ ] Unplanned bug classification applies to bug-type tickets added after day 2
- [ ] Priority escalation applies to tickets created before sprint start, added after day 2 (that aren't bugs)
- [ ] Scope injection is the fallback for remaining mid-sprint additions after day 2
- [ ] Excluded-from-scope statuses setting is configurable via Settings UI
- [ ] Tickets with excluded final statuses are removed from SP metric calculations
- [ ] Both active and total committed SP are displayed ("Active: 30 SP | Total: 50 SP")
- [ ] Excluded items still appear in the event table but are visually marked as excluded
- [ ] Net scope change metric correctly computes Added SP - Removed SP
- [ ] Bug count tracks bug-type tickets regardless of whether they have story points
- [ ] Tickets with no story points are excluded from SP metrics but included in ticket counts
- [ ] Division by zero cases (no committed SP, no bugs) produce 0 or hide the section respectively
- [ ] GET /api/settings/excluded-statuses returns the current exclusion list
- [ ] PUT /api/settings/excluded-statuses updates the exclusion list
- [ ] Status matching for exclusion is case-insensitive

## Out of Scope

- **Net-zero swap annotations** — identifying specific ticket pairs that were "swapped" (one removed, one added with matching SP). The net scope change metric captures the aggregate signal. Individual swap detection is deferred pending user feedback on whether item-level pairing adds value.
- **Configurable grace period** — the planning overflow threshold is fixed at 2 days (matching F8's mid-sprint disruption flag). Making it configurable adds a setting with low user demand in v1.
- **Manual disruption tagging** — all classification is automatic from Jira data. No UI for overriding or manually tagging disruption categories.
- **Disruption alerts or notifications** — no Slack/email when disruption rate exceeds a threshold. Deferred to v2.
- **Sprint comparison mode** — no side-by-side comparison of two specific sprints. The trend chart and delta indicators provide cross-sprint context.
- **Export (PDF/PNG)** — deferred to v2 per the product spec.
- **Real-time updates** — no SignalR push when sync completes. User refreshes after syncing.
- **Carry-over sections** — the Sprints page will host carryover data, but those sections are owned by F11. This feature builds the page shell and scope change sections only.
- **Churn rate composite metric** — Harness SEI's formula (added + removed + estimation changes / committed) is a potential v2 addition. V1 focuses on disruption rate and net scope change as separate signals.
- **Per-developer disruption attribution** — "who caused the scope change" or "whose tickets were disrupted" is not tracked. Disruption is analyzed at the sprint/team level, not blamed on individuals.
