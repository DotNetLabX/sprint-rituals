# Epic Progress

**Traces to:** `docs/product/v1.md` §5.6 (Epic Progress), §6.1 (Navigation — Epics page)
**Source:** Scratch
**Dependencies:** F2 (Domain Model & Persistence), F3 (Settings System), F5 (Sprint Sync), F7 (App Shell & Navigation), F8 (Sprint Summary Card — cross-cutting patterns)
**Status:** Done
**Plan:** `docs/plans/EpicProgress/plan.md`

---

## Purpose

Scrum Masters need to answer "how's this epic actually going?" with data, not gut feel. Jira shows per-ticket status but no cross-sprint epic velocity, projected completion, or progress visibility across the full epic scope. The Sprint Summary Card (F8) surfaces a lightweight "Top 3 epics progressed" per sprint — this feature provides the full analytical depth: total scope (including backlog tickets not yet in a sprint), dual completion tracking (by ticket count and by story points), epic-level velocity, projected sprints to completion, and per-ticket drill-down. It owns the Epics page in the sidebar navigation.

## Entities

This feature introduces no new domain entities or settings. It queries existing data using two complementary data sources.

**Ticket-level data (cumulative epic state):**
- Tickets with an epic key represent the full scope of that epic — including backlog tickets discovered via sync that have never appeared in a sprint
- Each ticket's current status determines whether it is done (status in the configured done statuses list) or remaining
- Story points on the ticket represent the current estimate

**Sprint membership data (velocity and sprint activity):**
- Sprint memberships for tickets belonging to an epic track which sprints the epic was active in and how much SP was completed per sprint
- Only sprint membership data can answer "how much did this epic progress in sprint X?" — ticket current status only reflects the latest state

**Imputed story points for unestimated tickets:**
- Within each epic, tickets without story points receive an imputed estimate equal to the average story points of estimated tickets in the same epic
- Imputed SP are used in total scope, remaining SP, progress percentage, and projection calculations
- Imputed SP are never used in velocity calculations — velocity only counts actual completed SP
- Imputed values are clearly labeled wherever they appear (distinct visual treatment, separate from real SP totals)
- When an epic has zero estimated tickets, SP metrics are unavailable — only ticket-count progress is shown

## User Flows

```
Flow 1: View Epic Progress (Landing)
1. User navigates to "Epics" in the sidebar
2. Three summary cards appear at the top: Active Epics (count), Average Completion (percentage across active epics by SP), Unlinked Work (ticket count and SP total for tickets with no epic)
3. An active/completed toggle defaults to "Active" — showing epics with less than 100% ticket completion
4. The epic table shows one row per epic, sorted by completion percentage ascending (least-done first):
   - Epic name
   - Progress bar with completion percentage (by SP, including imputed SP shown as a distinct segment)
   - SP done / SP total (with imputed portion labeled separately, e.g., "+~12 SP est.")
   - Tickets done / tickets total
   - Velocity (SP completed per sprint, rolling 3-sprint average of sprints where this epic had progress)
   - Projected sprints remaining (remaining SP including imputed / velocity)
5. If no epics exist in the system, the page shows the empty state ("No epic data yet — sync a sprint to see epic progress here")
```

```
Flow 2: Expand Epic Detail
1. User clicks the expand control on an epic row
2. A ticket table appears below the row showing all tickets belonging to that epic:
   - Ticket key, summary, issue type, story points (or "—" for unestimated), current status, assignee display name
3. Tickets are sorted by status: remaining tickets first (grouped by current status), then done tickets
4. Unestimated tickets show "—" in the story points column — no imputed value is shown at the ticket level
5. Clicking the expand control again collapses the ticket table
```

```
Flow 3: Toggle Completed Epics
1. User clicks the "Completed" toggle
2. The table switches to showing epics at 100% ticket completion
3. Completed epics are sorted by epic name alphabetically
4. All summary cards update to reflect completed epics (count shows completed epic count, average completion is 100%)
5. Clicking "Active" switches back to the default view
```

```
Flow 4: Filter by Sub-Team
1. User selects a sub-team from the sub-team filter in the page toolbar
2. All epic content recalculates scoped to tickets assigned to developers in that sub-team:
   - Epic progress shows only tickets assigned to developers in the selected sub-team
   - Velocity uses only SP completed by that sub-team's developers
   - Projection uses the sub-team-scoped velocity and remaining SP
   - Epics with zero tickets assigned to that sub-team disappear from the table
   - Summary cards recalculate for the filtered view
   - Unlinked work counts only tickets assigned to the sub-team's developers
3. Selecting "All" removes the filter
```

