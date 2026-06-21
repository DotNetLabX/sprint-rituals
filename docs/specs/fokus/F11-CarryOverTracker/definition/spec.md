# Carry-Over Tracker

**Traces to:** `docs/product/v1.md` §5.3 (Carry-Over Tracker)
**Source:** Scratch
**Dependencies:** F2 (Domain Model & Persistence), F3 (Settings System), F5 (Sprint Sync), F6 (Workflow Auto-Detection), F7 (App Shell & Navigation), F8 (Sprint Summary Card — cross-cutting patterns), F10 (Scope Change & Disruption — Sprints page structure, excluded-from-scope statuses)
**Status:** Done
**Plan:** `docs/plans/CarryOverTracker/plan.md`

---

## Purpose

The Sprint Summary Card (F8) shows a single carry-over rate number. This feature provides the analytical depth: which tickets didn't finish, where they're stuck, how long they've been stuck, and whether carry-over tickets from prior sprints eventually get completed. It answers "what keeps rolling from sprint to sprint and why isn't it getting done?" with evidence for retrospectives and capacity planning.

This feature extends the Sprints page built by F10 (Scope Change & Disruption). It adds carry-over sections below the scope change content on the same page — carry-over data is sprint-level context that belongs alongside scope change data.

## Entities

This feature introduces no new domain entities or settings. It queries existing data and uses settings introduced by other features.

**Existing data queried:**
- Sprint memberships (WasCommitted, FinalStatus, StoryPoints, AddedAt, RemovedAt)
- Tickets (Key, Summary, IssueType, StoryPoints, AssigneeId)
- Sprints (Id, Name, StartDate, EndDate, State)
- Developers (SubTeam, IsActive)
- App settings (DoneStatuses, WorkflowStages, ExcludedFromScopeStatuses from F10)

**Workflow stage mapping:** Each carry-over ticket's FinalStatus maps to a workflow stage via the ordered stages list configured through F6. Statuses that don't match any configured stage are grouped into an "Other" bucket.

## User Flows

```
Flow 1: View Multi-Sprint Carry-Over Trend (Landing Section)
1. User navigates to "Sprints" in the sidebar
2. Below the scope change sections (owned by F10), the carry-over sections appear
3. The sprint selector value set by F10 applies — defaults to "Last 5" closed sprints
4. Summary metric cards appear: Average Carry-Over Rate (as percentage), Average Carry-Over SP, Total Zombie Tickets (count of tickets in 3+ sprints across the selected range)
5. A carry-over rate trend line chart shows the carry-over rate per sprint across the selected range
6. A stacked bar chart shows carry-over SP per sprint, segmented by workflow stage — revealing which phases accumulate unfinished work over time
7. Below the charts, an issue type breakdown shows carry-over ticket counts grouped by raw Jira issue type (Story, Bug, Task, Improvement, etc.) totaled across the selected sprints
8. A zombie tickets summary table lists all tickets that appear in 3 or more sprints within the selected range, showing: ticket key, summary, issue type, current status, sprint count
```

```
Flow 2: Single-Sprint Carry-Over Detail
1. User selects a single sprint from the sprint selector (or clicks a sprint bar in the carry-over chart)
2. The carry-over section updates to single-sprint detail mode:
   a. Summary metric cards: Carry-Over Rate, Carry-Over SP, Carry-Over Ticket Count — each with delta vs prior sprint
   b. A status distribution donut chart showing carry-over tickets grouped by workflow stage (count and SP per stage)
   c. An issue type breakdown for this sprint (carry-over tickets by raw issue type)
   d. A carry-over destination section (see Flow 3)
   e. A full carry-over ticket table listing every ticket not completed in this sprint (see below)
   f. A zombie tickets section with trajectory (see Flow 4)
3. The carry-over ticket table shows for each ticket: ticket key, summary, issue type, story points (or "—"), final status, workflow stage, sprint count (how many sprints this ticket has appeared in), and a zombie indicator for tickets in 3+ sprints
4. The table is grouped by workflow stage, with stage headers showing the count and SP subtotal per group
5. Zombie tickets are visually highlighted within the table (distinct background or badge)
```

