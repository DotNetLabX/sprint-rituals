# Sprint Summary Card

**Traces to:** `docs/product/v1.md` §5.7 (Sprint Summary Card), §6.1 (Navigation), §6.2 (Design Direction)
**Source:** Scratch
**Dependencies:** F2 (Domain Model & Persistence), F3 (Settings System), F5 (Sprint Sync), F7 (App Shell & Navigation)
**Status:** Done
**Plan:** `docs/plans/SprintSummaryCard/plan.md`

---

## Purpose

The Dashboard landing page — "give me the whole sprint in 30 seconds." A single-screen overview per sprint designed for retrospectives and stakeholder updates. This is the first analytics feature and establishes three cross-cutting patterns that all subsequent analytics features (F9-F14) inherit: the health score computation, the delta pattern (C1), and the sub-team filter data-wiring (C2). It also activates the sprint selector that F7 left as a visual stub.

## Entities

This feature introduces no new domain entities. It queries existing data (sprints, sprint memberships, tickets, developers, app settings) and computes all metrics on the fly.

It extends app settings with one new concept:

**Composite health score RAG thresholds**
- Green: composite score >= 75
- Amber: composite score 40-74
- Red: composite score < 40
- These are fixed in v1 (not user-configurable). The per-metric thresholds and weights that feed the composite are already user-configurable in app settings.

## User Flows

```
Flow 1: View Sprint Summary
1. User opens the app (or clicks Dashboard in the sidebar)
2. The sprint selector in the page toolbar populates with closed sprints, most recent selected by default
3. The page displays the summary card for the selected sprint:
   - Sprint name, dates, and duration at the top
   - A composite health score badge (0-100 number with green/amber/red color)
   - Four metric cards in a row: SP Completed/Committed, Completion %, Disruption Rate, Carry-Over Rate
   - Each metric card shows: big number, delta vs prior sprint (arrow + signed number, colored by polarity), 4-sprint sparkline
   - Top 3 epics progressed (if any epics had SP completed)
   - Developer leaderboard (all active developers, sorted by SP completed)
   - Flags section (zombie tickets, mid-sprint disruption, zero-SP developers)
4. If no closed sprints exist, the page shows the empty state defined by F7
```

```
Flow 2: Switch Sprint
1. User clicks the sprint selector dropdown
2. Dropdown shows all closed sprints, ordered by start date descending (most recent first)
3. User selects a different sprint
4. All content on the page updates: health score, metric cards (including deltas and sparklines), epics, leaderboard, flags
5. The URL updates to reflect the selected sprint
```

```
Flow 3: Filter by Sub-Team
1. User clicks the sub-team filter dropdown in the page toolbar
2. Dropdown shows "All" (default) plus each distinct sub-team name from the developer roster
3. User selects a sub-team
4. All content on the page recalculates scoped to developers in that sub-team:
   - Metric cards show metrics for only that sub-team's developers' tickets
   - Health score recomputes from the filtered metrics
   - Leaderboard shows only developers in that sub-team
   - Top epics reflect only SP completed by that sub-team's developers
   - Flags scope to that sub-team's developers and their tickets
5. Selecting "All" removes the filter
```

```
Flow 4: View Flags
1. The flags section appears below the leaderboard
2. Each flag type that fires shows its content:
   - Zombie tickets: ticket key, summary, and sprint count for each ticket in 3+ sprints
   - Mid-sprint disruption: total SP and ticket count added after the second day of the sprint
   - Zero-SP developers: display names of active developers who completed 0 SP
3. If no flags fire, the section shows a positive message ("No flags this sprint")
```

## API Surface

| Method | Route | Auth | Request | Response | Status codes |
|--------|-------|------|---------|----------|--------------|
| GET | /api/analytics/sprint-summary | None | Query params (see below) | Sprint summary data | 200, 400 |
| GET | /api/sprints/closed | None | -- | List of closed sprints | 200 |
| GET | /api/developers/sub-teams | None | -- | List of distinct sub-team names | 200 |

