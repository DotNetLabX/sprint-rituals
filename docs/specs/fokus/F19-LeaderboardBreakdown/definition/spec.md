# Leaderboard Breakdown

**Traces to:** `docs/product/v1.md` §5.7 (Sprint Summary Card — Developer leaderboard)
**Source:** Scratch
**Dependencies:** F8 (Sprint Summary Card — existing leaderboard), F13 (Bug Ratio — bug/non-bug classification), F9 (Developer Throughput — Developers page structure)
**Status:** Done
**Plan:** `docs/plans/LeaderboardBreakdown/plan.md`

---

## Purpose

The Dashboard leaderboard currently shows a single "SP completed" total per developer, blending feature work and bug fixes into one number. This hides work composition — a developer fixing 30 SP of bugs and building 20 SP of features looks identical to one shipping 50 SP of features. Since bug work typically means rework on poorly implemented features, and developers usually fix their own bugs, the combined number rewards the wrong signal. This feature splits the leaderboard into features and bugs modes, adds ticket count visibility, and introduces a richer Leaderboard tab on the Developers page for deeper analysis.

## Entities

This feature introduces no new domain entities. It extends the Dashboard sprint summary response and adds a new analytics endpoint for the Developers page. All data is computed from existing sprint memberships, tickets (issue type), and developer records using the same bug classification rule established by Bug Ratio (F13): issue type "Bug" = bug, everything else = non-bug.

## User Flows

```
Flow 1: View Dashboard Leaderboard (Features Mode — Default)
1. User opens the Dashboard
2. The leaderboard widget displays with a Features/Bugs toggle, defaulting to Features
3. The leaderboard shows one row per active developer, sorted by feature SP completed descending
4. Each row displays: rank, avatar, display name, sub-team badge, feature SP completed, feature tickets done
5. Developers with 0 feature SP appear at the bottom with zero values — they are never hidden
6. Only active developers are shown
```

```
Flow 2: Switch to Bugs Mode
1. User clicks the Bugs toggle on the leaderboard
2. The leaderboard switches to show bug work
3. Each row displays: rank, avatar, display name, sub-team badge, bug SP completed, bug tickets done
4. Sorted by bug SP completed descending — the developer with the most bug work is at the top
5. Switching back to Features restores the features view
```

```
Flow 3: Sub-Team Filter Interaction
1. User selects a sub-team from the page toolbar filter
2. The leaderboard filters to show only developers in that sub-team
3. Both Features and Bugs modes respect the sub-team filter
4. Selecting "All" removes the filter
```

```
Flow 4: View Developers Page Leaderboard Tab (Multi-Sprint)
1. User navigates to the Developers page
2. Three tabs are available: Throughput, Bug Ratio, Leaderboard
3. User clicks the Leaderboard tab
4. The sprint selector defaults to "Last 5" closed sprints
5. A stacked horizontal bar chart shows each developer with two segments: feature SP (blue) and bug SP (red), sorted by total SP descending
6. Below the chart, a table shows: name, sub-team, feature SP, bug SP, total SP, feature tickets, bug tickets, total tickets — aggregated across the selected sprints
7. Only active developers are shown
```

```
Flow 5: Developers Page Leaderboard — Single Sprint
1. User selects a single sprint from the sprint selector
2. The stacked bar chart shows per-developer feature SP vs bug SP for that sprint
3. The table shows per-developer metrics with delta values vs the prior sprint
4. Delta polarity: feature SP — higher is better; bug SP — lower is better; total SP — neutral
```

```
Flow 6: Developers Page Leaderboard — Change Sprint Range
1. User changes the sprint selector to "Last 3", "Last 5", "All", or a specific sprint
2. Selecting a range (Last 3/5/All) shows the multi-sprint view (Flow 4)
3. Selecting a single sprint shows the single-sprint view (Flow 5)
4. Chart and table update accordingly
```

```
Flow 7: Filter by Sub-Team on Developers Page
1. User selects a sub-team from the sub-team filter
2. All content on the Leaderboard tab recalculates scoped to developers in that sub-team
3. Chart and table filter to that sub-team's developers only
4. Selecting "All" removes the filter
```

## API Surface

### Modified endpoint

| Method | Route | Auth | Request | Response | Status codes |
|--------|-------|------|---------|----------|--------------|
| GET | /api/analytics/sprint-summary | Authenticated | Query params (unchanged) | Extended leaderboard entries | 200, 400 |

The existing sprint summary response's leaderboard entries are extended. Each developer entry adds: feature SP completed, bug SP completed, feature tickets done, bug tickets done. The existing total SP completed field is retained for backward compatibility. The endpoint always returns the full breakdown regardless of the active toggle — the Features/Bugs mode is a frontend-only view concern. The API continues to sort by total SP completed descending; the frontend re-sorts based on the active mode.