```
Flow 3: Carry-Over Destination
1. In single-sprint detail mode, a "Prior Sprint Carry-Over" section appears above the ticket table
2. This section answers: "What happened to the tickets that carried over from the previous sprint?"
3. It shows four outcome buckets with counts and SP totals:
   - Completed: tickets that were carry-over in the prior sprint and reached a done status in this sprint
   - Carried again: tickets that were carry-over in the prior sprint and are still not done in this sprint
   - Removed: tickets that were carry-over in the prior sprint and were removed from this sprint
   - Dropped: tickets that were carry-over in the prior sprint but do not appear in this sprint at all (deprioritized or moved elsewhere)
4. A simple horizontal stacked bar visualizes the proportions of these four outcomes
5. When no prior sprint exists (first synced sprint), this section is hidden
6. When the prior sprint had zero carry-over tickets, this section shows "No carry-over from prior sprint"
```

```
Flow 4: Zombie Tickets with Trajectory
1. In single-sprint detail mode, zombie tickets (3+ sprints) appear in a dedicated section below the ticket table
2. Each zombie ticket shows:
   a. Ticket key, summary, issue type, current story points
   b. A trajectory row: an ordered list of sprints the ticket appeared in, each showing the sprint name and the ticket's final status in that sprint
3. The trajectory makes stall patterns visible — e.g., "In Progress → Testing → Testing → Testing" reveals a ticket stuck in testing for 3 sprints
4. Zombie tickets are ordered by sprint count descending (longest-lived first)
5. When no zombie tickets exist for the selected sprint, this section is hidden
```

```
Flow 5: Filter by Sub-Team
1. User selects a sub-team from the sub-team filter in the page toolbar (shared with F10)
2. All carry-over content recalculates scoped to tickets assigned to developers in that sub-team:
   - Metric cards, charts, ticket table, zombie section, and carry-over destination all filter to that sub-team's developers
   - Carry-over rate denominators use only that sub-team's committed + added SP
3. Selecting "All" removes the filter
```

```
Flow 6: Change Sprint Range
1. User changes the sprint selector (shared with F10)
2. Selecting a range (Last 3/5/All) shows multi-sprint carry-over trend (Flow 1)
3. Selecting a single sprint shows single-sprint carry-over detail (Flow 2)
4. All carry-over charts and tables update accordingly
```

## API Surface

| Method | Route | Auth | Request | Response | Status codes |
|--------|-------|------|---------|----------|--------------|
| GET | /api/analytics/carry-over | None | Query params (see below) | Carry-over data | 200, 400 |

**Query parameters:**
- `sprintId` (int, optional) — single sprint detail mode. Mutually exclusive with `last`.
- `last` (int, optional) — last N closed sprints. Mutually exclusive with `sprintId`.
- `subTeam` (string, optional) — filter to developers in this sub-team. Omit or pass empty for all.

**Default behavior:** When neither `sprintId` nor `last` is provided, defaults to `last=5`.

**Response shape — multi-sprint mode (when `last` or default):**

Top level:
- `sprints` — ordered list of sprint summaries (id, name, start date, end date) included in this response
- `summaryMetrics` — aggregate cards: average carry-over rate, average carry-over SP, total zombie ticket count
- `perSprintData` — list of per-sprint carry-over entries (see below)
- `issueTypeBreakdown` — carry-over ticket counts by raw issue type, totaled across all selected sprints
- `zombieTickets` — tickets appearing in 3+ sprints within the selected range

