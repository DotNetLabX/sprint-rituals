# Questions — F35-EpicViewEnhancements

**Status:** None — all clear, ready to implement.

## Pre-flight verification

All plan steps verified:

| Step | Referenced files | Status |
|------|-----------------|--------|
| 1 | `EpicProgressService.cs` (found), `TransitionAttributionChecker.ResolveStartIndex` (found), `transitionsByTicket` dict at line 170 (found), `EpicProgressEntry` record (found), `activeSprintCount` field (found) | OK |
| 2 | `client/src/types/index.ts` — `EpicProgressEntry` interface (found, `activeSprintCount` after which `startedDate`/`lastWorkDate` will be inserted, before `isCompleted`) | OK |
| 3 | `client/src/stores/epicsStore.ts` (found) — `filteredEpics` computed exists, localStorage not yet used | OK |
| 4 | `client/src/views/EpicsView.vue` (found), `client/src/components/epics/EpicColumnToggle.vue` (does not exist — to be created) | OK |
| 5 | `client/src/components/epics/EpicTable.vue` (found) — `Sprints` column at line 85 uses `activeSprintCount`, colspan at line 210 hardcoded (8 or 11) | OK |
| 6 | `EpicsView.vue` and `EpicSummaryCards.vue` (both found) — summary cards currently accept `summaryMetrics` object, needs prop refactor | OK |
| 7 | `docs/kb/analytics/epic-progress.md` (found) | OK |

## Observations for implementation

- **Step 1 scope note:** The plan says "reuse `transitionsByTicket` dictionary at line 170". That dictionary is currently built inside the `foreach (var group in epicGroups)` loop — it is rebuilt per-epic group. For date derivation, the plan says to iterate `transitionsByTicket` for each epic's tickets. This is consistent with the existing pattern — no architectural change needed, just add date derivation after the dictionary is built within the loop.
- **Step 5 colspan:** The expanded ticket row colspan is currently hardcoded as `8` (no QA) or `11` (with QA). After F35 adds "Started" and "Last Work" columns (replacing "Sprints" = net +1 column), these become `9` and `12`. Column visibility complicates this further — the plan says "update the expanded ticket table colspan to account for visible column count dynamically." This needs to count always-visible columns (chevron + epic name = 2) plus visible optional columns.
- **Step 3 note:** `hiddenColumns` as a `Set<string>` is not directly JSON-serializable via `JSON.stringify`. The plan specifies localStorage key `'fokus-epics-columns'` as `JSON: string array of hidden column IDs` — serialize as `Array.from(hiddenColumns)` and deserialize as `new Set(parsed)`.
- **Known deviation watch:** After Step 1 changes `EpicProgressEntry` record constructor, verify all call sites — there is one in the early-return empty path at `GetEpicProgressEndpoint.cs` line 27-33 and the main one in `EpicProgressService.cs` line 244.
