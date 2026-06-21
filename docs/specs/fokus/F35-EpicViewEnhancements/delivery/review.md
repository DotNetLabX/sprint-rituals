# Epic View Enhancements — Review

## Reviewed By
reviewer (Sonnet agent — claude-sonnet-4-6)

## Verdict: APPROVE (Cycle 3)

---

## Pre-commitment Predictions

| Prediction | Found? |
|-----------|--------|
| `hiddenColumns` Set reactivity bug (developer self-flagged) | YES — confirmed CRITICAL |
| `visibleColspan` off-by-one or wrong column count | Not found — logic is correct |
| `searchSummaryMetrics` weighted average division-by-zero edge case | Handled — fallback to ticket-count average exists |
| Date formatting boundary condition off-by-one | Found — LOW severity |
| BR11 sort-reset at runtime vs hydration only | BR11 path covered correctly in `toggleColumnVisibility` |

---

## Findings

### [CRITICAL] Set mutations do not trigger Vue reactivity — column visibility is broken at runtime

**File:** `client/src/stores/epicsStore.ts:255–266`

**Issue:** `toggleColumnVisibility` calls `hiddenColumns.value.delete(columnId)` and `hiddenColumns.value.add(columnId)` directly on the Set object. Vue 3's `ref<Set<string>>` tracks reassignment of `.value` but does NOT track mutations on the Set itself. The three downstream consumers all depend on reactivity from this ref:

1. `EpicColumnToggle.vue:65` — `:checked="!hiddenColumns.has(col.id)"` — passed as a prop; Vue will not re-render the checkbox state after a toggle.
2. `EpicTable.vue:13` — `isColumnVisible: (columnId: string) => boolean` is a plain function prop; the `v-if="isColumnVisible(col)"` bindings in the table template are not tracked by Vue's reactivity system.
3. `epicsStore.ts:269` — `isColumnVisible` reads `!hiddenColumns.value.has(col)` — Vue cannot track this read through a Set mutation.

The result: clicking a column checkbox calls the action and updates localStorage correctly, but the table columns do not appear/disappear and the checkboxes do not reflect the new state. The feature is functionally broken at runtime.

The developer correctly flagged this in the Carry-Over section: "The current implementation calls `add`/`delete` on the set directly, which won't trigger reactivity without reassignment."

Note: `expandedEpicKeys` has the same pattern and is pre-existing — not in scope for this review.

**Fix:** In `toggleColumnVisibility`, reassign `hiddenColumns.value` after any mutation:
```ts
function toggleColumnVisibility(columnId: string) {
  const next = new Set(hiddenColumns.value)
  if (next.has(columnId)) {
    next.delete(columnId)
  } else {
    next.add(columnId)
    if (sortColumn.value === columnId) {
      sortColumn.value = DEFAULT_SORT_COLUMN
      sortDirection.value = DEFAULT_SORT_DIRECTION
      localStorage.setItem(SORT_STORAGE_KEY, JSON.stringify({ column: DEFAULT_SORT_COLUMN, direction: DEFAULT_SORT_DIRECTION }))
    }
  }
  hiddenColumns.value = next
  localStorage.setItem(COLUMNS_STORAGE_KEY, JSON.stringify(Array.from(next)))
}
```

**Confidence:** HIGH — `ref<Set>` mutation reactivity limitation is a documented Vue 3 constraint. The developer themselves flagged it as a known risk.

---

### [HIGH] No guard for `startIndex < 0` in date derivation loop

**File:** `src/Services/Fokus/Fokus.API/Features/Analytics/EpicProgressService.cs:185`

**Issue:** The date derivation loop uses `TransitionAttributionChecker.GetStageIndex(transition.ToStatus, orderedStages) >= startIndex`. When the workflow configuration has no cycle time start stage, `ResolveStartIndex` returns `startIndex = -1` (by convention — see `IsStartedInSprint` at line 96 which guards this explicitly). With `startIndex = -1`, any transition whose `ToStatus` is unknown (returns -1 from `GetStageIndex`) satisfies `-1 >= -1`, so every transition to an unrecognized status qualifies. This would produce incorrect `startedDate` and `lastWorkDate` values (likely very old noise transitions).

