# GAP-4: Sprint Page Cards Show Total-Scope but Burnup Shows Feature-Only Scope

**Reported:** 2026-05-10
**Reporter:** PO (Laurentiu)
**Status:** Open
**Severity:** Medium
**Feature:** F21 — Feature-Only Burnup

---

## What's Missing

The Committed SP cards at the top of the Sprint 26 detail page show total-scope values (223/226, including bugs), while the Scope Burnup chart below them shows feature-only scope (Scope SP = 201). There is no annotation or label explaining the discrepancy, so the two numbers cannot be reconciled from the UI alone.

## Expected Behavior

Either:
- The Committed SP cards should be feature-only to match the burnup (preferred for consistency), or
- The cards should annotate the bug portion (e.g., "223 features + 24 bugs = 247 total") so the user can reconcile them visually.

The current state — two different scope definitions on the same page with no explanation — is confusing.

## Root Cause

F21 made the burnup feature-only but did not adjust the scope metric cards above it. The cards still use total-scope computation.

## Spec Reference

- `docs/features/BoundaryDrivenCompletion/spec.md` — feature-only burnup scope definition
