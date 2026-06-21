# Developer Throughput — Guide Page Content

Sibling of `docs/features/DeveloperThroughput/spec.md`. Each section contains the **Long** variant (guide page paragraph explaining interpretation and recommended actions).

---

## SP Assigned

SP Assigned counts the story points on every ticket where this developer is the assignee and the ticket was not removed from the sprint. Removed tickets are excluded entirely — they don't count as zero, they simply aren't part of the developer's workload for that sprint. Tickets without story point estimates are also excluded from this number (they appear in ticket counts instead). Use SP Assigned to understand how much work was on a developer's plate, not how much they delivered.

---

## SP Completed

SP Completed counts the story points on tickets assigned to this developer whose final status matches any value in your configured done statuses list (default: "Done," "Closed"). This is the core delivery metric — it tells you how much work a developer actually finished. Compare it to SP Assigned to get the completion rate, and track it across sprints to spot capacity trends. Tickets without story point estimates are not included.

---

## Completion %

Completion percentage is calculated as SP Completed divided by SP Assigned, multiplied by 100. When a developer has zero SP assigned, the completion rate is 0% rather than undefined. This metric normalizes delivery across developers with different workloads — a developer who completed 8 of 10 SP (80%) may be performing better than one who completed 15 of 25 SP (60%), even though the raw SP completed is lower. Watch for consistently low completion rates, which may indicate overcommitment, blockers, or estimation problems.

---

## Tickets Done

Tickets Done is a simple count of completed tickets, regardless of whether they have story point estimates. This complements SP-based metrics by capturing unestimated work that SP metrics miss entirely. A developer with low SP Completed but high Tickets Done may be handling many small or unestimated items. Compare this with SP Completed to understand whether a developer's sprint was dominated by a few large items or many small ones.

---

## Tickets Carried Over

Tickets Carried Over counts every ticket assigned to this developer that was neither completed nor removed from the sprint. These are the items that will roll into the next sprint (or back to the backlog). A high carry-over count relative to tickets done suggests the developer was overloaded or blocked. Unlike the Carry-Over Tracker feature (which looks at team-level carry-over patterns), this metric is scoped to the individual developer.

---

## Delta Indicators

When viewing a single sprint, each metric shows a delta comparing the current value to the prior closed sprint. Deltas are polarity-aware: for SP Completed, Completion %, and Tickets Done, an increase is green (improvement) and a decrease is red (regression). For Tickets Carried Over, the polarity is reversed — a decrease is green (fewer items left unfinished) and an increase is red. SP Assigned uses neutral coloring (gray) because more assigned work is neither inherently good nor bad. Deltas are not shown in multi-sprint view — the trend chart serves that purpose instead.

---

## Rolling Average SP Completed

The rolling average smooths out sprint-to-sprint noise to reveal a developer's true delivery trend. It uses a fixed 3-sprint window, looking backward from each sprint and collecting the 3 most recent sprints where the developer had greater than 0% capacity. Sprints where the developer was fully absent (0% capacity) are skipped — they don't count as data points and don't drag the average down. If fewer than 3 qualifying sprints exist for a developer, the rolling average is not shown (null). This prevents misleading averages based on insufficient data. Use the rolling average on the trend chart to compare developers' sustained delivery rates rather than reacting to single-sprint fluctuations.

---

## Developer Sprint Capacity

Capacity represents how available a developer was during a sprint. Set it to 50% for someone on vacation for half the sprint, or 0% for someone fully absent. Capacity is managed on the Developers page — select a sprint and adjust each developer's capacity percentage. A developer with 0% capacity still appears in the throughput table with their actual metrics (they may have completed work before leaving), but that sprint is excluded from their rolling average calculation. This prevents absences from distorting the smoothed trend line. Capacity defaults to 100% when not explicitly set.

---

## Throughput Trend Chart

The trend chart appears when viewing multiple sprints (Last 3, Last 5, or All). Each developer is a separate line, plotting their rolling average SP completed across the selected sprints. The rolling average smooths individual sprint variation so you can see sustained trends — is a developer's delivery capacity growing, stable, or declining? Hovering a data point shows the exact rolling average value for that developer and sprint. Use this chart to identify developers who may need support (declining trend), recognize consistent performers, and set realistic sprint commitments based on demonstrated capacity rather than optimistic estimates.

---

## Sprint Selector

The sprint selector controls which sprints are included in the throughput data. Selecting a single sprint shows detailed per-developer metrics with delta indicators comparing to the prior sprint. Selecting a range (Last 3, Last 5, or All) shows averaged values in the table and enables the trend chart below it. The page loads with the most recent closed sprint pre-selected. Use single-sprint view for retrospective deep-dives into a specific sprint, and multi-sprint view for spotting delivery patterns over time.

---

## Sub-Team Filter

When a sub-team is selected, every metric on the throughput view recalculates using only that sub-team's developers. The table shows only developers in the selected sub-team, the trend chart plots only their lines, and all values reflect their work. The filter applies to developers, not tickets — a developer's full workload is shown regardless of which team the tickets belong to. Select "All" to remove the filter and see the entire team.

---

## Assignee Attribution

Developer Throughput attributes each ticket to the person who holds it when the sprint is synced. If a ticket is reassigned mid-sprint, the new assignee gets full credit — the original assignee gets none. This is a deliberate simplification: tracking reassignment history would require change-log analysis that adds complexity without proportional value in v1. Be aware of this when interpreting metrics for sprints with frequent reassignments. If a developer's numbers seem off, check whether tickets were reassigned close to sprint end.

---

## Zero-Ticket Developers

If a developer has no tickets assigned in a sprint, they still appear in the table with all metrics at zero. This is intentional — hiding developers with no work would mask capacity problems. A developer showing zeros might indicate they were pulled to another team, are blocked on non-Jira work, or were accidentally left out of sprint planning. Their visibility ensures the Scrum Master can investigate rather than unknowingly overlook an underutilized team member.

---

## Inactive Developers

Inactive developers do not appear in the throughput table, trend chart, or any calculations. This prevents people who have left the team or moved to a different role from cluttering the view. Developer active status is managed on the Developers page. If someone is temporarily unavailable but still on the team, use sprint capacity (set to 0%) rather than marking them inactive — inactive is for permanent removal from the throughput view.