Per-sprint entry:
- Sprint ID
- Carry-over SP (sum of SP on tickets not completed and not removed, excluding tickets with excluded-from-scope final statuses)
- Carry-over ticket count
- Carry-over rate (carry-over SP / (active committed SP + added SP) × 100)
- Total scope SP (active committed SP + added SP — the denominator)
- Status distribution (list of workflow stage entries: stage name, ticket count, SP total)

Zombie ticket entry (multi-sprint):
- Ticket key
- Summary
- Issue type
- Current status
- Story points (nullable)
- Sprint count (number of distinct sprints the ticket appears in)

**Response shape — single-sprint mode (when `sprintId`):**

Top level:
- `sprint` — sprint metadata (id, name, start date, end date)
- `metrics` — metric card objects (carry-over rate, carry-over SP, carry-over ticket count) each with delta vs prior sprint, direction, polarity
- `statusDistribution` — carry-over tickets grouped by workflow stage (stage name, ticket count, SP total, percentage of carry-over)
- `issueTypeBreakdown` — carry-over tickets grouped by raw issue type (type name, ticket count, SP total, percentage)
- `carryOverDestination` — prior sprint carry-over outcomes (see below). Null when no prior sprint exists.
- `tickets` — full list of carry-over tickets (see below)
- `zombieTrajectories` — zombie tickets with sprint-by-sprint history (see below)

Carry-over destination object:
- Prior sprint name and ID
- Prior sprint carry-over count and SP
- Outcomes: completed (count, SP), carried again (count, SP), removed (count, SP), dropped (count, SP)

Carry-over ticket entry:
- Ticket key
- Summary
- Issue type
- Story points (nullable)
- Final status (raw Jira status)
- Workflow stage (mapped stage name, or "Other")
- Sprint count (how many sprints this ticket has appeared in)
- Is zombie (boolean — true when sprint count >= 3)

Zombie trajectory entry:
- Ticket key
- Summary
- Issue type
- Story points (nullable)
- Current status
- Sprint count
- Sprints (ordered list of: sprint ID, sprint name, final status in that sprint)

**Error conditions:**
- 400: Both `sprintId` and `last` provided simultaneously
- 400: `sprintId` does not match any synced closed sprint
- 400: `last` is less than 1

## Business Rules

1. **"Carry-over" means not completed and not removed.** A ticket is carry-over if its FinalStatus is not in the DoneStatuses list AND it was not removed from the sprint (RemovedAt is null). This matches the carry-over definition established in F8.

2. **Carry-over rate = carry-over SP / (active committed SP + added SP) × 100.** The denominator is the total work the team was responsible for — committed SP (after exclusion) plus mid-sprint additions (after exclusion). This matches the carry-over rate formula established in F8.

3. **Excluded-from-scope statuses apply.** Tickets whose FinalStatus matches an excluded-from-scope status (introduced by F10) are excluded from all carry-over metrics — carry-over SP, carry-over rate denominator, status distribution, issue type breakdown. They still appear in the ticket table but are visually marked as excluded.

4. **Status distribution groups by workflow stage.** Each carry-over ticket's FinalStatus maps to a workflow stage from the ordered stages list configured through F6. The mapping matches by checking which stage's statuses include the ticket's FinalStatus. Statuses not found in any stage are grouped under "Other."

5. **"Other" stage appears last.** In the status distribution, configured workflow stages appear in their configured order. The "Other" bucket, if it has any tickets, appears last.

6. **Issue type breakdown uses raw Jira issue types.** No grouping or mapping — "Story", "Bug", "Task", "Improvement", and any custom types appear exactly as they are in Jira. This naturally handles teams with custom work types.

7. **Zombie threshold is 3 sprints.** A ticket appearing in SprintMembership for 3 or more distinct sprints is a zombie. The count includes all synced sprints, not just the selected range or consecutive sprints. This matches the zombie threshold established in F8.

8. **Zombie trajectory shows all sprints the ticket appeared in.** Ordered chronologically by sprint start date. Each entry shows the sprint name and the ticket's FinalStatus in that sprint. The trajectory is capped at the 10 most recent sprints to prevent unbounded growth for extreme cases.

