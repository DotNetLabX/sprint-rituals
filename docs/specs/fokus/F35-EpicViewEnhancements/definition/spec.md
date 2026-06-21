# Epic View Enhancements

**Traces to:** `docs/product/v1.md` §5.6 (Epic Progress), §6.1 (Navigation — Epics)
**Source:** Scratch
**Dependencies:** F14 (Epic Progress — base Epics page), F31 (Epic Test Health — QA columns, summary card)
**Status:** Done
**Plan:** `docs/specs/F35-EpicViewEnhancements/delivery/plan.md`

---

## Purpose

F14's Epics page answers "how's this epic going?" but with 47 active epics the answer is buried. The page has no search, no column sorting, no column customization, and a default sort that puts all-zero epics at the top. The "Sprints" count column says how many sprints an epic has existed in, but not *when* work started or *when* someone last touched it — and it counts raw sprint membership rather than actual work transitions. QA columns consume horizontal space even when empty, and the horizontal scrollbar is only reachable at the bottom of 47 rows, making right-side columns effectively invisible. F35 adds the table interaction layer needed to make the Epics page usable at scale.

## Entities

No new domain entities. No new settings.

**Epic progress response extensions (per epic):**
- Started date (date, nullable) — earliest qualifying status transition past the cycle time start boundary across all tickets in the epic
- Last work date (date, nullable) — most recent qualifying status transition past the cycle time start boundary across all tickets in the epic

**Client-side state (localStorage):**
- Column visibility preferences — which columns are shown or hidden
- Sort state — current sort column and direction

## User Flows

```
Flow 1: Search Epics
1. A search field appears above the epic table
2. User types text — the table filters to epics whose name or key contains the search text (case-insensitive substring match)
3. Summary cards recalculate based on the filtered set (same behavior as when sub-team filter narrows the visible set)
4. Clearing the search restores the full list
5. Search works identically on both Active and Completed tabs
6. Search combines with the sub-team filter — both narrow the visible set independently
```

```
Flow 2: Sort by Column
1. User clicks a sortable column header
2. Table sorts by that column ascending
3. Clicking the same header again reverses to descending
4. Clicking a different header switches to that column ascending
5. An arrow icon on the active header indicates sort direction
6. Default sort (first visit): last work date descending — most recently active epics first
7. Sort state persists in localStorage and applies on return visits
```

```
Flow 3: Customize Visible Columns
1. A gear/settings icon appears in the table toolbar area (near the search field)
2. Clicking it opens a dropdown with a checkbox per column
3. User toggles columns on/off — the table updates immediately
4. Column visibility persists in localStorage across sessions
5. Epic name cannot be hidden — it is always visible
6. QA columns follow the existing F31 gate: not rendered when Xray is disabled, regardless of visibility preference
```

```
Flow 4: View Activity Dates
1. Two date columns appear: "Started" and "Last Work" — replacing the previous "Sprints" count column
2. "Started" shows when the first ticket in the epic transitioned past the cycle time start boundary
3. "Last Work" shows when the most recent such transition occurred across any ticket in the epic
4. Both dates use the cycle time start stage from workflow settings — the same boundary used by velocity and sprint scope attribution
5. Epics with no qualifying transitions show "—" in both columns
6. Dates within the last 30 days display in relative format (e.g., "3d ago", "2w ago"). Dates older than 30 days display in absolute format (e.g., "Jan 15, 2025")
```

```
Flow 5: Horizontal Scroll with Pinned Epic Column
1. When the table is wider than the viewport, the epic name column stays pinned on the left
2. The expand/collapse chevron pins alongside the epic name
3. All other columns scroll horizontally beneath the pinned area
4. A subtle visual separator (shadow or border) distinguishes the pinned column from the scrolling area
```

## API Surface

| Method | Route | Auth | Request body | Response | Status codes |
|--------|-------|------|-------------|----------|-------------|
| GET | /api/analytics/epic-progress | Authenticated | — | Extended EpicProgressResponse | 200 |

**Query parameters (unchanged from F14):**
- `subTeam` (string, optional) — filter to tickets assigned to developers in this sub-team

**Response extensions (new fields per epic entry):**
- `startedDate` — ISO 8601 date, nullable. Earliest qualifying transition past the cycle time start boundary across all epic tickets.
- `lastWorkDate` — ISO 8601 date, nullable. Most recent qualifying transition past the cycle time start boundary across all epic tickets.

**Deprecated fields:**
- `activeSprintCount` — retained in the response for backward compatibility but no longer displayed in the UI. May be removed in a future version.

**Error conditions:** No new error conditions. Null dates when no qualifying transitions exist.

## Business Rules

### Date Derivation

BR1. **Started date.** For each epic, find the earliest status transition across all of the epic's tickets where the transition's target status is at or past the configured cycle time start stage in the ordered stage sequence (workflow stages followed by done statuses). When no qualifying transition exists, the started date is null.

BR2. **Last work date.** For each epic, find the most recent status transition across all of the epic's tickets where the transition's target status is at or past the cycle time start stage (same stage resolution as BR1). When no qualifying transition exists, the last work date is null.

BR3. **Transition data scope.** Date derivation uses the status transitions already loaded for velocity calculation. Epic tickets that have never appeared in any analytics sprint have no transitions in this collection — their contributions are excluded. This is acceptable: an epic ticket that has never been in a sprint has no meaningful "started" date in a sprint-tracking context.

### Search

BR4. **Search matching.** Case-insensitive substring match against epic name and epic key. Client-side filtering — no API changes for search.

