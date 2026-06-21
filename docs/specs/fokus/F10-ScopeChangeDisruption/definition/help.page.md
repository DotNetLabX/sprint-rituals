# Scope Change & Disruption — Guide Page Help

Sibling of `docs/features/ScopeChangeDisruption/spec.md`. Each section provides the Long variant (guide page paragraph explaining interpretation and recommended actions).

---

## Summary Metrics (Multi-Sprint)

### Average Disruption Rate

Disruption rate measures how much new work gets injected into sprints after they start. It's calculated per sprint as added story points divided by active committed story points, then averaged across the selected range. A consistently high disruption rate (above 20-30%) signals that sprint planning isn't capturing the real workload — either the backlog is poorly groomed, stakeholders are bypassing the process, or urgent issues are frequent. Use the trend chart to distinguish chronic disruption from occasional spikes. Pair with the classification breakdown to understand what kind of work is being injected.

### Average Net Scope Change

Net scope change captures whether sprints are growing or shrinking after they start. The formula is added story points minus removed story points, averaged across sprints. A consistently positive value means the team absorbs more work than it sheds — sprints are growing mid-flight. A negative value means more work is pulled out than added. Near zero is ideal: it means scope stays close to what was planned, or additions and removals balance out. Note that zero can also mean swaps — one item added for every item removed — which the classification breakdown helps identify.

### Total Bugs Added

This counts every bug-type ticket that was added after sprint start across the selected range, regardless of whether the bug has story points. Bugs are tracked by count rather than story points because many bugs go unestimated — a purely SP-based metric would undercount their impact. A high total suggests reactive quality problems: the team is spending sprint capacity fixing issues discovered after planning. Compare this with the bug ratio on the Developers page to see how much capacity bugs are consuming overall.

---

## Scope Change Bar Chart

The stacked or grouped bar chart visualizes the composition of each sprint's scope. Committed SP is the baseline — what the team planned. Added SP shows what was injected mid-sprint. Removed SP shows what was pulled out. Completed SP shows what actually got done. Healthy sprints have small added and removed segments relative to committed. When the added segment consistently rivals or exceeds committed, the team is running two sprints in one — the planned sprint and the reactive sprint. Click any sprint bar to drill into its single-sprint detail view.

---

## Disruption Rate Trend

This trend line plots the disruption rate for each sprint in the selected range. A flat or declining line means the team is keeping mid-sprint additions under control. A rising line over 3+ sprints suggests a systemic problem — stakeholders may be routinely adding work after sprint start, or the team's definition of "committed" is too loose. Compare this trend with the carry-over rate trend (if visible) to see the full picture: high disruption often leads to high carry-over, but carry-over without disruption points to estimation or capacity problems rather than scope instability.

---

## Classification Breakdown

### Planning Overflow

Planning overflow captures tickets that were added to the sprint within the first two days after it started. These typically represent work that should have been included during planning but was missed — a grooming gap rather than a genuine disruption. A consistently large planning overflow category suggests sprint planning sessions need more time, better backlog preparation, or wider participation. Unlike other categories, planning overflow doesn't indicate external interference — it's an internal planning quality signal.

### Unplanned Bug

Unplanned bugs are tickets with a "Bug" issue type that were added to the sprint after the two-day planning grace period. These represent genuine disruption — production issues, customer-reported defects, or QA findings that force the team to drop planned work. A high count of unplanned bugs points to upstream quality problems: insufficient testing, fragile code areas, or inadequate monitoring. Track this alongside the bug time-in-progress metric in the single-sprint view to understand how much capacity these bugs actually consumed.

### Scope Injection

Scope injection is the catch-all for mid-sprint additions that aren't bugs and aren't pre-existing work. These are typically new feature requests, newly discovered requirements, or stakeholder asks that appear after sprint planning. This is the purest measure of scope instability — work that literally didn't exist before being added to the sprint. A high scope injection rate suggests weak backlog discipline or stakeholders who bypass the planning process. Address it through clearer sprint boundaries and stakeholder communication about when work can enter a sprint.

### Priority Escalation

Priority escalation captures tickets that already existed in the backlog (created before the sprint started) but were added to the sprint after the planning grace period. Unlike scope injection, this work was known — it just wasn't planned for this sprint. Someone decided it couldn't wait. Frequent priority escalation suggests the team's prioritization process isn't stable, or that external stakeholders are overriding sprint commitments. It can also indicate that the backlog contains urgent items that aren't being surfaced during planning.

---

## Single-Sprint Metric Cards

### Committed SP (Active / Total)

Committed SP shows how much work the team planned for the sprint. Two values are displayed: "Active" excludes tickets whose final status matches an excluded-from-scope status configured in Settings (e.g., "To Do" or "Blocked" tickets that were technically committed but never worked on). "Total" includes everything. Active committed SP is the denominator for disruption rate and completion rate — it represents the work the team actually engaged with. A large gap between active and total suggests the team is committing to tickets it never starts.

### Added SP

