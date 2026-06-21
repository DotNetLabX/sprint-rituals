# Normalized Capacity Indicator

**Traces to:** `docs/product/v1.md` §5.1 (Developer Throughput)
**Source:** Scratch
**Dependencies:** F9 (Developer Throughput), F15 (Team Management), F19 (Leaderboard Breakdown)
**Status:** Done
**Plan:** `docs/plans/NormalizedCapacityIndicator/plan.md`

---

## Purpose

Developers with reduced capacity (vacation, tech lead duties) show lower raw SP completed than full-time peers. A manager glancing at the sprint sees "8 SP" and may think underperformance, when the developer is actually delivering at a higher rate per available time. This feature adds a bracketed normalized value next to SP completed — visible only for developers below 100% capacity — so comparisons are fair without replacing the real planning number.

## Entities

No new entities. This feature uses existing data:
- Developer Sprint Capacity (effective capacity percentage per developer per sprint)
- Developer default capacity percentage
- SP completed (from throughput and leaderboard computations)

## User Flows

```
Flow 1: View Normalized SP on Developers Throughput Tab
1. User navigates to the Developers page (Throughput tab)
2. A developer with 60% capacity has completed 8 SP this sprint
3. Next to "8" the system shows "(~13)" — the normalized estimate at 100% capacity
4. Developers at 100% capacity show only their raw SP value with no bracket
5. The normalized value is visually secondary (smaller, muted color) to the raw value
```

```
Flow 2: View Normalized SP on Leaderboard Tab
1. User navigates to the Developers page (Leaderboard tab)
2. The stacked bar chart and table show SP completed per developer
3. Developers with reduced capacity show the bracketed normalized value next to their SP completed
4. Developers at 100% capacity show only the raw value
```

```
Flow 3: View Normalized SP on Dashboard Leaderboard
1. User views the Dashboard leaderboard widget
2. Any developer with reduced capacity in that sprint shows the bracketed normalized value next to their SP completed
3. The toggle between Features/Bugs modes preserves the normalized indicator
```

```
Flow 4: Multi-Sprint View
1. User selects "Last 3" or "Last 5" on the Developers page
2. For averaged multi-sprint values, the normalized indicator uses the average capacity across those sprints
3. If a developer was at 100% in all selected sprints, no bracket appears
4. If a developer had mixed capacity (e.g., 100%, 50%, 100%), the bracket shows normalization based on the average effective capacity across selected sprints
```

## API Surface

No new endpoints. The normalization is computed and displayed client-side using data already returned by existing endpoints:

| Endpoint | Already returns | Used for normalization |
|----------|----------------|----------------------|
| GET /api/analytics/developer-throughput | SP completed + capacity % per sprint | Throughput tab |
| GET /api/analytics/leaderboard | SP completed per developer | Leaderboard tab + Dashboard |

**Leaderboard endpoint change:** The leaderboard response must include each developer's effective capacity percentage for the sprint(s) in the response. Currently it does not — the capacity field must be added to the leaderboard response.

## Business Rules

1. **Normalization formula.** Normalized SP = `spCompleted / (effectiveCapacity / 100)`, rounded to the nearest integer.

2. **Display threshold.** The normalized indicator appears only when a developer's effective capacity is below 100% for the sprint. Developers at exactly 100% show only the raw SP value.

3. **Display format.** Raw value followed by normalized in brackets: "8 (~13)". The bracket content uses a tilde to signal it is an estimate, not a measured value.

4. **Visual hierarchy.** The raw SP value is the primary display (normal weight, normal size). The bracketed normalized value is secondary (smaller font, muted color). The raw number is the planning truth; the normalized number is a comparison aid.

5. **Capacity resolution.** Uses the same effective capacity logic as existing features: sprint-specific override if set, otherwise developer default capacity percentage.

6. **Multi-sprint normalization.** When viewing multiple sprints, compute per-sprint normalized values first, then average them. Do not average raw SP and then normalize by average capacity — that produces different (less accurate) results.

7. **No normalization at 100%.** If a developer's effective capacity is 100%, the formula produces the same value as raw SP. The bracket is suppressed — showing "(~8)" next to "8" is noise.

8. **Leaderboard includes capacity.** The leaderboard API response must include each developer's effective capacity so the frontend can compute the normalized indicator. For multi-sprint leaderboard, include average effective capacity across the selected sprints.

## Acceptance Criteria

- [ ] Developer Throughput tab shows "(~X)" next to SP completed for developers with <100% capacity
- [ ] Leaderboard tab shows "(~X)" next to SP completed for developers with <100% capacity
- [ ] Dashboard leaderboard widget shows "(~X)" next to SP completed for developers with <100% capacity
- [ ] Normalized value uses formula: `spCompleted / (effectiveCapacity / 100)`, rounded to nearest integer
- [ ] No bracket appears for developers at exactly 100% capacity
- [ ] The normalized value is visually smaller/muted compared to the raw SP value
- [ ] Multi-sprint views compute per-sprint normalization then average (not average-then-normalize)
- [ ] Leaderboard API response includes effective capacity per developer
- [ ] Features/Bugs toggle on Dashboard preserves normalized indicators
- [ ] Bracket format is "(~X)" with tilde indicating estimate

## Out of Scope

- **Normalizing other metrics** — only SP completed gets the indicator. SP assigned, ticket counts, rolling averages, and completion % are not normalized.
- **Configurable display toggle** — no setting to hide/show normalized values. Always shown for <100% capacity.
- **Capacity-normalized rolling average** — the rolling average already skips 0% sprints. No further capacity adjustment to it.
- **Color coding the normalized value** — no green/red on the bracket. It's informational context, not a performance judgment.
