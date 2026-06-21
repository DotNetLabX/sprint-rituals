# Daily Developer Progress

**Traces to:** `docs/product/v1.md` §5.1 (Developer Throughput — per-developer analytics pattern). Extends v1 into active sprint daily tracking — a concept not in the original spec.
**Source:** Scratch
**Dependencies:** F9 (Developer Throughput — Developers page, tab pattern), F23 (Transition-Based Sprint Scope — completion timestamps), adhoc-ActiveSprintAnalytics (active sprint support in analytics pipeline)
**Status:** Done
**Plan:** `docs/specs/F32-DailyDeveloperProgress/delivery/plan.md`

---

## Deviations from Source

**All ticket types included.** v1 §5.1 Developer Throughput is feature-only (bugs excluded from SP counts). Daily Progress includes features, bugs, and tasks because it tracks total daily work output — not delivery-vs-bug separation. A developer who spent the day fixing 3 bugs is progressing through work, and the burnup should reflect that.

**Active sprint focus.** v1 §1 states Fokus "analyzes closed sprints" and is "not a real-time monitoring tool." This feature extends into active sprint progress tracking — a deliberate expansion of scope. No competing tool offers per-developer burnup charts natively; this fills that gap.

**Assigned scope includes all tickets, not just started.** v1 §5.1 Developer Throughput defines "assigned" as tickets that transitioned to the start stage (transition-based scope). For Daily Progress, "assigned SP" means all non-removed tickets in the sprint for the developer — regardless of whether they have started. Rationale: a Scrum Master checking daily progress wants to see total workload. A developer with 10 SP total but only 3 SP started should see a pace line based on 10 SP, not 3 SP. Showing only started work would make progress look artificially high.

## Purpose

Fokus shows per-developer analytics aggregated at sprint level — how many SP a developer completed over an entire sprint. But during an active sprint, Scrum Masters need to know where each developer is right now: who is on track, who is falling behind, and which tickets are stalled. The Daily Progress tab gives Scrum Masters a live signal for mid-sprint course correction without waiting for the retro.

## Entities

No new entities. This feature reads from existing data:

- **Sprint** — start date, end date, state (active/closed)
- **Sprint Membership** — ticket assignment to sprint, story points, removal status
- **Ticket** — key, summary, issue type, assignee, story points
- **Status Transition** — timestamped status changes per ticket (determines completion day and stall detection)
- **Developer** — identity, sub-team, active status
- **Developer Sprint Capacity** — per-sprint capacity override

No settings extensions. The stall detection threshold (2 business days) and grace period (2 calendar days) are fixed.

## User Flows

```
Flow 1: View Daily Progress for Active Sprint
1. User navigates to the Developers page
2. User selects the "Daily Progress" tab. URL updates to ?tab=daily-progress
3. The sprint selector in the toolbar is disabled (grayed out) on this tab — Daily Progress always shows the active sprint, so sprint selection is not applicable. The previously selected sprint is preserved and restored when the user switches to another tab.
4. If no active sprint exists, the tab shows: "No active sprint. Sync a sprint to get started."
5. An alert banner at the top lists developers currently behind their expected pace, showing: name, SP gap, and days behind (e.g., "John: 5 SP behind, ~1.5 days")
6. The alert banner only appears after the 2-day grace period from sprint start
7. Below the banner, a card grid displays one card per developer who has assigned tickets
8. Each card shows: developer avatar and name, sub-team badge, mini burnup chart, SP completed / SP assigned, completion %, stalled ticket count (if any)
9. The mini burnup chart displays: actual cumulative SP line (solid, colored), expected pace line (dashed, gray), day numbers on x-axis
10. Cards for behind-pace developers have a visual warning indicator
11. Cards for developers with stalled tickets show a stall badge with count
```

```
Flow 2: Drill Into a Day
1. User clicks or hovers a day point on a developer's burnup chart
2. A tooltip or popover shows tickets completed that day
3. Each ticket displays: ticket key, summary, story points, issue type icon
4. If no tickets were completed that day, shows "No completions"
```

```
Flow 3: View Stalled Tickets
1. User clicks the stall badge on a developer's card
2. An expandable section or popover shows stalled tickets
3. Each ticket displays: ticket key, summary, current status, days since last status transition
4. Stalled = no status transition in 2+ business days (Mon-Fri), still in progress (started but not completed)
```

