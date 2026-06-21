# BUG-2: Bar Chart Completed SP and Burnup Completed SP Disagree

**Reported:** 2026-05-10
**Reporter:** PO (Laurentiu)
**Status:** Open
**Severity:** High
**Feature:** F10 — Scope Change & Disruption / F21 — Feature-Only Burnup

---

## What's Wrong

For Sprint 26, the Scope Change bar chart shows Completed SP = 202, while the Scope Burnup chart shows Completed SP = 115. Even accounting for bugs (24 SP in the burnup tooltip), 115 + 24 = 139 — still 63 SP short of 202. The gap is not explained by scope type differences alone.

## Expected Behavior

Completed SP values should be reconcilable between the two charts. If they use different scope definitions (total vs. feature-only), the delta should equal exactly the bug SP. A 63 SP unexplained gap is a computation error.

## Root Cause Hypothesis

The two charts use different computation paths:
- Bar chart (ScopeChangeService): likely uses `FinalStatus` — the ticket's current status at query time.
- Burnup (SprintSummaryService): tracks transitions within the sprint window — a ticket must have transitioned to Done *during* the sprint to count.

A ticket completed after sprint end (or whose Done transition falls outside the window) would appear in the bar chart but not the burnup, explaining the gap.

## Investigation Hints

- Compare the set of tickets counted as "Completed" by each service for Sprint 26.
- Check whether tickets with Done status but no in-sprint Done transition exist in the data.
- Verify which service is correct per spec: the burnup's within-window approach or the bar chart's current-status approach.

## Spec Reference

- `docs/features/ScopeChangeDisruption/spec.md` — Completed SP definition
- `docs/kb/analytics/scope-change.md` — computation logic
- `docs/kb/analytics/throughput.md` — completion criteria
