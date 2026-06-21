# Sprint Summary Card — Guide Page Help

Sibling of `docs/features/SprintSummaryCard/spec.md`. Each section provides the Long variant (guide page paragraph explaining interpretation and recommended actions).

---

## Health Score

The health score distills three sprint metrics into a single number from 0 to 100. Each sub-metric (completion rate, disruption rate, carry-over rate) is converted to a 0-100 sub-score using the threshold bands you configure in Settings, then combined using configurable weights (default: completion 40%, disruption 30%, carry-over 30%). The composite score determines the badge color: green at 75 or above, amber from 40 to 74, red below 40. Use this as a quick pulse check — a green score means the sprint went well across all dimensions, while amber or red tells you to dig into the individual metrics below.

---

## Health Score — Per-Metric Sub-Scores

Each sub-metric is scored independently on a 0-100 scale. For completion rate (higher is better): at or above the green threshold scores 100, between amber and green interpolates from 50 to 99, below amber interpolates from 0 to 49. For disruption and carry-over rates (lower is better): at or below the green threshold scores 100, between green and amber interpolates from 99 to 50, above amber interpolates from 49 to 0. Each sub-score has its own RAG color based on per-metric thresholds in Settings — independent of the composite badge color. If you see a green composite but one sub-score is red, the other two are compensating. Address red sub-scores even when the overall health looks fine.

---

## SP Completed / Committed

Committed SP is the sum of story points on tickets present in the sprint at or before the sprint start date — your sprint commitment. Completed SP is the subset that reached a done status by sprint end. This metric shows whether the team delivered what it planned. A consistent gap between committed and completed suggests overcommitment or estimation problems. Tickets without story point estimates are excluded from both numbers. The delta compares to the prior sprint; the sparkline shows the last 4 sprints. Delta color is neutral (no green/red) because more committed SP is neither inherently good nor bad.

---

## Completion %

Calculated as completed SP divided by committed SP, multiplied by 100. This measures how well the team delivered against its sprint commitment — the denominator is what was planned at sprint start, not total work assigned. When committed SP is zero, completion percentage is zero rather than undefined. A healthy team typically sustains 80% or above. Consistent drops below 70% warrant discussion about estimation accuracy, sprint planning practices, or external blockers. The delta arrow is green when completion rises and red when it drops.

---

## Disruption Rate

Disruption rate is the sum of story points on tickets added after sprint start (and not subsequently removed) divided by committed SP, multiplied by 100. It measures how much unplanned work entered the sprint. Some disruption is normal — urgent bugs, priority shifts — but a consistently high rate means the team can't protect its sprint commitment. Compare with the mid-sprint disruption flag below for details on what was added. The delta arrow is green when disruption drops and red when it rises.

---

## Carry-Over Rate

Carry-over rate is the sum of story points on tickets that didn't reach done status, divided by total scope (committed SP + added SP), multiplied by 100. The denominator includes mid-sprint additions because the team was responsible for that work too. A carry-over rate under 15% is healthy. Rates consistently above 30% suggest overcommitment, blockers, or work items that are too large to finish in a sprint. The delta arrow is green when carry-over drops and red when it rises.

---

## Delta Indicator

Each metric card shows a delta comparing the current sprint's value to the prior closed sprint (the one with the next-earlier start date). The delta appears as an arrow (up or down) with a signed number. Color follows the metric's polarity: for completion, higher is green; for disruption and carry-over, lower is green; for SP completed/committed, the delta is neutral (no color). When no prior sprint exists, the delta is absent entirely — it is not shown as zero.

---

## Sparkline

The sparkline shows a mini chart of the metric's value across the 4 most recent closed sprints ending at (and including) the selected sprint. When you select an older sprint, the window shifts backward — it always ends at the sprint you're viewing. If fewer than 4 closed sprints exist, the sparkline shows whatever is available. Use sparklines to spot trends: a steadily rising disruption rate over 4 sprints is a stronger signal than one bad sprint.

