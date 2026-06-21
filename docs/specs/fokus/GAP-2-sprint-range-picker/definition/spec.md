# GAP-2: Sprint Range Picker for Sync

**Reported:** 2026-05-09
**Reporter:** PO (Laurentiu)
**Status:** Open
**Severity:** Medium
**Feature:** F5 — Sprint Sync (JiraSync)

---

## What's Missing

From/To sprint dropdown selectors on the sync UI. The Settings page only shows a "Sync All" button. Users cannot sync a single sprint, a custom range, or re-sync a suspect sprint without pulling everything.

## Spec Reference

- `docs/features/JiraSync/spec.md` — Flow 2 (Initial Backfill: "Sprint range picker shown: two dropdowns From/To"), Flow 3 (Ongoing Sprint Sync: "User selects the just-closed sprint"), Flow 5 (Re-sync: "User selects that sprint in both From and To"). API endpoint `POST /api/sync/sprints` accepts `{ fromSprintId, toSprintId }`.

## User Impact

Every sync pulls all sprints from Jira, which is slow and wasteful. No way to quickly sync just the latest closed sprint or re-sync a single suspect sprint.