```
Flow 5: Empty and Edge States
1. No epics exist: page shows the empty state defined by F7
2. All epics completed and "Active" toggle selected: "All epics are complete" message with a prompt to switch to the Completed view
3. No completed epics and "Completed" toggle selected: "No completed epics yet" message
4. Epic with all tickets unestimated: progress bar shows ticket-count percentage only, SP columns show "—", velocity shows "—", projection shows "Insufficient data"
5. Epic with fewer than 3 sprints of progress data: velocity uses available data points (1 or 2 sprints), projection labeled "Low confidence" alongside the number
```

## API Surface

| Method | Route | Auth | Request | Response | Status codes |
|--------|-------|------|---------|----------|--------------|
| GET | /api/analytics/epic-progress | None | Query params (see below) | Epic progress data | 200 |

**Query parameters:**
- `subTeam` (string, optional) — filter to tickets assigned to developers in this sub-team. Omit or pass empty for all.

**Response shape:**

Top level:
- `summaryMetrics` — summary card data (see below)
- `epics` — list of epic progress entries (see below)
- `unlinkedWork` — unlinked work summary (see below)

Summary metrics object:
- Active epic count (int)
- Completed epic count (int)
- Average completion percentage across active epics (decimal, weighted by adjusted total SP — larger epics contribute proportionally more)

Epic progress entry:
- Epic key (string — the Jira epic issue key)
- Epic name (string)
- Total tickets (int)
- Done tickets (int)
- Remaining tickets (int)
- Ticket completion percentage (decimal)
- Total SP (decimal — sum of actual SP on estimated tickets)
- Done SP (decimal — actual SP on completed tickets)
- Remaining SP (decimal — actual SP on remaining estimated tickets)
- Imputed SP (decimal — imputed estimate for unestimated remaining tickets, null when no estimated tickets exist in the epic)
- Adjusted total SP (decimal — total SP + imputed SP, null when imputation unavailable)
- SP completion percentage (decimal — done SP / adjusted total SP, null when imputation unavailable and no estimated tickets)
- Unestimated ticket count (int — tickets with no story points)
- Velocity (decimal, nullable — rolling 3-sprint average of SP completed in sprints where this epic had progress. Null when no sprint progress data exists)
- Projected sprints remaining (decimal, nullable — (remaining SP + imputed SP) / velocity. Null when velocity is null or zero)
- Projection confidence (string, nullable — "low" when fewer than 3 data points, null otherwise)
- Active sprint count (int — distinct sprints where this epic had at least one ticket)
- Is completed (boolean — true when all tickets are in a done status)
- Tickets (list of epic ticket entries — see below)

Epic ticket entry:
- Ticket key (string)
- Summary (string)
- Issue type (string)
- Story points (decimal, nullable)
- Current status (string)
- Assignee display name (string, nullable)
- Is done (boolean)

Unlinked work object:
- Ticket count (int — tickets with no epic key that appear in at least one sprint membership)
- Total SP (decimal — sum of SP on those tickets)

## Business Rules

1. **Epic scope includes all tickets with a matching epic key.** Every ticket in the system with that epic key counts toward the epic's totals — whether it has appeared in a synced sprint or not. Backlog-discovered tickets (from the backlog sync endpoint) are included.

2. **"Done" uses the configured done statuses list.** A ticket is done if its current status matches any value in the app settings done statuses list. This is the same definition used by F8 and all other analytics features.

3. **Dual completion tracking.** Each epic shows completion by ticket count (done tickets / total tickets) and by story points (done SP / adjusted total SP). Both are shown because they tell different stories — an epic can be 90% done by ticket count but 60% done by SP if the remaining tickets are large.

4. **Imputed story points fill the estimation gap.** For each epic, unestimated remaining tickets receive an imputed SP value equal to the average SP of estimated tickets within that same epic. Imputed SP increases the total scope denominator and the remaining SP, making progress percentages and projections more realistic. Imputed SP is always presented distinctly from actual SP.

5. **Imputation is per-epic, not global.** Each epic computes its own average from its own estimated tickets. Different epics have different complexity profiles — a global average would misrepresent scope.

6. **Imputation applies only to remaining tickets.** Done tickets without SP are not imputed — they contributed their work already and including them would inflate the "done" total. Only remaining unestimated tickets receive imputed values.

7. **Velocity uses actual SP only.** Epic velocity is the average SP completed per sprint, computed from sprint membership data (tickets with a done final status in that sprint). Only sprints where the epic had at least 1 SP completed count as data points. Imputed SP is never included in velocity calculations.

8. **Velocity skips zero-progress sprints.** If an epic had no SP completed in a sprint, that sprint is not a data point for velocity. This avoids diluting velocity with sprints where the team worked on other epics. The rolling average uses the 3 most recent sprints with progress.