```
Flow 4: Filter by Sub-Team
1. User selects a sub-team from the toolbar filter
2. Cards filter to show only developers in that sub-team
3. Alert banner recalculates for the filtered set only
4. Developers outside the selected sub-team are hidden
```

```
Flow 5: Auto-Refresh on Sync
1. A user (same or different browser) triggers a Jira sync
2. The sync completes and publishes a real-time event
3. The Daily Progress view refreshes data automatically — no page reload required
4. Updated burnup charts, alerts, and stall indicators reflect the new data
```

```
Flow 6: Empty States
6a. No active sprint -> tab shows "No active sprint" message
6b. Active sprint exists but no developers have assigned tickets -> tab shows "No developers with assigned tickets in this sprint"
6c. Active sprint, day 1-2 (grace period) -> cards show normally, alert banner is hidden
```

## API Surface

| Method | Route | Auth | Request body | Response | Status codes |
|--------|-------|------|-------------|----------|-------------|
| GET | /api/analytics/developer-progress | Authenticated | — | DeveloperProgressResponse | 200 |

**Query parameters:**
- `subTeam` (string, optional) — filter developers by sub-team name.

**No sprint selection parameter.** This endpoint always returns data for the active sprint. If no active sprint exists, returns 200 with an empty-state response.

**Response shape:**

Top level:
- `hasActiveSprint` — boolean (false when no active sprint exists)
- `sprint` — sprint summary (id, name, start date, end date) or null
- `currentDay` — integer, current day of the sprint (1-based, calendar days from start date)
- `totalDays` — integer, total calendar days in the sprint
- `isGracePeriod` — boolean, true when currentDay <= 2
- `alerts` — list of behind-pace alerts (empty during grace period or when no one is behind)
- `developers` — list of developer progress entries

Per alert entry:
- Developer identity (display name, avatar URL, account ID)
- `gapSp` — SP behind expected pace (positive number)
- `gapDays` — days behind expected pace (gapSp / daily pace, rounded to 1 decimal)

Per developer entry:
- Developer identity: display name, sub-team, avatar URL, account ID
- `assignedSp` — total SP assigned in active scope (all ticket types)
- `completedSp` — total SP completed so far (all ticket types)
- `completionPercent` — completedSp / assignedSp x 100
- `capacityPercent` — effective capacity for this sprint
- `dailyPace` — expected SP per day (assignedSp x capacityPercent / 100 / totalDays)
- `isBehindPace` — boolean (false during grace period)
- `paceGapSp` — SP behind expected cumulative pace (positive = behind, negative = ahead; null during grace period)
- `stalledTickets` — list of stalled ticket summaries (may be empty)
- `dailyBreakdown` — array of day entries from day 1 to currentDay

Per stalled ticket:
- Ticket key, summary, current status, issue type, story points
- `daysSinceLastTransition` — integer, business days since last status transition

Per day entry:
- `day` — day number (1-based)
- `date` — calendar date
- `completedTickets` — list of tickets completed that day (key, summary, storyPoints, issueType)
- `cumulativeSp` — cumulative SP completed through this day
- `expectedCumulativeSp` — expected SP at this day (dailyPace x day)

**Error conditions:**
- 200 with `hasActiveSprint: false` when no active sprint exists — not an error

## Real-time Events

| Event | Payload | Trigger |
|-------|---------|---------|
| Sprint data updated | Sprint ID | Sprint sync completes for the active sprint |

**Infrastructure note:** No real-time push infrastructure exists in the codebase yet. The tech stack includes SignalR and a domain event interceptor is registered, but no hubs or client subscriptions are wired. This feature requires building the initial real-time infrastructure: a hub that broadcasts after sprint sync completes, and a frontend subscription that triggers a data refresh. The scope is narrow — a single event type with a sprint ID payload — but it is new infrastructure, not reuse of an existing pattern.

## Business Rules

### Ticket Scope

1. **All ticket types included.** Unlike the Throughput tab (feature-only), Daily Progress includes features, bugs, and tasks. This reflects total daily work output, not delivery-vs-bug separation. To prevent cross-tab confusion (the Throughput tab shows different SP numbers for the same developer), the SP values on each developer card display a small "(all types)" label. This signals to the user that the numbers include bugs and tasks, not just features.

