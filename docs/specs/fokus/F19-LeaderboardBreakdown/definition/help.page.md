# Leaderboard Breakdown — Guide Page Help

Sibling of `docs/features/LeaderboardBreakdown/spec.md`. Each section provides the Long variant (guide page paragraph explaining interpretation and recommended actions).

---

## Features / Bugs Toggle

The toggle switches the Dashboard leaderboard between two views of the same sprint. Features mode (the default) shows only story points and tickets completed on non-bug work — Stories, Tasks, and other planned delivery. Bugs mode shows only story points and tickets completed on bug fixes. This split exists because combining both into a single number hides whether a developer's output is new value or rework. A developer ranking high in Features mode is delivering planned work. A developer ranking high in Bugs mode is spending capacity on fixes — which usually means rework on previously implemented features. Use this to spot developers who are stuck in a bug-fixing cycle and may need support breaking out of it.

---

## Dashboard Leaderboard — Features Mode

Shows all active developers sorted by feature story points completed in descending order. Each entry displays the developer's avatar, name, sub-team, feature SP, and feature ticket count. Developers with zero feature SP still appear at the bottom — they are never hidden. This is your primary view of who is delivering planned work. A developer consistently at the bottom in features mode isn't necessarily underperforming — check bugs mode to see if they're absorbing rework, and check the Throughput tab for capacity context.

---

## Dashboard Leaderboard — Bugs Mode

Shows all active developers sorted by bug fix story points completed in descending order. The developer at the top has spent the most capacity on bug fixes. Bug work typically represents rework — fixing defects in previously implemented features — and developers usually fix their own bugs. A developer consistently high on this list may be struggling with quality in their feature implementations, or they may be absorbing bugs from the broader team. Cross-reference with the Bug Ratio tab on the Developers page for the multi-sprint trend. A single high-bug sprint can be noise; a pattern across sprints is a signal.

---

## Leaderboard SP Column

The story points completed for the selected work type (features or bugs) in the current sprint. Tickets without story point estimates are excluded from this number — they don't count as zero, they're simply absent. If you see a developer with high ticket count but low SP, they may be closing many small or unestimated items. The configured default SP per bug applies to unestimated bug tickets, giving them a fallback SP value.

---

## Leaderboard Tickets Column

The number of done tickets for the selected work type (features or bugs) in the current sprint. Unlike the SP column, this includes tickets with no story point estimate — a completed ticket counts regardless of whether it was estimated. This means the ticket count may represent a broader set of work than the SP column. Use both columns together: SP tells you the estimated effort delivered, ticket count tells you the volume of items completed.

---

## Leaderboard Tab (Developers Page)

The Leaderboard tab on the Developers page provides a richer view than the Dashboard widget. It shows a stacked horizontal bar chart with feature SP (blue) and bug SP (red) per developer, plus a detailed table with both SP and ticket breakdowns. Unlike the Dashboard (which shows a single sprint), this tab supports multi-sprint ranges via the sprint selector — defaulting to the last 5 closed sprints. Use this view in retrospectives to discuss work composition patterns: who is spending their capacity on features vs bugs, and whether that distribution matches the team's goals.

---

## Stacked Bar Chart

Each bar represents one developer, with blue for feature SP and red for bug SP. Bars are sorted by total SP descending — the developer who completed the most total work is at the top. The relative proportions within each bar tell the story: a bar that's mostly blue means the developer focused on planned work; a bar that's mostly red means they spent their sprint on bug fixes. In multi-sprint mode, the bars aggregate across all selected sprints. Compare bar lengths to spot capacity imbalances, and compare color proportions to spot quality patterns.

---

## Leaderboard Table — Feature SP

The total story points completed on non-bug tickets (Stories, Tasks, Sub-tasks, and any other non-Bug issue type) across the selected sprint range. In multi-sprint mode, this is a straight sum across all selected sprints. Tickets without story points are excluded from this number. Higher feature SP generally indicates a developer focused on delivering planned roadmap work. If a developer's feature SP is consistently low relative to their peers, check whether they're being pulled into bug fixing, support work, or non-sprint activities.

---

## Leaderboard Table — Bug SP

The total story points completed on bug tickets across the selected sprint range. In multi-sprint mode, this is a straight sum across all selected sprints. Unestimated bugs use the configured default SP per bug as a fallback. Higher bug SP means more capacity spent on rework. Since developers typically fix their own bugs, a high bug SP is both a quality signal and a capacity drain signal. Compare this with the developer's feature SP to understand their work composition. A developer with 40 bug SP and 10 feature SP has a very different sprint than one with 10 bug SP and 40 feature SP, even if their totals are similar.

---

## Leaderboard Table — Total SP

The sum of feature SP and bug SP. This is the same number that the original undifferentiated leaderboard showed — provided here for reference and sorting. On its own, total SP tells you volume but not composition. Two developers with 50 total SP may have completely different work profiles. Always read total SP alongside the feature/bug split for a complete picture.

---

## Leaderboard Table — Feature Tickets

The number of completed non-bug tickets across the selected sprint range. This count includes tickets with no story point estimate — a completed ticket counts regardless of whether it was estimated. This makes the ticket count consistent with the Throughput and Bug Ratio tabs. Compare feature tickets with feature SP to understand estimation density: many tickets with low SP suggests small, granular work items; few tickets with high SP suggests large, chunky work.

---

## Leaderboard Table — Bug Tickets

The number of completed bug tickets across the selected sprint range. This count includes unestimated bugs. Compare bug ticket count with bug SP: a high bug count with low bug SP means many small fixes; a low bug count with high bug SP means fewer but expensive bugs. Both patterns warrant different conversations — many small bugs might indicate sloppy implementation, while a few expensive bugs might indicate missed requirements or architectural gaps.

---

## Leaderboard Table — Total Tickets

The combined count of all completed tickets (both features and bugs) across the selected sprint range. Like the individual ticket counts, this includes tickets with no story point estimate. This number matches what you would see if you counted all done tickets for a developer in the Throughput tab — it provides a consistency anchor across the Developers page tabs.

---

## Leaderboard Delta Indicators

In single-sprint view on the Developers page, each metric shows a delta comparing to the prior closed sprint. Feature SP uses positive polarity — an increase is green (more feature work is good). Bug SP uses negative polarity — a decrease is green (less rework is good). Total SP uses neutral polarity — no color, because more total SP is neither inherently good nor bad without knowing the composition. Feature and bug ticket counts follow the same polarity as their SP counterparts. In multi-sprint mode, deltas are not shown — the chart provides trend context instead.