Added SP measures the volume of new work injected mid-sprint. Only tickets that stayed in the sprint (weren't subsequently removed) and aren't excluded from scope metrics are counted. The delta compared to the prior sprint shows whether mid-sprint additions are growing or shrinking. An increasing trend means the team is absorbing more unplanned work each sprint — sustainable only if the team has built buffer capacity into their planning.

### Removed SP

Removed SP captures work that was taken out of the sprint, whether it was originally committed or added mid-sprint. Removal isn't inherently good or bad — removing work to make room for higher-priority items is healthy sprint management. But frequent removals of committed work suggest the team's initial commitments aren't reliable. The delta shows whether removal volume is changing. Compare removed SP with added SP: if both are high, the team is actively swapping scope rather than executing a stable plan.

### Net Scope Change

Net scope change distills the sprint's scope stability into a single number. Zero is ideal — the sprint ended with the same amount of work it started with. Positive values mean more was added than removed, indicating scope growth. Negative values mean the sprint shed more work than it gained. The delta compares to the prior sprint's net scope change. Persistent positive values across sprints suggest a cultural pattern of over-adding — the team needs stronger sprint boundaries.

### Disruption Rate

Disruption rate normalizes the amount of mid-sprint additions against the sprint's committed scope. A 10% rate means the team added work equal to 10% of their original commitment. This is more meaningful than raw added SP because it accounts for sprint size — adding 5 SP to a 20 SP sprint (25%) is more disruptive than adding 5 SP to a 100 SP sprint (5%). The delta uses polarity-aware coloring: green when the rate drops (less disruption), red when it rises. When active committed SP is zero, the rate displays as 0%.

### Bug Count

Bug count tracks disruption that story-point metrics can miss. Many teams don't estimate bugs, so a sprint could absorb significant bug work without it appearing in the added SP metric. This count captures every bug added after sprint start. The delta compares to the prior sprint — a rising count over time suggests growing quality issues in the product or insufficient upstream testing. Cross-reference with the bug time-in-progress section below to understand the capacity impact of each bug.

---

## Scope Burnup Chart

The burnup chart tracks two lines across the sprint's days. The "total scope" line starts at the active committed SP and steps up when work is added or down when work is removed, on the exact day it happened. The "completed SP" line climbs as tickets reach done statuses. In a well-managed sprint, the scope line stays flat while the completed line rises to meet it. Scope line jumps mid-sprint are visible disruption events. The chart background is shaded into two phases: days 1-2 (planning phase) and day 3 onward (execution phase), giving visual context for when changes occurred. Scope changes during the planning phase are expected; changes during execution are the real disruption signal.

---

## Event Table

The event table is the audit trail for all scope changes. Each row shows the sprint day number and calendar date, the Jira ticket key and summary, story points (if estimated), issue type, whether the ticket was added or removed, and which disruption classification category it falls into. Items whose final status matches an excluded-from-scope status are still shown (for completeness) but are visually dimmed and tagged as excluded — they don't contribute to metric calculations. Use this table to reconstruct what happened during a sprint: who asked for what, when it arrived, and how it was classified.

---

## Bug Time-in-Progress

For each bug added mid-sprint, this section shows how much time it spent in active work statuses (the statuses between the workflow start and end boundaries configured in your app settings). Time is measured in fractional calendar days. A bug that spent 0.5 days in progress had minimal impact; one that consumed 3+ days is a significant capacity drain. This section is hidden when no bugs were added mid-sprint. Sum the days across all bugs to estimate total bug-driven capacity loss for the sprint. Compare this with your team's total available capacity to understand the real cost of reactive bug work.

---

## Sprint Selector

The sprint selector controls which view you see. Selecting a range (Last 3, Last 5, or All) shows the multi-sprint trend view with summary metrics, bar charts, and aggregated breakdowns. Selecting a single sprint shows the detail view with metric cards, burnup chart, event table, and bug time-in-progress. "Last 5" is the default. Use multi-sprint view for spotting patterns and trend analysis. Use single-sprint view for retrospective deep-dives into specific sprints. The URL updates when you change selection, so you can bookmark or share a specific view.

---

## Sub-Team Filter

When a sub-team is selected, every metric on the Sprints page recalculates using only tickets assigned to developers in that sub-team. Committed SP, added SP, removed SP, disruption rate, classification breakdown, event table, and bug metrics all scope to that sub-team. This lets you compare how different sub-teams experience scope disruption — one sub-team may absorb most of the unplanned bugs while another gets hit with scope injection. Select "All" to return to the full team view.

---

## Excluded-from-Scope Statuses

This setting lets you remove tickets from scope metrics based on their final status at sprint end. For example, if a ticket was committed but still in "To Do" when the sprint closed, it arguably wasn't real scope — the team never started it. Adding "To Do" to the exclusion list removes such tickets from committed SP, disruption rate denominators, and completion rate calculations. The excluded tickets still appear in the event table (visually marked) for audit purposes. This setting is system-wide — it affects all analytics features, not just scope change. Configure it in the Settings page. Status matching is case-insensitive.