9. **Projection = remaining work / velocity.** Projected sprints remaining is (remaining SP + imputed SP) / velocity. This gives the most realistic estimate by accounting for unestimated work in the numerator and using only real delivery data in the denominator.

10. **Projection confidence reflects data quality.** When fewer than 3 sprints of velocity data exist, the projection is labeled "low confidence." When no velocity data exists (no sprint has completed SP for this epic), projection is unavailable.

11. **Active/completed is derived, not stored.** An epic is "completed" when 100% of its tickets have a done current status. There is no separate epic entity or workflow state — completion is computed from ticket data. An epic can flip back to active if a ticket's status changes (e.g., reopened after sync).

12. **Default sort is completion percentage ascending.** Active epics are sorted by SP completion percentage, least-done first — the ones that need attention surface at the top. Completed epics are sorted alphabetically by name.

13. **Sub-team filter scopes everything.** When a sub-team is selected, all computations restrict to tickets assigned to developers in that sub-team: ticket counts, SP totals, imputation averages, velocity, projection, unlinked work, and summary metrics. Epics with zero tickets assigned to the filtered sub-team are excluded from the response.

14. **Unlinked work counts tickets without an epic key.** Only tickets that appear in at least one sprint membership are counted — pure backlog tickets without sprint history are excluded from the unlinked count, since they may not be relevant work items.

15. **Tickets with no story points are excluded from SP metrics but included in ticket counts.** Unestimated tickets count toward ticket totals and ticket completion percentage. They are absent from actual SP sums but receive imputed values for adjusted totals (when imputation is available).

16. **Velocity data comes from sprint memberships.** A ticket "completed SP in sprint X" means it has a sprint membership for sprint X where the final status is in the done statuses list and the membership has story points. The ticket's current status is irrelevant for velocity — what matters is what was done when.

17. **Active sprint count uses all sprint memberships.** The count of distinct sprints where the epic had at least one ticket in a sprint membership (regardless of outcome). This shows how long the epic has been worked on.

18. **No sprint selector on the Epics page.** Epic progress is cumulative across all time. The sprint selector in the page toolbar is suppressed on this page, matching how the Dashboard suppresses aggregate sprint options. Only the sub-team filter is active.

19. **Division by zero produces zero or null.** When adjusted total SP is 0, SP completion percentage is 0. When velocity is 0 or null, projection is null. When an epic has zero estimated tickets, SP-based metrics are null.

20. **F8 Top Epics must be aligned as part of F14.** The Sprint Summary Card's "Top 3 epics" currently computes completion from sprint membership data only — it counts SP completed within sprints and total SP across sprint memberships. This page computes completion from all tickets (including backlog-discovered ones) using current ticket status. The numbers would differ because F14 sees a larger scope. F14's implementation must update F8's Top Epics calculation to use the same cumulative ticket-status-based approach so numbers are consistent across pages.

21. **Excluded-from-scope statuses do not apply.** The excluded-from-scope statuses introduced by F10 are a sprint-level concept — they exclude tickets from sprint scope metrics (committed SP, disruption rate, carry-over rate). Epic progress is cumulative and status-independent: a ticket either has a done status or it doesn't. Excluded-from-scope statuses have no effect on epic progress calculations.

22. **Epic name falls back to epic key.** When a ticket's epic name is null but its epic key is present, the epic key is used as the display name. This matches the existing pattern in F8's Top Epics computation.

23. **Unassigned tickets show "Unassigned" in the assignee column.** When a ticket has no assignee, the expanded ticket table displays "Unassigned" rather than a blank cell.

24. **Velocity computation groups by current epic key.** Sprint membership records are joined to their parent tickets and grouped by the ticket's current epic key. If a ticket moves between epics in Jira and is re-synced, its velocity contribution shifts to the new epic. Historical epic assignment is not tracked.

25. **Epic progress accuracy depends on backlog sync.** Without running the backlog sync, only tickets that appeared in synced sprints are visible. Backlog-discovered tickets (those belonging to referenced epics but not yet in any sprint) are only available after a backlog sync has been performed. The Epics page works without backlog sync but shows a narrower scope.

## Acceptance Criteria

