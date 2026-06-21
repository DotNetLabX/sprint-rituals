# GAP-1: Excluded from Scope Statuses UI

**Reported:** 2026-05-09
**Reporter:** PO (Laurentiu)
**Status:** Open
**Severity:** High
**Feature:** F10 — Scope Change & Disruption

---

## What's Missing

A Settings section where the user configures a list of statuses (e.g., "To Do", "Blocked") to exclude from scope metrics. The backend endpoints exist (`GET /api/settings/excluded-statuses`, `PUT /api/settings/excluded-statuses`) but no UI section was built in the Settings page.

## Spec Reference

- `docs/features/ScopeChangeDisruption/spec.md` — Entities section ("New: Excluded-from-scope statuses"), Flow 5 ("View with Excluded Statuses Applied"), Acceptance Criteria ("Excluded-from-scope statuses setting is configurable via Settings UI")

## User Impact

Active committed SP always equals Total committed SP on the Sprints page because no statuses can be excluded. The "Committed SP (Total)" card showing "Active: 208 | Total: 208" is correct behavior for an unconfigured exclusion list, but useless without the ability to configure it.
