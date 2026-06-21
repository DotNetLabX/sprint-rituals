# Planning-Gated Disruption — Tooltip Help

Sibling of `docs/features/PlanningGatedDisruption/spec.md`. Each section provides the Short variant (tooltip text, under 150 chars, shown on info icon hover).

---

## Single-Sprint Metric Cards

### Committed SP (Total)

Feature story points in the sprint when the planning window closed — what the team committed to going into execution.

### Added SP

Feature story points added after planning ended that entered the work cycle. Measures real disruption, not planning adjustments.

### Removed SP

Feature story points removed after planning ended that had entered the work cycle. Only counts started work that was pulled.

### Net Scope Change

Added SP minus Removed SP, both measured after planning ended. Positive means execution scope grew.

### Disruption Rate

Post-planning Added SP as a percentage of Active SP. Only counts additions that entered the cycle.

---

## Classification Breakdown

### Unplanned Bug

Bug-type tickets added after the planning window that entered the work cycle — reactive quality work.

### Priority Escalation

Pre-existing tickets (created before sprint start) pulled in after the planning window — reprioritized work.

### Scope Injection

New work added after the planning window that entered the cycle — truly unplanned scope.

---

## Planning Window

The number of days after sprint start reserved for planning adjustments (configurable in Settings). Disruption metrics only count activity after this window closes.
