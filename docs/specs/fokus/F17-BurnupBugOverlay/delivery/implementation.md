# Burnup Bug Overlay — Implementation

## Files Modified
- `src/Services/Fokus/Fokus.API/Features/Analytics/ScopeChangeService.cs` — Extended `BurnupDataPoint` record with `BugSp` field. Added cumulative bug SP computation in `BuildBurnupData`: starting committed bug SP + daily bug additions - daily bug removals, filtered to `IssueType == "Bug"`, using `GetEffectiveSp(defaultSpPerBug)`, respecting excluded-from-scope statuses.
- `client/src/types/index.ts` — Added `bugSp: number` to `BurnupDataPoint` interface.
- `client/src/components/sprints/BurnupChart.vue` — Added "Bug SP" area series (semi-transparent red `#ef4444` at 20% opacity, no stroke, anchored to baseline). Renders first in series array so it appears behind Total Scope and Completed SP. Colors, stroke widths, and fill arrays updated for 3 series.

## Key Decisions
- Bug SP area placed first in the ApexCharts series array so it renders behind both existing series (z-order by array position).
- Used `stroke.width: 0` for the bug area to avoid a red border line — only the shaded fill appears.
- `fill.opacity: [0.2, 1, 1]` gives the bug area 20% opacity while preserving existing gradient behavior on Completed SP.

## Deviations from Plan
- None (no plan existed — implemented directly from spec).