**Alignment note:** The existing Dashboard leaderboard does not currently apply excluded-from-scope statuses. This feature brings the Dashboard leaderboard into alignment with the Bug Ratio (F13) pattern by applying excluded-from-scope filtering consistently. This may cause minor differences in total SP compared to the pre-existing leaderboard for sprints where exclusions are configured.

### New endpoint

| Method | Route | Auth | Request | Response | Status codes |
|--------|-------|------|---------|----------|--------------|
| GET | /api/analytics/leaderboard | Authenticated | Query params (see below) | Leaderboard data | 200, 400 |

**Query parameters:**
- `sprintId` (int, optional) — single sprint. Mutually exclusive with `last`.
- `last` (int, optional) — last N closed sprints. Mutually exclusive with `sprintId`.
- `subTeam` (string, optional) — filter developers by sub-team name. Omit or pass empty for all.

**Default behavior:** When neither `sprintId` nor `last` is provided, defaults to `last=5`.

**Response shape — multi-sprint mode (when `last` or default):**

Top level:
- `sprints` — ordered list of sprint summaries (id, name, start date, end date)
- `developers` — list of per-developer leaderboard entries (see below)

Per developer entry (multi-sprint):
- Developer identity: display name, sub-team, avatar URL
- Aggregated totals across selected range: feature SP, bug SP, total SP, feature tickets, bug tickets, total tickets
- Per-sprint breakdown (one entry per sprint): sprint ID, feature SP, bug SP, total SP, feature tickets, bug tickets

**Response shape — single-sprint mode (when `sprintId`):**

Top level:
- `sprint` — sprint metadata (id, name, start date, end date)
- `developers` — list of per-developer entries (see below)

Per developer entry (single sprint):
- Developer identity: display name, sub-team, avatar URL
- Metrics: feature SP, bug SP, total SP, feature tickets, bug tickets, total tickets
- Delta values (current minus prior sprint): feature SP delta, bug SP delta, total SP delta, feature tickets delta, bug tickets delta — each with direction and polarity

**Error conditions:**
- 400: Both `sprintId` and `last` provided simultaneously
- 400: `sprintId` does not match any synced closed sprint
- 400: `last` is less than 1
- 200 with empty results: No closed sprints exist

## Business Rules

1. **Bug classification uses the same rule as Bug Ratio (F13).** Issue type "Bug" (exact match, case-sensitive) = bug. All other issue types = non-bug (feature). No additional classification rules.

2. **Only completed tickets count.** Both SP and ticket counts measure completed work only — tickets whose final status matches a value in the done statuses list. Unfinished tickets do not appear in the leaderboard.

3. **Removed tickets are excluded.** Tickets removed from the sprint (removal timestamp present) do not count toward any leaderboard metrics.

4. **Excluded-from-scope statuses apply.** Tickets whose final status matches an excluded-from-scope status are excluded from all leaderboard calculations, consistent with Bug Ratio (F13) and Scope Change (F10).

5. **Ticket count includes all done tickets regardless of story points.** A done ticket counts in the ticket column whether or not it has story points assigned. This is consistent with ticket-based metrics in Throughput (F9) and Bug Ratio (F13). SP columns exclude no-SP tickets as usual.

6. **Dashboard leaderboard defaults to Features mode.** The toggle resets to Features when navigating away from the Dashboard and returning.

7. **Dashboard leaderboard sort order is SP descending in the active mode.** In Features mode, sorted by feature SP descending. In Bugs mode, sorted by bug SP descending. The developer with the most bug work appears at the top of the bugs view.

8. **Zero-value developers are visible.** Active developers with 0 SP in the active mode still appear in the leaderboard, sorted to the bottom. They are never hidden.

9. **Only active developers.** Developers marked as inactive are excluded from all leaderboard results.

10. **Cross-cutting developer exclusion applies.** Developers with 0% effective capacity who completed 0 tickets in the sprint are excluded from all leaderboard results, consistent with all other analytics endpoints.

11. **Developers page Leaderboard tab is the third tab.** Tab order: Throughput, Bug Ratio, Leaderboard. Sprint selector and sub-team filter persist across all three tabs.

12. **Multi-sprint aggregation uses summed totals.** Feature SP, bug SP, and ticket counts across multiple sprints are straight sums. Total SP = feature SP + bug SP.

13. **Stacked bar chart is sorted by total SP descending.** In multi-sprint mode, bars are sorted by aggregated total SP. In single-sprint mode, by that sprint's total SP.

