# Burnup Bug Overlay

**Traces to:** `docs/product/v1.md` §5.2 (Sprint Scope Change & Disruption Analysis)
**Source:** Scratch
**Dependencies:** F10 (Scope Change & Disruption — owns the burnup chart), F16 (Bug Cost & Disruption Split — default SP per bug ensures bugs carry SP)
**Status:** Done
**Plan:** N/A — implemented incrementally via F21/F23/F24 burnup chart work

---

> **Revision (2026-05-10) — Implemented:** Bug SP changed from scope tracking to flow tracking. Previously, Bug SP = all bugs in the sprint (membership only — completion didn't reduce it, making the line nearly flat). Now, Bug SP = remaining/open bugs only — completed bugs (first done transition) are subtracted. The feature is already implemented with the old behavior. Only the Bug SP computation needs updating; the frontend renders whatever `bugSp` value the API returns. The backend already has the `doneTransitionByTicket` lookup used for the Completed SP line — filter it to bugs and subtract from cumulative bug SP each day.

---

## Purpose

The Scope Burnup chart shows total scope and completed SP over sprint days, but gives no visibility into the remaining bug burden. A Scrum Master looking at the chart cannot tell how much bug work is left, whether bugs are being burned down, or whether new bugs are outpacing completions. This feature adds a cumulative remaining bug SP shaded area to the burnup chart so teams can see bug burn-down happening alongside scope burn-up — and spot when new bugs are outpacing completions.

## Entities

No new entities. This feature reads existing sprint membership data (IssueType, StoryPoints, WasCommitted, AddedAt, RemovedAt) and the default SP per bug setting introduced by F16.

## User Flows

```
Flow 1: View Bug Overlay on Single-Sprint Burnup
1. User navigates to Sprints and selects a single sprint
2. The Scope Burnup chart renders as before: orange line (Total Scope SP) and green line (Completed SP)
3. A semi-transparent red shaded area appears anchored to the bottom of the chart
4. The area represents remaining (open) bug SP in the sprint as of each day:
   - Day 1: bug SP from committed bugs (present at sprint start)
   - Subsequent days: area grows when bug tickets are added, shrinks when bug tickets are completed or removed
5. The legend updates to include "Bug SP" alongside the existing entries
6. Hovering a data point shows all three values in the tooltip: Total Scope SP, Completed SP, Bug SP
```

```
Flow 2: Sprint with No Bugs
1. User selects a sprint that has zero bug tickets
2. The burnup chart renders normally with no shaded area
3. The "Bug SP" legend entry still appears but shows 0
```

```
Flow 3: Sub-Team Filter Applied
1. User selects a sub-team from the toolbar filter
2. The bug SP area recalculates to include only bugs assigned to developers in that sub-team
3. All three series (total scope, completed, bug SP) are filtered consistently
```

## API Surface

No new endpoints. The existing scope-change endpoint response is extended.

| Method | Route | Auth | Changes | Status codes |
|--------|-------|------|---------|--------------|
| GET | /api/analytics/scope-change?sprintId={id} | None | Burnup data point gains `bugSp` field | 200, 400 |

**Burnup data point (extended):**
- Day number (1-based) — existing
- Calendar date — existing
- Total scope SP (cumulative) — existing
- Completed SP (cumulative) — existing
- Phase label — existing
- **Bug SP (remaining)** — new. Remaining SP from bug-type tickets that are in the sprint as of that day and not yet completed. Decreases when bugs hit a done status, increases when new bugs are added. Uses effective SP (actual estimate when present, default SP per bug fallback when unestimated, per F16 rules).

## Business Rules

1. **Bug SP tracks remaining (open) bug work.** On each sprint day, bug SP = sum of effective SP for all bug-type tickets that are in the sprint as of that day and not yet completed. A bug is considered completed on its first transition to a done status — matching how Completed SP counts done work. Completed bugs are subtracted from bug SP from that day onward. New bugs added mid-sprint increase bug SP; removed bugs and completed bugs decrease it.

2. **Effective SP follows F16 rules.** A bug's effective SP is its actual story point estimate when present, or the configured default SP per bug when unestimated. When default SP per bug is 0 (disabled), unestimated bugs contribute 0 SP.

3. **The shaded area is anchored to the chart baseline (zero).** It is not stacked on top of another series. The area height at any day equals the cumulative bug SP value for that day. This means the completed SP line (green) draws on top of the shaded area when both are in the same vertical range.

4. **Removed bugs reduce the area.** When a bug ticket is removed from the sprint, its SP stops contributing to the bug SP area from the removal day onward. This keeps the bug area consistent with the total scope line.

5. **Excluded-from-scope filtering applies.** Bug tickets whose final status matches an excluded-from-scope status are excluded from the bug SP area, consistent with how they are excluded from all other scope metrics.

6. **Sub-team filtering applies.** When a sub-team filter is active, only bugs assigned to developers in that sub-team contribute to the bug SP area.

7. **Visual styling: semi-transparent red area.** The area uses a soft red fill with low opacity (~20%) so the completed SP line remains clearly readable when it passes through the shaded region. The exact color should harmonize with the existing chart palette.

## Acceptance Criteria

- [ ] Single-sprint burnup chart displays a semi-transparent red shaded area representing cumulative bug SP
- [ ] Bug SP area starts at day 1 with committed bug SP and grows/shrinks as bugs are added/removed/completed
- [ ] Bug SP area is anchored to the chart baseline (zero), not stacked on another series
- [ ] Bug SP uses effective SP (actual estimate or default SP per bug fallback per F16)
- [ ] When default SP per bug is 0, unestimated bugs contribute 0 to the bug SP area
- [ ] Completed SP line (green) renders on top of the bug SP area without visual interference
- [ ] Chart legend includes "Bug SP" entry
- [ ] Tooltip on hover shows Total Scope SP, Completed SP, and Bug SP for that day
- [ ] Bug SP area is absent (or flat at 0) when the sprint has no bug tickets
- [ ] Sub-team filter restricts bug SP area to bugs assigned to that sub-team's developers
- [ ] Excluded-from-scope bug tickets are excluded from the bug SP area
- [ ] Bug SP area decreases when a bug ticket transitions to a done status (first done transition)
- [ ] Removed bug tickets stop contributing from their removal day onward
- [ ] GET /api/analytics/scope-change?sprintId={id} burnup data points include `bugSp` field
- [ ] `bugSp` field reflects remaining (open) bug SP and is consistent with done-transition logic used by Completed SP
- [ ] Multi-sprint mode (trend view) is unaffected — the overlay appears only in single-sprint detail

## Out of Scope

- **Bug SP line instead of area** — a line would overlap with the completed SP line. The shaded area solves this by design.
- **Bug SP in multi-sprint trend view** — the stacked bar chart in trend mode already shows disruption categories including unplanned bugs. The temporal overlay is only meaningful at the single-sprint level.
- **Bug SP percentage overlay** — showing bug SP as a percentage of total scope rather than absolute SP. Absolute SP is more immediately readable on the existing SP-scaled Y-axis.