2. **Assigned scope.** A ticket is in scope when it is a non-removed membership in the sprint and is assigned to this developer. Unlike Throughput (which requires a transition to the start stage), Daily Progress counts all non-removed tickets for the developer regardless of whether they have started. This gives the Scrum Master a true picture of total workload — a developer with 10 SP total but only 3 SP started should see a pace line based on 10 SP, not 3 SP.

3. **Completed definition.** A ticket is completed when it has transitioned to the cycle time end stage or beyond during the sprint. The completion timestamp from this transition determines which day the ticket counts on. Same transition-based attribution as other analytics.

4. **Removed tickets excluded.** Tickets with a non-null removal date are excluded from all computations — assigned SP, completed SP, stall detection.

5. **Story points.** Uses the effective SP computation: ticket SP if present, the configured default SP per bug (DefaultSpPerBug setting) for unestimated bugs, null for unestimated non-bugs. Tickets with null effective SP are counted in ticket lists but excluded from SP sums.

6. **Ticket attribution.** Tickets are attributed to the current assignee at sync time (the developer who has the ticket when the sprint is synced). Same rule as all other analytics tabs.

7. **ExcludedFromScopeStatuses not applied.** Same as the Throughput tab — daily progress tracks total work output and does not filter by excluded-from-scope statuses.

### Pace Computation

8. **Daily pace per developer.** assignedSp x (capacityPercent / 100) / totalDays. Where totalDays is the number of calendar days from sprint start to sprint end (inclusive), and capacityPercent is the developer's effective capacity for this sprint (sprint-specific override or developer default).

9. **Expected cumulative SP at day N.** dailyPace x N. This is the expected pace line on the burnup chart — a straight diagonal from (day 0, 0 SP) to (last day, assignedSp x capacity%).

10. **Calendar days, not business days.** The pace line uses calendar days for simplicity. Weekends appear as flat spots on the actual completion line, which is expected and matches Jira's burndown approach.

### Behind-Pace Alerting

11. **Behind-pace threshold.** A developer is behind pace when their cumulative completed SP on the current day is less than the expected cumulative SP by more than one day's worth of SP. Formula: isBehindPace = (expectedCumulativeSp - cumulativeSp) > dailyPace.

12. **Grace period.** No behind-pace alerts during the first 2 calendar days of the sprint (currentDay <= 2). The alert banner is hidden entirely. Individual cards show progress but no warning indicators.

13. **Pace gap computation.** paceGapSp = expectedCumulativeSp - cumulativeSp. paceGapDays = paceGapSp / dailyPace (rounded to 1 decimal). Both are null during the grace period.

14. **Alert banner scope.** The alert banner lists only developers who are behind pace (isBehindPace = true). It recalculates when a sub-team filter is applied — only filtered developers appear.

### Stall Detection

15. **Stalled ticket definition.** A ticket is stalled when: (a) it is in the sprint (non-removed membership), (b) it has been started (transitioned to the cycle time start stage or beyond), (c) it is not completed, and (d) its most recent status transition is more than 2 business days ago. Business days are Monday through Friday.

16. **Days since last transition.** Counts business days (Mon-Fri) from the most recent status transition timestamp to the current date. Weekends and the transition day itself are excluded.

17. **Stall detection is independent of pace.** A developer can be on pace overall but still have a stalled ticket. Both signals are shown independently.

### Display & Filtering

18. **Hide developers with no assignments.** Developers with zero non-removed tickets in the sprint are not shown — no card, not counted in alerts.

19. **Only active developers.** Developers marked as inactive are excluded from all results.

20. **Sub-team filter.** Same pattern as other Developers tabs: filters developers by sub-team, recalculates alert banner for the filtered set. Null sub-team = all developers.

21. **Developer exclusion.** Same excluded developer rules as other tabs: 0% capacity + 0 completed tickets = hidden.

22. **Sprint selector behavior.** The sprint selector is disabled (grayed out) on the Daily Progress tab. This tab always shows the active sprint — no sprint selection is needed. The selector does not mutate shared state: the previously selected sprint is preserved and restored when the user switches to any other tab. This avoids the problem of tab-switching changing the sprint context for other tabs.

23. **Tab position.** Daily Progress appears as the last tab on the Developers page — after QA Workload (when Xray is enabled) or after Leaderboard (when Xray is disabled). It is always visible regardless of Xray status.

24. **Burnup chart Y-axis scale.** Fixed to the developer's assigned SP x capacity% (the pace line target). This ensures the chart proportions are meaningful — a developer with 2/10 SP done shows a small portion, not a full chart.

