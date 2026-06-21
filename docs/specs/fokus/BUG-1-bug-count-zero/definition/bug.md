# BUG-1: Sprint 26 Bug Count Shows 0 Despite Mid-Sprint Bug Additions

**Reported:** 2026-05-09
**Reporter:** PO (Laurentiu)
**Status:** Open
**Severity:** High
**Feature:** F10 — Scope Change & Disruption

---

## What's Wrong

The Bug Count metric card on the Sprints page shows 0 for Sprint 26. The user confirms bugs were definitely added mid-sprint — a count of zero is incorrect.

## Expected Behavior

Bug count tracks the number of bug-type tickets added mid-sprint regardless of whether they have story points. A ticket is "added mid-sprint" when `WasCommitted = false` (added after sprint start date). Issue type = "Bug" determines the count.

## Spec Reference

- `docs/features/ScopeChangeDisruption/spec.md` — Business Rule 8: "Bug count tracks the number of bug-type tickets added mid-sprint regardless of whether they have story points."

## Investigation Hints

- Is `WasCommitted` being derived correctly from the Jira changelog? If bugs added mid-sprint are incorrectly marked as committed, they won't count.
- Is the issue type matching correct? Jira may use a different casing or name (e.g., "bug" vs "Bug", or a custom issue type).
- Are the bugs being synced at all? Check if the bug tickets appear in SprintMembership for Sprint 26.
