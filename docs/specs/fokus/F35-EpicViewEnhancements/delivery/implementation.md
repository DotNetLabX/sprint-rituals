# Implementation — F35-EpicViewEnhancements

**Status:** Complete
**Branch:** main

## Steps

### Step 1: Extend EpicProgressService with date derivation and response fields

**Status:** Done

**Files Modified:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/EpicProgressService.cs`
  - Changed `var (orderedStages, _)` to `var (orderedStages, startIndex)` — was discarding `startIndex` which is needed for date derivation
  - Added `DateOnly? StartedDate` and `DateOnly? LastWorkDate` to `EpicProgressEntry` record after `ActiveSprintCount`, before `IsCompleted`
  - Added date derivation block inside per-epic loop after `transitionsByTicket` dictionary is built: iterates all transitions for epic tickets, qualifies via `TransitionAttributionChecker.GetStageIndex >= startIndex`, computes earliest (startedDate) and most recent (lastWorkDate) as `DateOnly`
  - Updated `epicEntries.Add(new EpicProgressEntry(...))` constructor call to include `startedDate` and `lastWorkDate`

**Notes:**
- Early-return call site in `GetEpicProgressEndpoint.cs` did not need updating — it passes an empty `List<EpicProgressEntry>` (`[]`), not individual `EpicProgressEntry` constructors
- The only C# compiler errors at build time were MSB3026/MSB3027 file-lock errors (app was running); zero `error CS` errors confirmed

---

### Step 2: Update frontend types and API module

**Status:** Done

**Files Modified:**
- `client/src/types/index.ts`
  - Added `startedDate: string | null` and `lastWorkDate: string | null` to `EpicProgressEntry` interface after `activeSprintCount`, before `isCompleted`

---

### Step 3: Extend epicsStore with search, sort, and column visibility state

**Status:** Done

**Files Modified:**
- `client/src/stores/epicsStore.ts` — full rewrite with:
  - `searchQuery` ref (default `''`)
  - `sortColumn` ref (default `'lastWorkDate'`), `sortDirection` ref (default `'desc'`)
  - `hiddenColumns` ref (Set<string>, default empty) — hydrated from localStorage on store init
  - localStorage helpers: `loadSortFromStorage()` and `loadHiddenColumnsFromStorage()` — module-level functions; parse errors are silently caught
  - BR11 enforced at hydration: if stored sort column is in hiddenColumns, reset to defaults
  - `searchFilteredEpics` computed: case-insensitive substring match on `epicName`/`epicKey` applied to `filteredEpics`
  - `sortedEpics` computed: sorts `searchFilteredEpics` with null-last logic for all nullable columns (BR8); `getEpicSortValue()` helper maps column IDs to values
  - `searchSummaryMetrics` computed: recalculates `activeEpicCount`, `averageCompletion` (weighted by adjustedTotalSp), `averageTestCoverage` (arithmetic mean of non-null coverageRate) from search-filtered set. Unlinked Work not recalculated (BR5)
  - Actions: `setSearchQuery`, `toggleSort` (flip direction or switch column, persist), `toggleColumnVisibility` (add/remove, reset sort if hiding sorted column per BR11, persist), `isColumnVisible`
  - `Set<string>` serialized as `Array.from()` / deserialized as `new Set()` for localStorage compatibility

**Deviation:** Removed standalone `averageTestCoverage` computed (previously exported, now dead code since `EpicsView` uses `searchSummaryMetrics.averageTestCoverage`). Cleaned up in boy scout pass.

---

### Step 4: Add search field and column visibility toggle to EpicsView

**Status:** Done

**Files Created:**
- `client/src/components/epics/EpicColumnToggle.vue`
  - L3 humble component: receives `columns: ColumnDef[]`, `hiddenColumns: Set<string>`, `hasQaData: boolean`; emits `toggle(columnId)`
  - Gear icon button opens a dropdown with checkboxes per column
  - QA columns (`coverageRate`, `passRate`, `bugsFound`) filtered out of list when `hasQaData=false` (BR14)
  - Click-outside overlay closes the dropdown

**Files Modified:**
- `client/src/views/EpicsView.vue`
  - Added `EpicColumnToggle` import and `COLUMN_DEFS` constant (10 columns, epic name excluded per BR13)
  - Replaced toolbar row: search input + `EpicColumnToggle` + `EpicActiveCompletedToggle` now share a flex row
  - `EpicSummaryCards` now receives individual values from `store.searchSummaryMetrics` (not raw `summaryMetrics` object)
  - `EpicTable` now receives `sortColumn`, `sortDirection`, `isColumnVisible`, emits `sort`; data source changed from `filteredEpics` to `sortedEpics`
  - Added search-specific empty state message when `store.searchQuery` is set

---

### Step 5: Rework EpicTable with sortable headers, sticky column, and date columns

**Status:** Done

**Files Modified:**
- `client/src/components/epics/EpicTable.vue` — full rewrite with:
  - New props: `sortColumn`, `sortDirection`, `isColumnVisible: (columnId: string) => boolean`
  - New emit: `sort(columnId: string)`
  - **Sticky column:** chevron `<th>`/`<td>` at `left: 0`, epic name `<th>`/`<td>` at `left: 1.5rem` — both use `position: sticky; z-index: 10; bg-surface-default`. Box-shadow `2px 0 4px -1px rgba(0,0,0,0.15)` on epic name cell is the BR16 visual separator
  - **Sortable headers:** click handler emits `sort(columnId)`. Active column shows up/down arrow SVG via `sortIcon()` helper. "SP Done / Total" is not sortable (no click handler)
  - **Column visibility:** all optional columns have `v-if="isColumnVisible(columnId)"` on both `<th>` and `<td>`; QA columns additionally gated by `hasQaData`
  - **Column replacement:** removed "Sprints" `<th>`/`<td>` (previously showed `activeSprintCount`); added "Started" and "Last Work" date columns in same position
  - **Date formatting:** `formatActivityDate(isoDate)` helper — relative format within 30 days (`Xd ago`, `Xw ago`, `Today`), absolute (`Jan 15, 2025`) beyond 30 days, `'—'` for null
  - **Dynamic colspan:** `visibleColspan` computed counts always-visible columns (2) plus visible optional columns, respecting `hasQaData` gate for QA columns

---

### Step 6: Wire summary card recalculation for search

**Status:** Done

**Files Modified:**
- `client/src/components/epics/EpicSummaryCards.vue`
  - Props changed from `summaryMetrics: EpicProgressSummaryMetrics` + `averageTestCoverage: number | null` to individual props: `activeEpicCount: number`, `averageCompletion: number`, `averageTestCoverage: number | null`
  - Removed `EpicProgressSummaryMetrics` type import (no longer needed)
  - Template updated: `activeEpicCount` replaces conditional `summaryMetrics.activeEpicCount`/`summaryMetrics.completedEpicCount`; `averageCompletion` replaces `summaryMetrics.averageCompletionPercentage` (active/completed conditional now handled in store's `searchSummaryMetrics`)
  - Unlinked Work card unchanged (still receives `unlinkedWork` directly — BR5)

---

### Step 7: Update KB entries

**Status:** Done

**Files Modified:**
- `docs/kb/analytics/epic-progress.md`
  - Added "## Activity Dates (F35)" section: derivation rule, qualifying transition logic, sub-team filter scope, null conditions, API serialization, `activeSprintCount` deprecation note
  - Updated "## Sort and Display" to "## Sort and Display (updated F35)": documented new default sort (lastWorkDate desc), sortable columns (BR9), null-last convention (BR8)
  - Added F35 additions note to "## Per-Epic Metrics": startedDate/lastWorkDate as computed fields
  - Added "## Client-Side Interactions (F35)" section: search behavior, summary card recalculation rules (BR5), localStorage keys and defaults, sticky column implementation, column IDs reference

## Deviations

| Deviation | Reason |
|-----------|--------|
| Removed standalone `averageTestCoverage` export from epicsStore | Dead code — `EpicsView` now uses `searchSummaryMetrics.averageTestCoverage`. Cleaned in boy scout pass after Step 6. |

## Codex Fix Round

### Fix 1 — HIGH: `formatActivityDate` parses ISO date as local time

**File:** `client/src/components/epics/EpicTable.vue`

Replaced `new Date(isoDate)` with `const [y, m, d] = isoDate.split('-').map(Number); new Date(y, m - 1, d)`. `new Date('YYYY-MM-DD')` interprets the string as UTC midnight; in negative-offset timezones this shifts the displayed day one day earlier. The split constructor uses local midnight.

### Fix 2 — MEDIUM: sort column validated against full renderable set at hydration and toggle

**File:** `client/src/stores/epicsStore.ts`

Added `SORTABLE_COLUMNS` (all sortable column ids) and `QA_SORTABLE_COLUMNS` (QA-gated subset) constants, and an `isSortableColumn(columnId, hiddenCols, hasQaData)` helper. Applied at two call sites:

- **Hydration:** replaced `savedHiddenColumns.has(savedSort.column)` with `isSortableColumn(...)`. At hydration `hasQaData` is unknown so QA columns are conservatively treated as invalid (they become available once the API response arrives).
- **`toggleColumnVisibility`:** reads `epicProgress.value?.hasQaData` after computing `next` and calls `isSortableColumn(sortColumn.value, next, hasQaData)`. Resets sort to default if the current sort column is no longer renderable/sortable under the full gate. Broader than the previous check, which only reset when hiding the exact sorted column.

### Fix 3 — LOW: `loadSortFromStorage` validates column id against allowlist

**File:** `client/src/stores/epicsStore.ts`

Added `SORTABLE_COLUMNS.has(parsed.column)` guard to the hydration condition. Rejects persisted values that are no longer valid column ids (e.g. stale keys from a previous app version), falling back to default.

### Fix 4 — MEDIUM: `transitionsByTicket` hoisted above `foreach` loop

**File:** `src/Services/Fokus/Fokus.API/Features/Analytics/EpicProgressService.cs`

Moved the `statusTransitions.GroupBy(...).ToDictionary(...)` call from inside the per-epic loop to above `var epicEntries`. The dictionary is built once for all epics instead of O(n) times. Added comment clarifying it is shared across velocity, date derivation, and sprint completion checks.

Both backend (zero `error CS`) and frontend (`npm run build`, exit 0) verified clean after all four fixes.

## Reviewer Fix Round 1

### Fix 1 — CRITICAL: Set reactivity in `toggleColumnVisibility`

**File:** `client/src/stores/epicsStore.ts`

Changed `toggleColumnVisibility` to copy-then-reassign: creates `const next = new Set(hiddenColumns.value)`, mutates `next`, then assigns `hiddenColumns.value = next`. This triggers Vue's ref reactivity and causes downstream consumers (`EpicColumnToggle` checkboxes, `EpicTable` v-if bindings) to re-render correctly.

### Fix 2 — HIGH: `startIndex < 0` guard in date derivation loop

**File:** `src/Services/Fokus/Fokus.API/Features/Analytics/EpicProgressService.cs`

Wrapped the date derivation `foreach` in `if (startIndex >= 0)`. When no cycle time start stage is configured, `ResolveStartIndex` returns `-1` and `GetStageIndex` also returns `-1` for unrecognised statuses — without the guard, every unknown-status transition would satisfy `-1 >= -1` and pollute the date results. Pattern matches the sibling guard in `IsStartedInSprint`.

### Fix 3 — MEDIUM: ISO 8601 sort comment in `getEpicSortValue`

**File:** `client/src/stores/epicsStore.ts`

Added inline comment above the `startedDate`/`lastWorkDate` cases documenting that these return ISO 8601 strings (`YYYY-MM-DD`) whose lexicographic order equals chronological order, making `localeCompare` in `sortedEpics` produce correct date sorting.

Both backend (`dotnet build`, zero `error CS`) and frontend (`npm run build`, exit 0) verified clean after fixes.

## Carry-Over

- The `EpicColumnToggle` dropdown closes via a fixed-position click-outside overlay (`<div class="fixed inset-0 z-10">`). This is the same approach used elsewhere in the codebase for lightweight dropdowns and avoids adding a composable for a single-use case.
- The `formatActivityDate` helper lives inline in `EpicTable.vue`. If another component needs date formatting in relative/absolute hybrid format, consider extracting to a composable at that time.
- `hiddenColumns` Set reactivity carry-over resolved by Fix 1 above. The `isColumnVisible` function prop pattern (passing a plain function to `EpicTable` rather than the `hiddenColumns` ref) still works correctly: after Set reassignment, `EpicsView` re-renders and re-passes the function prop, causing `EpicTable` to re-render. Noted in lessons.md as a pattern consideration.