The velocity path (`IsStartedInSprint`, `IsCompletedInSprint`) both guard `if (startIndex < 0) return early` before using the same condition. The F35 date derivation omits this guard.

**Fix:** Add a guard before the date derivation loop:
```csharp
// Guard: if no cycle time start stage is configured, dates cannot be derived
if (startIndex >= 0)
{
    foreach (var ticket in tickets)
    {
        // ... existing loop
    }
}
```

**Confidence:** HIGH — `GetStageIndex` returning -1 for not-found is confirmed at `TransitionAttributionChecker.cs:80-81`. The sibling `IsStartedInSprint` at line 96 uses the identical guard pattern for the identical condition.

---

### [MEDIUM] Sort comparison treats ISO date strings as strings, not dates

**File:** `client/src/stores/epicsStore.ts:44-47` and `sortedEpics` computed at lines 116-135

**Issue:** `getEpicSortValue` returns `epic.startedDate` and `epic.lastWorkDate` as `string | null` (ISO 8601 date strings like `"2025-01-15"`). In `sortedEpics`, the sort comparator reaches the `string` branch at line 127 and uses `localeCompare`. ISO 8601 date strings (`YYYY-MM-DD`) sort correctly lexicographically, so this works in practice. However, the return type of `getEpicSortValue` is declared as `number | string | null`, and for date columns the sort uses string comparison — which is correct only because ISO 8601 strings happen to be lexicographically sortable. There is no type annotation distinguishing "this is a date string that happens to sort like a number" from "this is an alpha string like epicName." If a date format ever changes or the API returns a different ISO variant, the sort would silently break.

**Fix (optional — acceptable as-is given ISO 8601 guarantee):** Document with a comment in `getEpicSortValue` that date columns return ISO 8601 strings which are lexicographically sortable, or parse to number via `Date.parse()` before returning. Consider returning `Date.parse(epic.startedDate)` (a number) so these columns flow through the numeric comparison branch.

**Confidence:** MEDIUM — works correctly today with ISO 8601 dates but is fragile by design.

---

### [LOW] Date boundary: exactly 30 days shows "4w ago" via relative format

**File:** `client/src/components/epics/EpicTable.vue:102–108`

**Issue:** `if (diffDays <= 30)` includes day 30 in the relative branch. Inside that branch, `if (diffDays < 30)` is false for exactly 30, so it falls through to the hardcoded `return '4w ago'`. The spec says "dates within the last 30 days display in relative format." Exactly 30 days is a judgment call. The displayed value "4w ago" for day 30 is acceptable and the spec does not define the exact boundary as exclusive.

**Fix:** No fix required. LOW severity, no behavior change needed.

**Confidence:** LOW — spec language "within the last 30 days" is ambiguous at the boundary.

---

## Positive Observations

- **Backend date derivation is clean and correct.** The loop at `EpicProgressService.cs:177-194` correctly reuses the `transitionsByTicket` dictionary and the already-resolved `orderedStages`/`startIndex` from the same method. Sub-team scoping is correct: `tickets` is the filtered group, so only filtered-ticket transitions are accessed. The `DateOnly.FromDateTime` conversion is the right approach for a date-only field.

- **BR11 (sort reset when hiding sorted column) is fully implemented.** Both the hydration-time check (lines 81-86) and the runtime action (lines 260-263) correctly enforce the rule. The localStorage update is included in the reset path.

- **`searchSummaryMetrics` correctly handles all BR5 edge cases.** The weighted average falls back to ticket-count average when no SP data exists. The Unlinked Work card correctly reads from `epicProgress.unlinkedWork` rather than the search-filtered set.

- **`visibleColspan` computation is correct.** It correctly iterates `OPTIONAL_COLUMNS`, gates QA columns by `hasQaData`, and calls `isColumnVisible` for each. The "2 always-visible columns" base count (chevron + epic name) is accurate.