**Sprint summary query parameters:**
- `sprintId` (int, optional) -- specific sprint. Defaults to the most recent closed sprint when omitted.
- `subTeam` (string, optional) -- filter to developers in this sub-team. Omit or pass empty for all.

**Sprint summary response shape:**

Top level:
- `sprint` -- sprint metadata (id, name, start date, end date, duration in days, synced-at timestamp). Null when no closed sprints exist.
- `healthScore` -- composite health score object (see below). Null when sprint is null.
- `metrics` -- four metric card objects (see below). Null when sprint is null.
- `topEpics` -- list of epic progress objects, max 3. Empty list when no epics had progress.
- `leaderboard` -- list of developer summary objects, sorted by SP completed descending. Empty list when no developers.
- `flags` -- flags object (see below).

Health score object:
- Composite score (0-100, decimal)
- Composite RAG status (green, amber, red)
- Per-metric sub-scores: completion sub-score, disruption sub-score, carry-over sub-score (each 0-100)
- Per-metric RAG status (each green, amber, red)

Metric card object (one per metric):
- Metric name
- Value (decimal)
- Display value (formatted string -- e.g., "24 / 30 SP", "80.0%", "16.7%")
- Delta vs prior sprint (decimal, nullable -- null when no prior sprint exists)
- Delta direction (up, down, flat, or null)
- Delta polarity (positive, negative, neutral) -- determines color
- Sparkline data points (list of up to 4 entries, each with sprint name and value)

Epic progress object:
- Epic name
- SP completed this sprint
- Total SP across all sprints (done + remaining)
- Done SP across all sprints
- Completion percentage

Developer summary object:
- Display name
- Avatar URL (nullable)
- Sub-team (nullable)
- SP completed

Flags object:
- Zombie tickets: list of (ticket key, summary, sprint count) for tickets in 3+ sprints
- Mid-sprint disruption: total SP added after day 2, ticket count. Null when no mid-sprint additions.
- Zero-SP developers: list of display names of active developers with 0 SP completed
- Has any flags (boolean convenience field)

**Closed sprints endpoint response:**
- List of (id, name, start date, end date, state) ordered by start date descending

**Sub-teams endpoint response:**
- List of distinct non-null sub-team name strings, sorted alphabetically

**Error conditions:**
- 400: `sprintId` provided but does not match any synced closed sprint
- 200 with null sprint: no closed sprints exist (not an error)

## Business Rules

1. **Health score is a weighted composite of three sub-metrics.** Each sub-metric (completion rate, disruption rate, carry-over rate) maps to a 0-100 sub-score using the configurable threshold bands. The composite is their weighted average using the configurable weights (default: completion 40%, disruption 30%, carry-over 30%).

2. **Sub-score interpolation uses threshold bands.** For completion (higher is better): at or above the green threshold scores 100; between amber and green interpolates linearly from 50 to 99; below amber interpolates linearly from 0 to 49. For disruption and carry-over (lower is better): at or below the green threshold scores 100; between green and amber interpolates linearly from 99 to 50; above amber interpolates linearly from 49 to 0, bottoming out at twice the amber threshold.

3. **Composite RAG thresholds are fixed.** Green >= 75, Amber 40-74, Red < 40. These are not user-configurable in v1.

4. **Per-metric RAG uses the configurable thresholds.** Each metric card shows its own RAG color based on the per-metric thresholds in app settings — independent of the composite RAG.

5. **"Completed" uses the done statuses list.** A ticket is completed if its final status in the sprint matches any value in the app settings done statuses list.

6. **"Committed" means present at sprint start.** Committed SP = sum of story points on memberships where the ticket was in the sprint at or before the sprint start date.

7. **"Added" means mid-sprint, not removed.** Added SP = sum of story points on memberships added after sprint start where the ticket was not subsequently removed. Disruption rate = added SP / committed SP x 100.