BR5. **Summary card recalculation on search.** When search narrows the visible set, three of the four summary cards recalculate client-side based on the filtered epics:
   - **Active Epics** — count of filtered active epics
   - **Average Completion** — weighted average of SP completion % across filtered active epics, weighted by each epic's adjusted total SP
   - **Average Test Coverage** — arithmetic mean of non-null coverage rates across filtered epics (same formula as F31 BR16, scoped to filtered set)
   - **Unlinked Work** — does NOT recalculate on search. Unlinked tickets have no epic name or key to match against. This card remains static during search, reflecting the full (unfiltered) count.

   This differs from the sub-team filter, which triggers a server-side recomputation. Search is client-side only — the frontend computes summary values from the filtered epic array.

BR6. **Search and filter interaction.** Search combines with the sub-team filter and active/completed toggle. All three narrow the visible set independently — epics must match all active filters to appear.

### Sorting

BR7. **Default sort.** On first visit (no saved preference), epics sort by last work date descending. Epics with null last work date sort last.

BR8. **Null sort ordering.** For all sortable columns with nullable values (velocity, projected sprints, started date, last work date, coverage %, pass rate %), null values sort last regardless of ascending or descending direction.

BR9. **Sortable columns.** The following columns support click-to-sort: Epic name (alphabetical), Progress (numeric — sort by SP completion % when available, fall back to ticket completion % when SP data is unavailable), Tickets (numeric — sort by done count), Velocity (numeric), Projected sprints (numeric), Started (date), Last Work (date), Coverage % (numeric), Pass Rate % (numeric), Bugs Found (numeric). The "SP Done / Total" column is not sortable — the Progress column already sorts by completion %, which is more useful than raw SP values.

BR10. **Sort persistence.** Sort column and direction persist in localStorage. Persisted sort takes precedence over the default on subsequent visits.

BR11. **Sort and hidden columns.** If the user hides the column they are currently sorting by, the sort resets to the default (last work date descending).

### Column Visibility

BR12. **Persistence.** Column visibility state persists in localStorage. When no saved preference exists, all columns are visible.

BR13. **Protected column.** The epic name column cannot be hidden — it is always visible and excluded from the toggle list.

BR14. **QA column gate.** QA columns (Coverage %, Pass Rate %, Bugs Found) remain gated by the existing F31 Xray check. When Xray is disabled, QA columns are not rendered regardless of visibility preferences. When Xray is enabled, QA column visibility follows the user's preference.

### Sticky Column

BR15. **Pinned area.** The epic name column and its leading expand/collapse chevron remain fixed on the left during horizontal scroll. All other columns scroll.

BR16. **Visual separator.** A shadow or border appears on the right edge of the pinned column to indicate the scroll boundary.

### Sprints Column Replacement

BR17. **Column replacement.** The "Sprints" count column is removed from the UI and replaced by "Started" and "Last Work" date columns. The underlying API field (`activeSprintCount`) is deprecated but retained for backward compatibility.

## Acceptance Criteria

### Search

- [ ] A search field appears above the epic table
- [ ] Typing filters epics by name or key (case-insensitive substring)
- [ ] Active Epics, Average Completion, and Average Test Coverage cards recalculate based on the filtered set
- [ ] Unlinked Work card does NOT recalculate on search (remains static)
- [ ] Search works on both Active and Completed tabs
- [ ] Search combines with sub-team filter (both narrow independently)
- [ ] Clearing search restores the full list

### Sorting

- [ ] Clicking a column header sorts ascending; clicking again sorts descending
- [ ] An arrow icon indicates the active sort column and direction
- [ ] Default sort is last work date descending (most recently active first)
- [ ] Null values sort last regardless of direction
- [ ] Sort state persists in localStorage across sessions
- [ ] All columns listed in BR9 are sortable

### Column Visibility

- [ ] A gear/settings icon opens a column toggle dropdown
- [ ] Toggling a column immediately shows/hides it in the table
- [ ] Column visibility persists in localStorage
- [ ] Epic name column cannot be hidden
- [ ] QA columns respect the existing F31 Xray gate independently of visibility preferences
- [ ] Hiding the currently sorted column resets sort to default

### Activity Dates

- [ ] "Started" column shows the date of the first qualifying transition past the cycle time start boundary
- [ ] "Last Work" column shows the date of the most recent qualifying transition past the cycle time start boundary
- [ ] Both dates use the cycle time start stage from workflow settings
- [ ] Epics with no qualifying transitions show "—"
- [ ] Dates within 30 days display in relative format (e.g., "3d ago"), older dates in absolute format (e.g., "Jan 15, 2025")
- [ ] The previous "Sprints" count column is removed from the UI

### Sticky Column

- [ ] Epic name column stays fixed on the left during horizontal scroll
- [ ] The expand/collapse chevron pins alongside the epic name
- [ ] A visual separator distinguishes the pinned area from scrolling columns

### API

- [ ] GET /api/analytics/epic-progress response includes `startedDate` and `lastWorkDate` per epic
- [ ] Both fields are null when no qualifying transitions exist
- [ ] `activeSprintCount` remains in the response (deprecated, not removed)
- [ ] Sub-team filter scopes date derivation (only transitions on filtered tickets)

## Out of Scope

- **Advanced filtering (by date range, velocity threshold, completion range)** — search by name/key is the minimum viable improvement. Structured filters can follow if needed.
- **Multi-column sort** — no competitor tool supports this on epic views. Single-column sort with click-to-switch covers all use cases.
- **Column reordering (drag to rearrange)** — adds complexity without clear value. Column visibility is sufficient.
- **Server-side search** — with fewer than 100 epics expected, client-side filtering is sufficient and avoids API changes.
- **Timeline / Gantt visualization** — competitors use timeline bars for epic date ranges. This is a separate visualization mode, not a table enhancement.
- **Epic health score / attention flags** — automatically flagging stale or troubled epics is a separate feature that builds on the data F35 surfaces.

## Open Questions

None — all resolved during discussion.
