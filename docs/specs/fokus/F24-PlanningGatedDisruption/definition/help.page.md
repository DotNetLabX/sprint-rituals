# Planning-Gated Disruption — Guide Page Help

Sibling of `docs/features/PlanningGatedDisruption/spec.md`. Each section provides the Long variant (guide page paragraph explaining interpretation and recommended actions).

---

## Single-Sprint Metric Cards

### Committed SP (Total)

Total shows the feature story points that were in the sprint at the moment the planning window closed — the team's commitment going into execution. This is a pure membership count: every feature ticket present at that point, regardless of whether it was later started, completed, or removed. It matches the dashed line's value in the burnup chart on the planning cutoff day. Compare Total with Active to see how much of the plan was never started (the over-commitment gap), and with Completed to see the full delivery funnel.

### Added SP

Added SP measures genuine mid-sprint disruption: feature tickets that were added to the sprint after the planning window closed AND that entered the active work cycle (transitioned to the cycle time start stage). Tickets reshuffled during planning don't count — that's planning, not disruption. Tickets added post-planning but never started don't count either — if nobody worked on it, it didn't disrupt anyone. This two-part filter (post-planning + cycle-entered) means the number you see corresponds to real capacity consumed by unplanned work. A rising trend across sprints signals that external forces are routinely overriding sprint commitments.

### Removed SP

Removed SP captures started work that was pulled from the team after planning ended. A ticket only counts here if it was removed after the planning window closed AND had already entered the work cycle (transitioned to the cycle time start stage before removal). Removing an unstarted ticket from the sprint is cleanup, not disruption — it doesn't appear here but is visible as a dip in the burnup chart's dashed line. When Removed SP is high, the team is losing work they already invested effort in. Compare with Added SP: if both are high, the team is swapping active work mid-sprint, which is the most disruptive pattern.

### Net Scope Change

Net scope change is Added SP minus Removed SP, both measured after the planning window. Zero means execution scope was stable (or swaps balanced out). Positive means the team absorbed more disruption than it shed. Negative means more started work was pulled than unplanned work was added. Because both inputs are cycle-filtered, this number reflects real execution-level scope movement — planning adjustments are invisible here.

### Disruption Rate

Disruption rate normalizes post-planning Added SP against Active SP (the work the team actually engaged with during the sprint). A 15% rate means 15% of the team's active work was unplanned additions. The denominator is Active SP (not Total) because it answers "of the work we actually did, how much was unplanned?" — this maps directly to how disrupted the sprint felt. With cycle-filtered Added SP, this metric no longer inflates from planning-window reshuffling or unstarted tickets, giving you a number you can trust for trend analysis and retro discussions.

---

## Classification Breakdown

### Unplanned Bug

Bug-type tickets added after the planning window that entered the work cycle. These represent production issues, customer-reported defects, or QA findings that forced the team to drop planned work. Unlike the previous model which included planning-window bugs, only bugs that appeared during execution and were actually worked on count here. A high count points to upstream quality problems. Track alongside bug time-in-progress to understand the capacity impact.

### Priority Escalation

Tickets that existed in the backlog before the sprint started but were pulled into the sprint after the planning window, and then entered the work cycle. This is known work that wasn't planned for this sprint — someone decided it couldn't wait. Frequent escalation suggests prioritization instability or stakeholders overriding sprint commitments. The work itself isn't new (unlike Scope Injection), which means better backlog grooming could have surfaced it during planning.

### Scope Injection

The catch-all for post-planning additions that entered the cycle and aren't bugs or pre-existing tickets. These are new feature requests, newly discovered requirements, or stakeholder asks that appeared after planning ended and consumed team capacity. This is the purest measure of scope instability — work that didn't exist before the sprint and wasn't in the backlog. Address it through clearer sprint boundaries and stakeholder alignment on when work can enter a sprint.

---

## Planning Window

The planning window (configurable as "Planning Window Days" in Settings) defines how many days after sprint start are reserved for planning adjustments. During this window, tickets being added, removed, or reshuffled is expected — it's the team finalizing their commitment. After the window closes, additions and removals that enter the work cycle are treated as disruption and appear in Added SP, Removed SP, and the classification breakdown. The Committed SP (Total) card snapshots sprint membership at the moment this window closes. Adjust the window length to match your team's actual planning cadence: 2 days for teams that plan on day one, 3+ days for teams with extended planning rituals, 0 for teams that start executing immediately.
