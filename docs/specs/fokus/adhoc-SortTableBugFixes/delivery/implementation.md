# Sort Table Bug Fixes — Implementation

## Files Modified
- `client/src/views/DevelopersView.vue` — Broke v-if/v-else-if chain for Bug Ratio tab so BugRatioTab stays mounted during loading (prevents sort state reset). Added `:class` highlight bindings to all 12 sortable throughput table headers (6 single-sprint, 6 multi-sprint).
- `client/src/components/developers/BugRatioDevTable.vue` — Added `:class` highlight bindings to all 10 sortable bug ratio table headers (5 per table, across both multi and single-sprint tables).

## Key Decisions
- Used independent `v-if`s instead of `v-if`/`v-else-if` chain to match the existing throughput tab loading pattern (line 350 shows "Updating..." alongside stale data).
- Used dual `class` + `:class` bindings (Vue merges them) rather than converting to a single dynamic `:class` array — minimal diff, Tailwind-safe.

## Deviations from Plan
- None (conversation-based plan).
