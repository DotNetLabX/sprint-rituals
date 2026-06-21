# Epic Progress — Guide Page Help Content

Sibling of `docs/features/EpicProgress/spec.md`. Each section provides the Long variant (guide page paragraph explaining interpretation and recommended actions).

---

## Summary Cards

### Active Epics

Shows how many epics still have tickets that haven't reached a done status. An epic is considered active until every one of its tickets is complete. This count helps you gauge how much parallel epic work your team is juggling.

### Average Completion

The average completion percentage across all active epics, measured by story points. This gives a quick sense of how far along your epic portfolio is overall. A low average with many active epics may indicate the team is spread too thin across initiatives.

### Unlinked Work

Counts tickets that appeared in at least one sprint but have no epic assigned in Jira. A high unlinked count means sprint effort isn't tracked against any larger initiative — making it harder to measure progress toward goals. Consider linking these tickets to epics in Jira, then re-syncing.

---

## Epic Table

### Progress Bar

The filled segment shows actual story points completed versus the total estimated scope. When an epic has unestimated tickets, a lighter segment extends the bar to show imputed scope — an estimate based on the average story points of estimated tickets in the same epic. This gives a more realistic picture of true progress instead of ignoring unestimated work.

### SP Done / SP Total

Shows actual completed story points against the total scope. When unestimated tickets exist, the total includes an imputed estimate labeled separately (e.g., "+~12 SP est."). The imputed portion is calculated from the average story points of estimated tickets within the same epic. If no tickets in an epic have estimates, SP metrics are unavailable and only ticket counts are shown. Note: only remaining unestimated tickets receive imputed values — done tickets without estimates are not included in SP metrics. This means SP completion may appear lower than ticket completion when many remaining tickets lack estimates.

### Tickets Done / Total

A simple count of how many tickets have reached a done status versus the total number of tickets in the epic. Unlike SP completion, this treats every ticket equally regardless of size. Comparing ticket completion to SP completion reveals whether remaining work is mostly small tasks or a few large items.

### Velocity

The rolling average of story points completed per sprint, using only sprints where this epic had at least one story point completed. Sprints where the team worked on other epics are excluded so the number reflects actual delivery pace on this epic. When fewer than 3 data points exist, the velocity is computed from what's available but projections are marked as low confidence.

### Projected Remaining

Divides the remaining work (including imputed estimates for unestimated tickets) by the epic's velocity to project how many more sprints are needed. This is a rough estimate — it assumes future velocity matches recent velocity and that scope won't change. "Insufficient data" appears when no velocity data exists. "Low confidence" appears when fewer than 3 sprints of delivery data are available.

### Active Sprint Count

Counts the distinct sprints where at least one ticket from this epic appeared. This shows how long the epic has been in flight. A high sprint count with low completion may indicate the epic is being deprioritized or is too large.

---

## Expanded Ticket Table

### Ticket Row

Each row shows the ticket key, summary, issue type, story points, current status, and assignee. Tickets are grouped with remaining work first (so you can quickly see what's left) followed by completed tickets. Unestimated tickets show a dash instead of story points — no imputed value is displayed at the individual ticket level.

---

## Active / Completed Toggle

The Active view (default) shows epics that still have remaining work, sorted by completion percentage so the epics needing the most attention appear first. The Completed view shows epics where every ticket has reached a done status, sorted alphabetically. Summary cards update to reflect whichever view is selected.

---

## Sub-Team Filter

When a sub-team is selected, all epic metrics recalculate to show only that sub-team's contribution: ticket counts, story points, velocity, and projections reflect only tickets assigned to developers in the selected sub-team. Epics with no tickets assigned to the selected sub-team are hidden. This lets you see how much of an epic's progress comes from a specific sub-team, useful when multiple sub-teams contribute to the same epic.