9. **Carry-over destination compares to the immediate prior sprint.** "Prior sprint" is the closed sprint with the next-earlier start date relative to the selected sprint. For each ticket that was carry-over in the prior sprint, check its outcome in the selected sprint:
   - **Completed:** ticket is in the selected sprint and its FinalStatus is in DoneStatuses
   - **Carried again:** ticket is in the selected sprint, FinalStatus not in DoneStatuses, not removed
   - **Removed:** ticket is in the selected sprint but was removed (RemovedAt is not null)
   - **Dropped:** ticket does not appear in the selected sprint's memberships at all

10. **Carry-over destination is single-sprint only.** It compares two specific sprints and is only meaningful in single-sprint detail mode. Not shown in multi-sprint trend mode.

11. **Carry-over destination is hidden when no prior sprint exists.** When the selected sprint is the earliest synced sprint, the carry-over destination section is not shown.

12. **Delta compares to the prior closed sprint.** Single-sprint mode includes delta for each metric card, comparing to the closed sprint with the next-earlier start date. When no prior sprint exists, delta is null. This follows the delta pattern established in F8.

13. **Delta polarity: lower carry-over is better.** Carry-over rate and carry-over SP trending down is green (positive). Trending up is red (negative). Carry-over ticket count follows the same polarity.

14. **Multi-sprint mode shows no deltas, uses averages.** Summary metrics in multi-sprint mode show averages (carry-over rate, carry-over SP) or totals (zombie count) across the selected sprints. The trend chart serves the purpose of showing direction over time.

15. **Sub-team filter scopes everything.** When a sub-team is selected, all carry-over computations restrict to tickets assigned to developers in that sub-team. Carry-over SP, carry-over rate denominator, status distribution, issue type breakdown, ticket table, zombie detection, and carry-over destination all scope to that sub-team.

16. **Tickets with no story points are excluded from SP metrics.** They count in ticket-based metrics (carry-over ticket count, issue type breakdown counts, zombie sprint count) but are absent from SP calculations (carry-over SP, carry-over rate, status distribution SP totals).

17. **Division by zero produces zero.** When (committed SP + added SP) is 0, carry-over rate is 0. When no carry-over tickets exist, all sections show appropriate empty states.

18. **Carry-over sections extend the Sprints page.** F11 adds content below F10's scope change sections on the same scrollable page. The carry-over sections share F10's sprint selector and sub-team filter — no duplicate controls.

19. **Carry-over ticket table shows all carry-over tickets.** Not just zombies. The table lists every ticket that didn't complete in the sprint, grouped by workflow stage with count and SP subtotals per group. Zombie tickets are visually highlighted with a badge or distinct background. No pagination — carry-over counts are typically small enough to show in full.

20. **Workflow stages not configured is handled gracefully.** When F6 has not been run and no workflow stages exist in settings, the status distribution groups all tickets under "Other" and the donut chart shows a single segment. The ticket table omits the workflow stage column and does not group.

## Acceptance Criteria

