# Epic View Enhancements

**Feature Spec:** `docs/specs/F35-EpicViewEnhancements/definition/spec.md`

## Context

The Epics page (F14) displays all epics in a flat table but provides no search, sorting, column customization, or activity dates. With 47 active epics the page is unusable at scale — relevant epics are buried, QA columns waste space when empty, and the horizontal scrollbar is only reachable after scrolling past all rows. F35 adds the table interaction layer (search, sort, column visibility, sticky column) and replaces the uninformative "Sprints" count with meaningful "Started" and "Last Work" date columns derived from transition data.

**Services impacted:** Fokus (single service). Backend: extend `EpicProgressService` + response records with two new date fields. Frontend: major rework of `EpicTable.vue` (sort, sticky column), new search and column-visibility UI in `EpicsView.vue`, store extensions for client-side search, sort, and column persistence.

## Scope

**In scope:**
- Backend: Compute `startedDate` and `lastWorkDate` per epic using existing transition data
- Backend: Add both fields to `EpicProgressEntry` response record
- Backend: Deprecate `activeSprintCount` (retain in response, stop displaying)
- Frontend: Search field with client-side filtering and summary card recalculation
- Frontend: Click-to-sort on all specified columns with localStorage persistence
- Frontend: Column visibility toggle with localStorage persistence
- Frontend: Sticky epic name column with visual separator
- Frontend: Replace "Sprints" column with "Started" and "Last Work" date columns
- Frontend: Relative date formatting (within 30 days) vs absolute

**Out of scope:** Advanced filtering, multi-column sort, column reordering, server-side search, timeline/Gantt visualization, epic health scores.

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | analytics-computation-service | Follow | Date derivation logic in `EpicProgressService`, response record extension | |
| 2 | (none) | -- | Frontend types + API module update (trivial additions) | |
| 3 | pinia-patterns | Follow | Store extensions: search, sort, column visibility, localStorage, computed recalculation | |
| 4 | vue-patterns, vue-component-architecture | Follow | Search + column visibility toolbar UI | |
| 5 | vue-patterns, vue-component-architecture | Follow | EpicTable rework: sortable headers, sticky column, date columns | |
| 6 | (none) | -- | Summary card recalculation logic (client-side computed) | |
| 7 | (none) | -- | KB update | |

## Domain Model Changes

None. No new entities, value objects, or domain events.

## Data Model Changes

None. No new tables, columns, or migrations. The date fields are computed per-request from existing `StatusTransition` data.

## Implementation Steps

### Step 1: Extend EpicProgressService with date derivation and response fields

Compute `startedDate` and `lastWorkDate` per epic from the status transitions already loaded by `GetEpicProgressEndpoint`. Add both fields to the `EpicProgressEntry` response record.

**Follow analytics-computation-service.**

**Files:**
- Modify: `src/Services/Fokus/Fokus.API/Features/Analytics/EpicProgressService.cs`

**Feature-specific inputs:**
- New fields on `EpicProgressEntry` record: `DateOnly? StartedDate` and `DateOnly? LastWorkDate` (positioned after `ActiveSprintCount`, before `IsCompleted`)
- Date derivation uses the same `orderedStages` and `startIndex` already resolved at line 93-94 of the service (via `TransitionAttributionChecker.ResolveStartIndex`)
- For each epic group, iterate all transitions for the epic's tickets. A transition qualifies if `TransitionAttributionChecker.GetStageIndex(transition.ToStatus, orderedStages) >= startIndex`
- `StartedDate` = earliest qualifying `transition.Timestamp` across all epic tickets (DateOnly from DateTime). Null if no qualifying transitions.
- `LastWorkDate` = most recent qualifying `transition.Timestamp` across all epic tickets (DateOnly from DateTime). Null if no qualifying transitions.
- The `transitionsByTicket` dictionary is already built at line 170 — reuse it for date derivation
- Sub-team filter scope: date derivation operates on `filteredEpicTickets` (same as all other per-epic metrics), so only transitions belonging to filtered tickets contribute

**Accept:**
- `GET /api/analytics/epic-progress` response includes `startedDate` and `lastWorkDate` per epic entry
- Both are null when no qualifying transitions exist for the epic's tickets
- Both respect the sub-team filter (only transitions on filtered tickets count)
- `activeSprintCount` remains in the response (unchanged, deprecated)
- `dotnet build` passes

### Step 2: Update frontend types and API module

Add the new response fields to the TypeScript interface. No API function changes (same endpoint, same parameters).

**Files:**
- Modify: `client/src/types/index.ts`

**Feature-specific inputs:**
- Add to `EpicProgressEntry` interface: `startedDate: string | null` and `lastWorkDate: string | null` (ISO 8601 date strings)
- Position after `activeSprintCount`, before `isCompleted`