25. **Completion percent can exceed 100%.** If a developer completes carry-over tickets from prior sprints (started before this sprint, completed during it), their completedSp may exceed assignedSp. This is valid and should be displayed as-is, not capped.

### Division by Zero

26. **Zero assigned SP.** dailyPace = 0, completionPercent = 0%, isBehindPace = false (cannot be behind when nothing is assigned). Since isBehindPace is always false, paceGapDays is never computed — the division by dailyPace = 0 is unreachable.

27. **Zero total days.** If sprint start equals sprint end (degenerate sprint), totalDays = 1.

## Acceptance Criteria

### Daily Progress Tab

- [ ] "Daily Progress" tab appears as the last tab on the Developers page (after QA Workload or Leaderboard depending on Xray status)
- [ ] Tab is always visible regardless of Xray status
- [ ] Tab URL updates to ?tab=daily-progress
- [ ] Sprint selector is disabled (grayed out) on this tab
- [ ] Switching to another tab restores the previously selected sprint
- [ ] SP values on cards display "(all types)" label
- [ ] One card per developer with assigned tickets in the active sprint
- [ ] Cards show: avatar, name, sub-team, mini burnup chart, SP completed/assigned, completion %, stall count
- [ ] Developers with no assigned tickets are hidden
- [ ] Inactive developers are excluded

### Burnup Chart

- [ ] Each card contains a mini burnup chart with actual (solid) and expected pace (dashed) lines
- [ ] X-axis shows day numbers, Y-axis shows cumulative SP
- [ ] Clicking or hovering a day point shows completed tickets for that day (key, summary, SP, type)
- [ ] Days with no completions show "No completions" on drill-down

### Alert Banner

- [ ] Alert banner at top lists behind-pace developers with name, SP gap, and days behind
- [ ] Alert banner hidden during grace period (first 2 days of sprint)
- [ ] Behind-pace threshold: cumulative gap exceeds one day's worth of SP
- [ ] Alert banner respects sub-team filter

### Stall Detection

- [ ] Stall badge appears on cards with stalled tickets (no status transition in 2+ business days)
- [ ] Clicking stall badge shows stalled tickets: key, summary, status, days stalled
- [ ] Only in-progress (started, not completed) tickets are checked for stall
- [ ] Weekends excluded from business day count

### Filtering & Refresh

- [ ] Sub-team filter works the same as other Developers tabs
- [ ] View auto-refreshes via real-time push when a sprint sync completes
- [ ] No manual refresh required after sync

### Empty States

- [ ] No active sprint: "No active sprint" message
- [ ] Active sprint, no assigned developers: "No developers with assigned tickets" message
- [ ] Grace period: cards shown, alert banner hidden

### API

- [ ] GET /api/analytics/developer-progress returns 200 with active sprint progress data
- [ ] Endpoint accepts ?subTeam filter
- [ ] Response includes daily breakdown with per-day completed tickets and cumulative SP
- [ ] Response includes stalled tickets per developer with days since last transition
- [ ] Response includes alert list with pace gap in SP and days
- [ ] 200 with hasActiveSprint=false when no active sprint exists

## Out of Scope

- **Closed sprint daily progress** — The data exists for closed sprints but this feature targets active sprint only. Extending to closed sprints (for retro analysis) is a future enhancement with minimal additional effort.
- **Automated notifications** — No Slack, email, or push notifications for behind-pace developers. The alert banner is a pull-based signal visible when the Scrum Master opens the page.
- **Git activity signal** — Competitor tools (Swarmia, LinearB) flag "no git activity" alongside ticket stalls. Fokus doesn't integrate with GitHub/GitLab yet (deferred in v1 §8). Stall detection uses status transitions only.
- **WIP limits** — Working agreement enforcement (e.g., max 3 tickets in progress per developer) is a separate concept from progress tracking.
- **Burnout detection** — "Worked 90% of sprint days" alerting requires git activity data not available in Fokus.
- **Configurable stall threshold** — Fixed at 2 business days. Can be made configurable if teams need different thresholds.
- **Configurable grace period** — Fixed at 2 calendar days. Same rationale.
- **Multi-sprint trend view** — No daily progress comparison across sprints. This is a single-sprint, live-progress feature.

## Open Questions

None — all resolved during discussion.