- [ ] Carry-over sections appear on the Sprints page below the scope change sections (F10)
- [ ] Multi-sprint trend view shows when a sprint range is selected (Last 3/5/All)
- [ ] Single-sprint detail view shows when a single sprint is selected
- [ ] Summary metric cards display: Average Carry-Over Rate, Average Carry-Over SP, Total Zombie Tickets (multi-sprint mode)
- [ ] Carry-over rate trend line chart shows carry-over rate per sprint across the selected range
- [ ] Stacked bar chart shows carry-over SP per sprint segmented by workflow stage
- [ ] Issue type breakdown shows carry-over ticket counts by raw Jira issue type
- [ ] Zombie tickets summary table lists tickets in 3+ sprints with: key, summary, issue type, current status, sprint count
- [ ] Single-sprint metric cards show: Carry-Over Rate, Carry-Over SP, Carry-Over Ticket Count with delta vs prior sprint
- [ ] Delta polarity: lower carry-over is green, higher is red
- [ ] Status distribution donut chart groups carry-over tickets by workflow stage
- [ ] Statuses not matching any configured workflow stage appear under "Other"
- [ ] "Other" stage appears last in all groupings
- [ ] Carry-over destination section shows outcomes for prior sprint's carry-over tickets: completed, carried again, removed, dropped
- [ ] Carry-over destination shows counts and SP totals per outcome bucket
- [ ] Carry-over destination horizontal stacked bar visualizes outcome proportions
- [ ] Carry-over destination is hidden when no prior sprint exists
- [ ] Carry-over destination shows "No carry-over from prior sprint" when prior sprint had zero carry-over
- [ ] Full carry-over ticket table lists every non-completed ticket: key, summary, issue type, SP, final status, workflow stage, sprint count, zombie indicator
- [ ] Ticket table is grouped by workflow stage with count and SP subtotals per group header
- [ ] Zombie tickets are visually highlighted in the ticket table (badge or distinct background)
- [ ] Zombie trajectory section shows sprint-by-sprint history for each zombie ticket
- [ ] Each trajectory entry shows sprint name and final status in that sprint
- [ ] Trajectory is capped at 10 most recent sprints per ticket
- [ ] Zombie tickets are ordered by sprint count descending
- [ ] Zombie section is hidden when no zombie tickets exist for the selected sprint
- [ ] Sub-team filter restricts all carry-over content to the selected sub-team's developers
- [ ] Sprint selector changes update all carry-over content (shared with F10)
- [ ] GET /api/analytics/carry-over with no params defaults to last 5 closed sprints
- [ ] GET /api/analytics/carry-over with `sprintId` returns single-sprint carry-over detail
- [ ] GET /api/analytics/carry-over with `last=N` returns multi-sprint carry-over trend
- [ ] 400 returned when both `sprintId` and `last` are provided
- [ ] 400 returned when `sprintId` does not match a synced closed sprint
- [ ] Excluded-from-scope statuses are respected in all carry-over metrics
- [ ] Excluded tickets still appear in the ticket table but are visually marked as excluded
- [ ] Tickets with no story points are excluded from SP metrics but included in ticket counts
- [ ] Division by zero cases produce 0 or hide the section respectively
- [ ] Graceful fallback when no workflow stages are configured (all tickets grouped under "Other")
- [ ] Carry-over rate formula matches F8: carry-over SP / (committed SP + added SP) × 100

## Out of Scope

- **Root cause analysis** — no automated diagnosis of WHY tickets carry over (stuck on dependencies, wrong assignment, underestimated). Root cause analysis is a retrospective exercise, not a dashboard feature.
- **Per-developer carry-over attribution** — F9 (Developer Throughput) already shows tickets carried over per developer. This feature analyzes carry-over at the sprint level, not per individual.
- **Configurable zombie threshold** — fixed at 3 sprints, matching F8. Making it configurable adds a setting with low demand in v1.
- **Carry-over prediction** — no Monte Carlo forecasting or "will this ticket carry over again?" scoring. Deferred to v2.
- **Aging WIP chart** — Kanban-style percentile-based aging visualization. Fokus uses sprint-count-based zombie detection instead, which is more natural for Scrum teams.
- **Carry-over destination beyond immediate prior sprint** — destination tracking compares sprint N to sprint N-1 only. Tracing a ticket's full journey across 5+ sprints is covered by the zombie trajectory, not the destination metric.
- **Manual carry-over tagging** — no UI for marking tickets as "intentional carry-over" vs "unintentional." All carry-over is treated equally.
- **Export (PDF/PNG)** — deferred to v2 per the product spec.
- **Real-time updates** — no SignalR push when sync completes. User refreshes after syncing.