**Accept:**
- TypeScript interface matches the API response shape
- `npm run build` passes

### Step 3: Extend epicsStore with search, sort, and column visibility state

Add client-side search, sort, and column visibility to the Pinia store. All state persisted in localStorage. Computed properties for filtered+sorted epics and recalculated summary metrics.

**Follow pinia-patterns.**

**Files:**
- Modify: `client/src/stores/epicsStore.ts`

**Feature-specific inputs:**
- New reactive state: `searchQuery` (string, default ''), `sortColumn` (string, default 'lastWorkDate'), `sortDirection` ('asc' | 'desc', default 'desc'), `hiddenColumns` (Set<string>, default empty)
- localStorage keys: `'fokus-epics-sort'` (JSON: `{column, direction}`), `'fokus-epics-columns'` (JSON: string array of hidden column IDs)
- On store init: read localStorage, hydrate sort and column state. If stored sort column is in hiddenColumns, reset to default (BR11).
- New computed `searchFilteredEpics`: applies `searchQuery` against `epicName` and `epicKey` (case-insensitive substring). Operates on `filteredEpics` (already filtered by active/completed).
- New computed `sortedEpics`: sorts `searchFilteredEpics` by `sortColumn` + `sortDirection`. Null values sort last regardless of direction (BR8). Column-to-value mapping covers all BR9 columns. Progress column sort key: use `spCompletionPercentage` when non-null, fall back to `ticketCompletionPercentage` when SP data is unavailable (BR9).
- New computed `searchSummaryMetrics`: recalculates Active Epics count, Average Completion (weighted by adjustedTotalSp), and Average Test Coverage (arithmetic mean of non-null coverageRate) from the search-filtered active epics. Unlinked Work card is NOT recalculated (BR5 — remains from full response).
- Actions: `setSearchQuery(query)`, `toggleSort(column)`, `toggleColumnVisibility(columnId)`, `isColumnVisible(columnId): boolean`
- `toggleSort`: if same column, flip direction; if different column, set ascending. Persist to localStorage.
- `toggleColumnVisibility`: add/remove from hiddenColumns. If hiding the current sort column, reset sort to default. Persist to localStorage.
- Export `sortedEpics` (replaces `filteredEpics` as the table data source), `searchSummaryMetrics`, `searchQuery`, `sortColumn`, `sortDirection`, `hiddenColumns`

**Accept:**
- Search narrows visible epics by name/key substring
- Sort state persists across page refreshes
- Column visibility state persists across page refreshes
- Hiding sorted column resets to last-work-date descending
- Summary metrics recalculate on search (except Unlinked Work)
- `npm run build` passes

### Step 4: Add search field and column visibility toggle to EpicsView

Add a search input and a column visibility dropdown to the toolbar area above the epic table.

**Follow vue-patterns, vue-component-architecture.**

**Files:**
- Modify: `client/src/views/EpicsView.vue`
- Create: `client/src/components/epics/EpicColumnToggle.vue`

**Feature-specific inputs:**
- Search field: text input with placeholder "Search epics...", bound to `store.setSearchQuery`. Positioned between the toolbar and the active/completed toggle (or inline with the toggle row).
- Column toggle: gear icon button that opens a dropdown with checkboxes per column. Column IDs and labels matching BR9 (Epic name excluded — BR13). QA columns only shown in the list when `hasQaData` (BR14).
- `EpicColumnToggle` receives: column definitions array, hiddenColumns set, hasQaData boolean. Emits: `toggle(columnId)`.
- Summary card prop wiring deferred to Step 6. This step focuses on the search input and column toggle UI only.

**Accept:**
- Search field visible above the table, filters on keystroke
- Column toggle gear icon opens dropdown with all non-protected columns
- QA columns not shown in toggle when Xray disabled
- Summary cards reflect search-filtered values (except Unlinked Work)
- `npm run build` passes

### Step 5: Rework EpicTable with sortable headers, sticky column, and date columns

Rebuild the table with: sortable column headers (click handler + arrow indicator), sticky first column (epic name + chevron), and replace "Sprints" column with "Started" and "Last Work" date columns.

**Follow vue-patterns, vue-component-architecture.**

**Files:**
- Modify: `client/src/components/epics/EpicTable.vue`