- [ ] Epics page shows three summary cards: Active Epics count, Average Completion %, Unlinked Work (ticket count and SP)
- [ ] Active/Completed toggle defaults to Active, showing epics below 100% ticket completion
- [ ] Switching to Completed shows only 100%-complete epics sorted alphabetically
- [ ] Epic table shows: epic name, progress bar, SP done/total (with imputed portion labeled), tickets done/total, velocity, projected remaining
- [ ] Epics are sorted by SP completion percentage ascending (least-done first) in Active view
- [ ] Progress bar shows actual SP as a filled segment and imputed SP as a visually distinct segment
- [ ] Imputed SP label shows the imputed portion separately (e.g., "+~12 SP est.")
- [ ] Imputed SP is computed per-epic as average of estimated tickets in that epic
- [ ] Imputed SP applies only to remaining unestimated tickets, not done unestimated tickets
- [ ] Epic with zero estimated tickets shows ticket-count progress only; SP metrics show "—"
- [ ] Expanding an epic row shows all tickets: key, summary, issue type, SP, current status, assignee
- [ ] Expanded ticket table sorts remaining tickets first, then done tickets
- [ ] Unestimated tickets show "—" in the SP column at ticket level (no imputed value shown)
- [ ] Collapsing a previously expanded row hides the ticket table
- [ ] Velocity shows the rolling 3-sprint average of SP completed in sprints where this epic had progress
- [ ] Velocity skips sprints where the epic had zero SP completed
- [ ] Velocity shows "—" when no sprint progress data exists for the epic
- [ ] Projected sprints remaining = (remaining SP + imputed SP) / velocity
- [ ] Projection shows "Insufficient data" when velocity is unavailable
- [ ] Projection shows "Low confidence" label when fewer than 3 sprint data points exist
- [ ] Active sprint count shows the number of distinct sprints the epic has had tickets in
- [ ] Sub-team filter restricts all content to tickets assigned to developers in the selected sub-team
- [ ] Sub-team filter scopes imputation averages to the filtered tickets
- [ ] Epics with zero tickets for the selected sub-team are excluded
- [ ] Unlinked work counts tickets with no epic key that appear in at least one sprint
- [ ] Unlinked work shows both ticket count and total SP
- [ ] Sub-team filter scopes unlinked work to the selected sub-team's developers
- [ ] Summary cards update when sub-team filter changes
- [ ] Summary cards update when active/completed toggle changes
- [ ] No sprint selector appears on the Epics page
- [ ] Page shows empty state when no epics exist in the system
- [ ] Active view shows "All epics are complete" message when all epics are done
- [ ] Completed view shows "No completed epics yet" message when none are complete
- [ ] GET /api/analytics/epic-progress returns epic progress data for all epics
- [ ] GET /api/analytics/epic-progress with subTeam parameter filters to that sub-team
- [ ] Response includes summary metrics, epic list with tickets, and unlinked work
- [ ] F8 Top Epics completion percentages match F14's cumulative ticket-status-based approach after F14 ships
- [ ] Average Completion summary card uses a weighted average (by adjusted total SP), not arithmetic mean
- [ ] Epic name falls back to epic key when epic name is null
- [ ] Unassigned tickets show "Unassigned" in the assignee column of the expanded ticket table

## Out of Scope

- **Epic burndown/burnup chart** — the table with velocity and projection provides the key insight (when will this be done?). A per-epic burndown chart adds visual complexity without new information in v1. Deferred to v2.
- **Epic workflow states as a stored concept** — completed/active is derived from ticket data, not a separate epic entity. Adding a managed epic lifecycle (To Do / In Progress / Done) requires a new aggregate and manual state management. Not worth the overhead when the derived approach works.
- **Per-sprint epic breakdown** — this page shows cumulative progress. Per-sprint epic contributions are visible in the Sprint Summary Card (F8 — top 3 epics). A dedicated per-sprint epic view is deferred.
- **Epic-level story point editing** — epic SP totals come from child tickets. No UI for overriding an epic's total scope at the epic level.
- **Epic grouping or hierarchy** — no parent-epic / sub-epic nesting. Flat list of epics only.
- **Configurable velocity window** — fixed at 3 sprints. Making the rolling average window configurable adds a setting with low demand in v1.
- **Export (PDF/PNG)** — deferred to v2 per the product spec.
- **Real-time updates** — no SignalR push when sync completes. User refreshes after syncing.
- **Column sorting in the epic table** — fixed sort order (completion % ascending for active, alphabetical for completed). Column-header sorting deferred to v2.
- **Search/filter within epics** — no text search for epic names in v1. The list is typically short enough (5-15 active epics) to scan visually.
- **C1 (Delta Pattern)** — epic progress is cumulative across all sprints with no sprint selector. Without a sprint selection mechanism, there is no "prior sprint" to delta against. Velocity and projection inherently reflect trends over time.
- **C3 (Multi-Sprint Selection)** — epic progress is cumulative by nature (BR18). Sprint-scoped epic analysis is partially available via F8 Top Epics. A per-sprint epic breakdown view is deferred.
