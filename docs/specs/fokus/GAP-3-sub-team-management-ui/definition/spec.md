# GAP-3: Sub-Team Management UI

**Reported:** 2026-05-09
**Reporter:** PO (Laurentiu)
**Status:** Open
**Severity:** Medium
**Feature:** F3 — Settings System

---

## What's Missing

A UI for assigning developers to sub-teams. The `SubTeam` field exists on the Developer entity and the sub-team filter dropdown appears on analytics pages (Sprints, Developers), but there is no screen to actually tag developers with sub-team names.

## Spec Reference

- `docs/backlog.md` F3 description: "Developer sub-team tagging lives here"
- `docs/product/v1.md` §4.2 Developer entity: `SubTeam` field described as "manually tagged in app" with "Managed via a simple admin screen"

## User Impact

Sub-team filter dropdowns on analytics pages have no effect because no developers have sub-teams assigned. The filter exists but is unusable.