- **Column toggle dropdown correctly enforces BR13 and BR14.** Epic name is absent from `COLUMN_DEFS` in `EpicsView.vue`, so it cannot be toggled. QA columns are filtered from the dropdown when `hasQaData=false`.

- **`EpicColumnToggle` is a clean humble component.** Props in, events out, no store dependency. The click-outside overlay pattern is consistent with the codebase.

- **KB update (Step 7) is thorough.** All sections specified in the plan were added: Activity Dates (F35), updated Sort and Display, F35 additions to Per-Epic Metrics, and Client-Side Interactions. No existing sections were truncated.

- **TypeScript interface matches the C# record exactly.** `startedDate: string | null` and `lastWorkDate: string | null` in `index.ts:724-725` correctly mirror `DateOnly? StartedDate` and `DateOnly? LastWorkDate` in `EpicProgressEntry`, positioned after `activeSprintCount` and before `isCompleted` as specified.

---

## Gaps

- **No tests for `startedDate`/`lastWorkDate` derivation.** The plan's Testing Strategy calls for verifying: (a) multiple tickets with transitions, (b) no qualifying transitions, (c) mixed tickets. No backend tests were written. The plan listed these as manual verification scenarios — acceptable given the project has no existing test infrastructure, but noted as a gap.

- **`isColumnVisible` function prop is not reactive.** Even after fixing the Set reassignment, passing `store.isColumnVisible` as a plain function prop to `EpicTable` means Vue cannot track when its results change. After the Set fix, the `hiddenColumns` ref reassignment will trigger a re-render of the parent (`EpicsView`), which will re-pass the function prop, causing `EpicTable` to re-render — so it will work in practice. This is a consequence of the design choice to pass a function rather than the `hiddenColumns` ref itself. Note this in lessons.

---

## Open Questions

None — all findings are HIGH confidence or moved to Gaps.

---

## Carry-Over Resolution

| Carry-Over Item | Verdict |
|----------------|---------|
| `EpicColumnToggle` click-outside overlay (fixed-position div) | Confirmed acceptable — consistent with codebase pattern. No issue. |
| `formatActivityDate` inline in `EpicTable.vue` | Acceptable. No other component needs this yet. |
| `hiddenColumns` ref Set reactivity — developer flagged for runtime verification | CONFIRMED as CRITICAL bug. See Finding 1. Fix required before merge. |

---

## Evidence

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Backend build | PASS — 0 `error CS` lines | `dotnet build Fokus.API.csproj \| grep "error CS"` | (no output = zero errors) |
| Frontend build | PASS — exit 0, no TS errors | `cd client && npm run build` | `✓ built in 934ms` |
| Frontend tests | N/A | `npx vitest run` | No test files found |
| Pre-existing errors check | N/A | git log HEAD | F35 changes are on working tree, not committed |

---

## Cycle 2 Re-Review

**Fixes verified against cycle 1 findings:**

| Finding | Fix Applied | Verified |
|---------|-------------|---------|
| CRITICAL: Set reactivity — `toggleColumnVisibility` mutated Set in place | `const next = new Set(hiddenColumns.value)` → mutate `next` → `hiddenColumns.value = next`. Comment added explaining the reason. | `epicsStore.ts:256–271` — correct. Reassignment at line 270 triggers Vue reactivity. |
| HIGH: Missing `startIndex < 0` guard in date derivation | `if (startIndex >= 0) { ... }` wraps the entire `foreach` loop. Comment explains the -1 pattern. | `EpicProgressService.cs:182–199` — correct. Matches `IsStartedInSprint` guard pattern exactly. |
| MEDIUM: ISO 8601 sort comment missing | Comment added above `startedDate`/`lastWorkDate` cases explaining lexicographic == chronological for YYYY-MM-DD | `epicsStore.ts:52–53` — correct. |

**Adjacent call sites checked:** No regressions introduced. `sortedEpics` computed, `isColumnVisible` function, and `visibleColspan` computed all depend on `hiddenColumns.value` — reassignment correctly propagates to all three.

