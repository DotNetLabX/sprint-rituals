# Scope Change & Disruption — Tooltip Help

Sibling of `docs/features/ScopeChangeDisruption/spec.md`. Each section provides the Short variant (tooltip text, under 150 chars, shown on info icon hover).

---

## Summary Metrics (Multi-Sprint)

### Average Disruption Rate

Mean percentage of unplanned work added mid-sprint relative to committed scope, across selected sprints.

### Average Net Scope Change

Mean difference between added and removed SP across selected sprints. Positive means sprints grew.

### Total Bugs Added

Count of bug-type tickets added mid-sprint across all selected sprints.

---

## Scope Change Bar Chart

Per-sprint bars showing committed, added, removed, and completed SP side by side.

---

## Disruption Rate Trend

Disruption rate percentage per sprint over time. Spot sustained increases early.

---

## Classification Breakdown

### Planning Overflow

Items added within the first 2 days — work missed during sprint planning, not true disruption.

### Unplanned Bug

Bug-type tickets added after day 2 of the sprint — reactive quality work consuming planned capacity.

### Scope Injection

New work added after day 2 that didn't exist before the sprint — truly unplanned scope.

### Priority Escalation

Pre-existing tickets pulled into the sprint after day 2 due to changed priorities.

---

## Single-Sprint Metric Cards

### Committed SP (Active / Total)

Story points committed at sprint start. Active excludes tickets with excluded final statuses.

### Added SP

Story points on tickets added after the sprint started (not removed, not excluded).

### Removed SP

Story points on tickets explicitly pulled out of the sprint.

### Net Scope Change

Added SP minus removed SP. Positive means the sprint grew; negative means it shrank.

### Disruption Rate

Added SP as a percentage of active committed SP. Lower is better.

### Bug Count

Number of bug-type tickets added mid-sprint, regardless of story points.

---

## Scope Burnup Chart

Daily scope and completion lines showing when and how the sprint's total work changed.

---

## Event Table

Chronological log of every ticket added to or removed from the sprint after it started.

---

## Bug Time-in-Progress

Calendar days each mid-sprint bug spent in active work statuses — measures capacity consumed.

---

## Sprint Selector

Choose a single sprint for detail view, or a range (Last 3/5/All) for trend analysis.

---

## Sub-Team Filter

Restrict all scope change content to tickets assigned to the selected sub-team's developers.

---

## Excluded-from-Scope Statuses

Statuses (e.g., "To Do") whose tickets are excluded from scope metric calculations.