8. **Completion rate measures commitment delivery.** Completion % = SP completed / SP committed x 100. The denominator is committed SP (not assigned SP) because the summary card measures how well the team delivered against its sprint commitment.

9. **"Carry-over" is everything not done.** Carry-over SP = sum of story points on non-removed memberships whose final status is not in the done statuses list.

10. **Carry-over rate denominator includes both committed and added.** Carry-over rate = carry-over SP / (committed SP + added SP) x 100. This reflects the total work the team was responsible for, not just what they started with.

11. **Delta compares to the prior closed sprint.** "Prior" is the closed sprint with the next-earlier start date relative to the selected sprint. When no prior sprint exists, delta is null (not zero).

12. **Delta polarity is metric-specific.** Completion % and SP completed/committed: higher is better (green up, red down). Disruption rate and carry-over rate: lower is better (green down, red up). SP completed/committed is neutral (no color) because more committed SP is neither inherently good nor bad.

13. **Sparkline is a fixed 4-sprint trailing window.** Always shows the 4 most recent closed sprints ending at and including the selected sprint — the window shifts backward when an older sprint is selected, always ending at the selected sprint. If fewer than 4 closed sprints exist, show what's available.

14. **Top epics are ranked by SP completed in the selected sprint.** Only epics with at least 1 SP completed in the sprint appear. Take the top 3. Overall progress (done/total SP) is computed across all sprints, not just the selected one.

15. **Developer leaderboard shows all active developers.** Sorted by SP completed descending. Inactive developers are excluded. Developers with 0 SP completed still appear (sorted to the bottom). No truncation or cap. The leaderboard is for visibility, not gamification — it helps identify who might need support, not who "won" the sprint.

16. **Zombie ticket threshold is 3 sprints.** A ticket that appears in sprint memberships for 3 or more distinct sprints is flagged as a zombie. The count includes all sprints (including the selected one), not just consecutive sprints.

17. **Mid-sprint disruption uses a 2-day grace period.** Tickets added to the sprint more than 2 days after the sprint start date (and not removed) are flagged as mid-sprint disruption. Day 1-2 additions are considered planning overflow, not disruption. The flag reports total SP and ticket count.

18. **Zero-SP developers are active developers only.** Any active developer assigned to the sprint with 0 SP completed is flagged. Developers not assigned to the sprint at all are not flagged — they would appear in the leaderboard at 0 but are not a "flag" unless they had tickets.

19. **Sub-team filter scopes everything.** When a sub-team is selected, all computations (metrics, health score, leaderboard, epics, flags) are restricted to tickets assigned to developers in that sub-team.

20. **Dashboard is single-sprint only.** The sprint selector on Dashboard shows individual closed sprints. The sprint selector toolbar built by F7 includes visual stubs for aggregate options (Last 3, Last 5, All) — on Dashboard, these are suppressed. Other analytics pages (F9-F14) will enable aggregate options when they are built.

21. **Tickets with no story points are excluded from SP metrics.** They do not count as 0 SP. They are simply absent from committed SP, completed SP, added SP, carry-over SP, and all rate calculations. They still count toward ticket-based flags (zombie tickets count by membership, not SP).

22. **Division by zero produces zero.** When committed SP is 0, completion % is 0 and disruption rate is 0. When committed + added SP is 0, carry-over rate is 0.

## Acceptance Criteria