14. **Delta is single-sprint only.** When viewing a single sprint on the Developers page, each metric includes a delta comparing to the prior closed sprint. Multi-sprint views have no delta — the chart provides trend context.

15. **Delta polarity.** Feature SP: higher is better (green up, red down). Bug SP: lower is better (green down, red up). Total SP: neutral (no color). Feature tickets: higher is better. Bug tickets: lower is better.

16. **Developers page Leaderboard tab defaults to "Last 5" sprint range.** Consistent with Bug Ratio (F13) default behavior.

17. **Sub-team filter applies to developers, not tickets.** Filtering by sub-team shows developers in that sub-team with all their tickets. Consistent with all existing analytics pages.

18. **Sprint ordering.** Sprints are ordered by start date ascending. "Most recent closed sprint" means the closed sprint with the latest start date. "Prior sprint" for delta means the closed sprint with the next-earlier start date.

19. **Default SP per bug applies.** Unestimated bugs use the configured default SP per bug value (from F16) when computing SP metrics, consistent with existing throughput calculations.

## Acceptance Criteria

- [ ] Dashboard leaderboard shows a Features/Bugs toggle, defaulting to Features
- [ ] Features mode displays: rank, avatar, name, sub-team, feature SP, feature tickets done
- [ ] Bugs mode displays: rank, avatar, name, sub-team, bug SP, bug tickets done
- [ ] Dashboard leaderboard sorts by SP descending in the active mode
- [ ] Toggle resets to Features when navigating away from Dashboard and returning
- [ ] Sub-team filter on Dashboard scopes the leaderboard to the selected sub-team
- [ ] Developers page shows three tabs: Throughput, Bug Ratio, Leaderboard
- [ ] Throughput and Bug Ratio tabs are unchanged
- [ ] Sprint selector and sub-team filter persist across all three tabs
- [ ] Leaderboard tab defaults to "Last 5" sprint range
- [ ] Stacked horizontal bar chart shows feature SP (blue) and bug SP (red) per developer
- [ ] Chart is sorted by total SP descending
- [ ] Table below chart shows: name, sub-team, feature SP, bug SP, total SP, feature tickets, bug tickets, total tickets
- [ ] Multi-sprint view aggregates totals across selected sprints
- [ ] Single-sprint view shows delta values for each metric vs prior sprint
- [ ] Delta polarity: feature SP higher=green, bug SP lower=green, total SP neutral
- [ ] Bug classification: issue type "Bug" = bug, all others = non-bug (consistent with F13)
- [ ] Only completed tickets (final status in done statuses) count
- [ ] Removed tickets (removal timestamp present) are excluded
- [ ] Excluded-from-scope statuses are respected
- [ ] Ticket count includes all done tickets regardless of story points
- [ ] SP metrics exclude tickets with no effective story points (unestimated bugs use the default SP per bug; only tickets with neither estimated SP nor applicable default are excluded)
- [ ] Default SP per bug applies to unestimated bugs (consistent with F16)
- [ ] Zero-value developers appear with zero values (not hidden)
- [ ] Only active developers appear in results
- [ ] Developers with 0% capacity and 0 completed tickets are excluded (cross-cutting exclusion)
- [ ] Dashboard leaderboard applies excluded-from-scope statuses (aligning with Bug Ratio pattern)
- [ ] GET /api/analytics/leaderboard with no params defaults to last 5 closed sprints
- [ ] GET /api/analytics/leaderboard with `sprintId` returns single-sprint data with deltas
- [ ] GET /api/analytics/leaderboard with `last=N` returns multi-sprint aggregated data
- [ ] 400 returned when both `sprintId` and `last` are provided
- [ ] 400 returned when `sprintId` does not match a synced closed sprint
- [ ] 400 returned when `last` is less than 1
- [ ] Sprint summary endpoint leaderboard entries include feature SP, bug SP, feature tickets, bug tickets
- [ ] Inactive developers are excluded from all responses

## Out of Scope

- **Investment profile categories** — LinearB-style multi-category classification (features, tech debt, KTLO, dev experience) requires configurable rules. The binary bug/non-bug split is sufficient for v1.
- **Per-developer detail page** — no individual developer page with full ticket breakdown. The table shows aggregates only. Deferred to v2.
- **Leaderboard on Developers page trend chart** — no multi-sprint trend line showing feature vs bug SP over time per developer. The stacked bar provides per-sprint composition; a trend line is a future enhancement.
- **Gamification elements** — no badges, streaks, or achievements. The leaderboard is a visibility tool.
- **Export (PDF/PNG)** — deferred to v2 per the product spec.
- **Real-time updates** — no SignalR push when sync completes. User refreshes after syncing.