**Evidence (cycle 2):**

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Backend build | PASS — 0 `error CS` lines | `dotnet build Fokus.API.csproj \| grep "error CS"` | (no output = zero errors) |
| Frontend build | PASS — exit 0, no TS errors | `cd client && npm run build` | `✓ built in 927ms` |

---

## Cycle 3 Re-Review (Codex cross-validation fixes)

**Scope:** Four additional issues raised by Codex cross-validation and fixed prior to this cycle. Cycle 1/2 findings not re-checked — those were confirmed APPROVE in cycle 2. This pass covers only the four new fixes and adjacent call sites.

**Fixes verified:**

| Finding | Fix Applied | Verified |
|---------|-------------|---------|
| HIGH: `formatActivityDate` UTC shift — `new Date('YYYY-MM-DD')` interprets as UTC midnight, shifts day by −1 in negative-offset timezones | Replaced with `const [y, m, d] = isoDate.split('-').map(Number); new Date(y, m - 1, d)` — constructs using local midnight. Comment added. | `EpicTable.vue:94–95` — correct. ISO string is split and passed to the multi-arg `Date` constructor which uses local time. No timezone shift. |
| MEDIUM: Sort column validated against full renderable set (visibility + QA gate) at hydration and toggle | Added `SORTABLE_COLUMNS`, `QA_SORTABLE_COLUMNS` constants and `isSortableColumn(columnId, hiddenCols, hasQaData)` helper. Hydration uses `isSortableColumn(..., false)` (conservative — QA columns invalid until API responds). `toggleColumnVisibility` reads `epicProgress.value?.hasQaData ?? false` and calls `isSortableColumn` after computing `next`. Resets sort to default if no longer sortable. | `epicsStore.ts:13–46` (constants + helper), `epicsStore.ts:106–108` (hydration), `epicsStore.ts:286–293` (toggle). All correct. |
| LOW: `loadSortFromStorage` accepted any string as column id — stale/unknown ids from old app versions would persist | Added `SORTABLE_COLUMNS.has(parsed.column)` to the hydration validation condition. | `epicsStore.ts:29` — correct. Stale column ids now fall back to default. |
| MEDIUM: `transitionsByTicket` rebuilt O(n epics) inside the per-epic loop | Hoisted above `var epicEntries`. Built once; shared across velocity, date derivation, and sprint completion checks. Comment added. | `EpicProgressService.cs:111–114` — correct. Dictionary is now outside the `foreach` loop. |

**Adjacent call sites checked:**

- `isSortableColumn` is called at two sites only (hydration and `toggleColumnVisibility`) — no other callers introduced. The helper correctly gates on `SORTABLE_COLUMNS`, `hiddenCols`, and `QA_SORTABLE_COLUMNS` in that order. Short-circuit is sound.
- The QA-conservative hydration (`hasQaData = false`) means that if a user had `coverageRate` as their sort column from a prior session, it resets to `lastWorkDate` on load and re-sorts once the API response arrives. This is the intended behavior per Fix 2's description — minor UX cost, correct correctness trade-off.
- `transitionsByTicket` hoist: the dictionary key is `TicketId` (OrdinalIgnoreCase). All consumer call sites (`transitionsByTicket.GetValueOrDefault(ticket.Id, [])` and `GetValueOrDefault(sm.TicketId, [])`) are unchanged and use the same case-insensitive lookup. No regressions from the hoist.
- `toggleColumnVisibility` BR11 path: the previous cycle 1 fix (copy-then-reassign) is preserved. The sort reset logic moved from inside the `else` branch to after `hiddenColumns.value = next`, and now uses `isSortableColumn` rather than the narrow `sortColumn.value === columnId` check. This is strictly broader (catches QA gate too) and correct.

**Evidence (cycle 3):**

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Backend build | PASS — 0 `error CS` lines | `dotnet build Fokus.API.csproj \| grep "error CS"` | (no output = zero errors) |
| Frontend build | PASS — exit 0, no TS errors | `cd client && npm run build` | `✓ built in 943ms` |