- [ ] Opening the app navigates to Dashboard with the most recent closed sprint selected
- [ ] Sprint selector dropdown lists all closed sprints ordered by start date descending
- [ ] Selecting a different sprint updates all page content
- [ ] Health score displays as a 0-100 number with green/amber/red color
- [ ] Health score composite uses the weighted formula with configurable thresholds and weights
- [ ] Health score composite RAG: green >= 75, amber 40-74, red < 40
- [ ] Four metric cards display: SP Completed/Committed, Completion %, Disruption Rate, Carry-Over Rate
- [ ] Each metric card shows a big number, delta indicator, and sparkline
- [ ] Delta compares to the prior closed sprint (by start date)
- [ ] Delta is absent when no prior closed sprint exists
- [ ] Delta polarity: completion % higher=green; disruption/carry-over lower=green; SP completed/committed neutral
- [ ] Sparklines show a fixed 4-sprint trailing window ending at the selected sprint
- [ ] Sparklines show fewer points when fewer than 4 sprints exist
- [ ] Top 3 epics section shows epics with SP completed in the sprint, sorted by SP completed
- [ ] Each epic entry shows: name, SP completed this sprint, overall progress (done/total SP with percentage)
- [ ] Top epics section is hidden when no epics had SP completed
- [ ] Developer leaderboard shows all active developers sorted by SP completed descending
- [ ] Leaderboard entries show: avatar, display name, sub-team, SP completed
- [ ] Inactive developers are excluded from the leaderboard
- [ ] Zombie tickets flag fires for tickets in 3+ sprints, showing ticket key, summary, and sprint count
- [ ] Mid-sprint disruption flag fires when SP were added after day 2, showing total SP and ticket count
- [ ] Zero-SP developers flag fires for active developers with 0 SP completed who had tickets assigned
- [ ] Flags section shows "No flags this sprint" when no flags fire
- [ ] Sub-team filter dropdown shows "All" plus distinct sub-team names
- [ ] Selecting a sub-team filters all page content to that sub-team's developers
- [ ] Sub-team filter scopes metric cards, health score, leaderboard, epics, and flags
- [ ] Dashboard sprint selector does not offer "Last 3", "Last 5", or "All" aggregate options
- [ ] GET /api/analytics/sprint-summary with no params returns data for the most recent closed sprint
- [ ] GET /api/analytics/sprint-summary with valid sprintId returns data for that sprint
- [ ] GET /api/analytics/sprint-summary returns 400 when sprintId does not match a closed sprint
- [ ] GET /api/analytics/sprint-summary returns 200 with null sprint data when no closed sprints exist
- [ ] GET /api/analytics/sprint-summary with subTeam parameter filters all metrics to that sub-team
- [ ] GET /api/sprints/closed returns closed sprints ordered by start date descending
- [ ] GET /api/developers/sub-teams returns distinct non-null sub-team names sorted alphabetically

## Out of Scope

- **Animated page transitions between sprints** -- standard data refresh on selection change. Smooth chart animations via ApexCharts are in scope; page-level transitions are not.
- **Configurable composite RAG thresholds** -- fixed at 75/40 in v1. Per-metric thresholds are already configurable; adding composite thresholds is complexity without clear value yet.
- **Sprint comparison mode** -- no side-by-side sprint view. The sparklines and delta provide trend context. Full comparison deferred to v2.
- **Export (PDF/PNG)** -- deferred to v2 per the product spec.
- **Real-time updates after sync** -- no SignalR push. User refreshes the page after syncing a sprint.
- **Configurable disruption grace period** -- fixed at 2 days. Making it configurable adds a setting with low user demand.
- **Ticket drill-down from flags** -- zombie tickets and disruption flags show summary data. Clicking through to individual ticket detail pages is deferred.
- **Aggregate sprint view on Dashboard** -- Dashboard is single-sprint only. Multi-sprint aggregation is available on Developers, Sprints, and Epics pages.
- **Day-level disruption timeline** -- v1.md §5.2 describes a per-day disruption timeline showing which days of the sprint items were added. This level of detail belongs to F10 (Scope Change & Disruption). The summary card flag uses a simplified 2-day grace period threshold instead.
- **Disruption classification categories** -- v1.md §5.2 defines four disruption types (unplanned bug, scope injection, priority escalation, net-zero swap) and a "Removed SP" metric. These belong to F10 (Scope Change & Disruption). The summary card shows only the aggregate disruption rate and a mid-sprint disruption flag.