---

## Top Epics

Shows the epics that received the most work during the selected sprint, ranked by story points completed. Each entry displays the epic name, SP completed this sprint, and overall progress across all sprints (done SP out of total SP with a completion percentage). This helps you understand where sprint effort was directed at the initiative level. The section is hidden when no epics had any story points completed in the sprint. Overall progress spans all sprints — not just the selected one — so you can see how much of each epic remains.

---

## Developer Leaderboard

Lists every active developer sorted by story points completed in descending order. Each entry shows the developer's avatar, display name, sub-team, and SP completed. Developers who completed zero story points still appear at the bottom of the list — they are never hidden. Inactive developers are excluded entirely. The leaderboard is designed for visibility, not competition. Use it to identify who might need support (consistently low output could signal blockers or overallocation to non-sprint work) and to recognize strong contributors.

---

## Zombie Tickets Flag

A ticket earns the zombie label when it appears in sprint memberships for 3 or more distinct sprints — they don't have to be consecutive. For each zombie, the flag shows the ticket key, summary, and the number of sprints it has lived through. Zombies are the strongest signal of stuck work: the ticket might be blocked by an external dependency, too large to finish in a sprint, poorly defined, or simply deprioritized every sprint without being removed. Each zombie should be discussed in a retrospective — decide whether to break it down, escalate the blocker, or remove it from the sprint entirely. Tickets without story points still count as zombies since this flag uses membership count, not SP.

---

## Mid-Sprint Disruption Flag

This flag fires when tickets were added to the sprint more than 2 days after the sprint start date and not subsequently removed. It reports the total story points and ticket count of those late additions. The 2-day grace period treats Day 1-2 additions as planning overflow rather than disruption — teams often finalize their sprint backlog in the first day or two. If this flag fires frequently with high SP values, the team's sprint boundary is not being respected. Investigate who is injecting work and whether it can wait for the next sprint.

---

## Zero-SP Developers Flag

Lists the display names of active developers who were assigned tickets in the sprint but completed zero story points. This flag only fires for developers who had work — developers not assigned to the sprint at all are not flagged (though they still appear in the leaderboard at zero). Common causes include being pulled onto non-sprint work, encountering blockers, or being assigned tickets that were subsequently removed. This flag is a conversation starter, not an indictment — check whether the developer was supporting the team in ways that don't show up in SP metrics.

---

## No Flags Message

When no zombie tickets, mid-sprint disruptions, or zero-SP developers are detected, the flags section displays a positive "No flags this sprint" message instead of being hidden. This is intentional — it confirms that the system checked for these conditions and found nothing, rather than leaving you wondering whether the section failed to load.

---

## Sprint Selector

The sprint selector dropdown lists all closed sprints ordered by start date, most recent first. Changing the selection updates everything on the page: health score, metric cards (including deltas and sparklines), top epics, leaderboard, and flags. The URL also updates to reflect the selected sprint, so you can bookmark or share a link to a specific sprint's summary. On Dashboard, the selector shows individual sprints only — aggregate options like "Last 3" or "Last 5" are not available here but are offered on other analytics pages.

---

## Sub-Team Filter

When a sub-team is selected, every metric on the page recalculates using only that sub-team's developers and their tickets. Metric cards show filtered values, the health score recomputes from those filtered metrics, the leaderboard shows only developers in that sub-team, top epics reflect only SP completed by those developers, and flags scope to that sub-team's developers and tickets. Select "All" to remove the filter and return to the full team view. This is useful for large teams with distinct sub-teams — you can run a focused retrospective for each sub-team using the same dashboard.

---

## Empty State

When no closed sprints exist in the system, the dashboard shows an empty state instead of blank cards. This typically means you haven't synced any completed sprints from Jira yet. Navigate to the Sprints page, sync a sprint that has been completed in Jira, and return to the Dashboard to see your first summary. The empty state is defined by the App Shell feature and is consistent across all analytics pages.