**Feature-specific inputs:**
- **Sortable headers:** Each `<th>` for sortable columns (BR9: Epic name, Progress, Tickets, Velocity, Projected, Started, Last Work, Coverage %, Pass Rate %, Bugs Found) gets a click handler emitting `sort(columnId)`. Active sort column shows an up/down arrow SVG indicating direction. "SP Done / Total" is NOT sortable.
- **Sticky column:** The chevron `<td>` and epic name `<td>` use `position: sticky; left: 0; z-index: 10` (chevron at left:0, name offset by chevron width). Background color must be set explicitly (not transparent) so scrolling content doesn't show through. A right shadow/border on the name cell indicates scroll boundary (BR16). The table container uses `overflow-x: auto`.
- **Column replacement:** Remove the "Sprints" `<th>` and `<td>`. Add "Started" and "Last Work" date columns in its position.
- **Date formatting:** Dates within 30 days of today display as relative ("3d ago", "2w ago"). Dates older than 30 days display as absolute ("Jan 15, 2025"). Null dates display as "—" (em-dash, matching spec convention). Create a local `formatActivityDate(isoDate: string | null): string` helper (or a shared composable if preferred).
- **Column visibility:** Each column `<th>` and `<td>` uses `v-if="isColumnVisible(columnId)"`. Chevron and Epic name columns are always visible (no v-if).
- **Props update:** Add `sortColumn`, `sortDirection`, `hiddenColumns`, `isColumnVisible` function. Add emit for `sort(columnId)`.
- **Colspan for expanded row:** Update the expanded ticket table `colspan` to account for visible column count dynamically.

**Accept:**
- Clicking a sortable header emits sort event, arrow icon indicates direction
- Epic name column + chevron stay fixed on horizontal scroll
- Shadow visible on right edge of pinned column
- "Sprints" column removed, "Started" and "Last Work" columns present
- Dates display in relative format within 30 days, absolute beyond
- Null dates show "--"
- Hidden columns not rendered in DOM
- `npm run build` passes

### Step 6: Wire summary card recalculation for search

Connect the store's search-aware computed metrics to the summary cards component so they recalculate when search narrows the visible set.

**Files:**
- Modify: `client/src/views/EpicsView.vue`
- Modify: `client/src/components/epics/EpicSummaryCards.vue`

**Feature-specific inputs:**
- `EpicSummaryCards` props update: accept individual metric values (activeEpicCount, averageCompletion, averageTestCoverage) from the store's search-computed values, rather than the raw `summaryMetrics` object from the API response.
- Unlinked Work card continues to receive `epicProgress.unlinkedWork` directly (BR5: not affected by search).
- When `activeFilter === 'completed'`, the Active Epics card shows the count of search-filtered completed epics, and Average Completion shows 100%.
- Average Test Coverage recomputes from search-filtered epics (arithmetic mean of non-null `coverageRate` values).

**Accept:**
- Typing in search updates all summary cards except Unlinked Work
- Clearing search restores full-set values
- Sub-team filter still triggers server-side re-fetch (existing behavior unchanged)
- Search combines with active/completed toggle
- `npm run build` passes

### Step 7: Update KB entries

Update the Epic Progress KB entry to document the new date fields, column changes, and client-side interaction layer.

**Files:**
- Modify: `docs/kb/analytics/epic-progress.md`

**Feature-specific inputs:**
- Add section "## Activity Dates (F35)" documenting: startedDate/lastWorkDate derivation, qualifying transition rule (stage index >= startIndex), null conditions, sub-team filter scope
- Update "## Sort and Display" section: document new default sort (lastWorkDate desc), sortable columns, null-last convention
- Add section "## Client-Side Interactions (F35)" documenting: search (client-side substring match on epicName/epicKey), column visibility (localStorage), sticky column
- Note "Sprints" column replaced by Started/Last Work in UI (API field `activeSprintCount` deprecated but retained)
- Update the `Per-Epic Metrics` section or add to it: `startedDate` and `lastWorkDate` as computed fields

**Accept:**
- KB entry accurately reflects the new F35 behavior
- No existing sections removed or truncated (KB hard rule)

## Testing Strategy

- **Backend:** Verify `startedDate`/`lastWorkDate` computation with epics that have: (a) multiple tickets with transitions, (b) no qualifying transitions (null result), (c) mixed tickets where some have transitions and some don't. Verify sub-team filter scopes date derivation correctly.
- **Frontend search:** Type partial epic name, verify table filters and summary cards update. Clear search, verify restoration. Combine with sub-team filter.
- **Frontend sort:** Click each sortable column, verify ascending then descending. Verify null-last behavior. Refresh page, verify persisted sort applies.
- **Frontend column visibility:** Toggle columns off/on, verify persistence. Hide sorted column, verify sort resets. Toggle QA columns with Xray on/off.
- **Frontend sticky column:** Scroll horizontally, verify epic name stays pinned with shadow separator.
- **Frontend dates:** Verify relative format for recent dates, absolute for older dates, "--" for null.

## KB Impact

- `docs/kb/analytics/epic-progress.md` — add activity dates derivation, update sort/display, add client-side interactions section (covered by Step 7)

## Open Questions

None.
