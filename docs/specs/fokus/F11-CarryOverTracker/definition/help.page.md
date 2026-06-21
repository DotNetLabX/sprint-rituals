# Carry-Over Tracker — Guide Page Help Content

Guide page paragraphs explaining interpretation and recommended actions for the carry-over sections on the Sprints page.

---

## Carry-Over Rate

Carry-over rate measures how much work rolls from sprint to sprint. The formula is: carry-over story points divided by total scope (committed + added story points). A rising trend suggests chronic overcommitment, blockers, or work that's too large to finish in a sprint. Healthy teams typically stay under 15%. Occasional spikes are normal — a consistent rate above 30% warrants a retrospective discussion about sprint planning, task breakdown, or dependency management.

## Carry-Over SP

The raw volume of unfinished work, measured in story points. Unlike carry-over rate (which is relative to sprint scope), this absolute number shows whether the amount of unfinished work is growing. Compare this alongside the carry-over rate — a stable rate with growing SP means your sprints are getting bigger but the same proportion rolls over. Tickets without story point estimates are not included in this number.

## Carry-Over Ticket Count

The count of all tickets that didn't reach a "done" status when the sprint closed, regardless of whether they have story points. This catches unfinished work that SP-based metrics miss — particularly unestimated bugs and tasks. A high ticket count with low carry-over SP suggests many small items are slipping through.

## Carry-Over Rate Trend

The trend line shows your carry-over rate for each sprint in the selected range. Look for patterns: is carry-over consistently high (systemic issue), spiking occasionally (incident-driven), or trending upward (degrading delivery capability)? Compare this with the disruption rate trend in the scope change section above — high disruption often drives high carry-over, but carry-over without disruption points to estimation or capacity problems.

## Carry-Over by Workflow Stage

This chart breaks down carry-over story points by workflow stage (the phases detected by workflow auto-detection). If most carry-over is stuck in "Testing," your QA capacity may be the bottleneck. If it's in "In Progress," developers may be overloaded or blocked. In multi-sprint mode, the stacked bars show how this distribution changes over time — a growing segment in one stage is a persistent bottleneck worth addressing.

## Status Distribution (Donut Chart)

The donut chart shows what percentage of carry-over tickets are in each workflow stage for the selected sprint. It answers "where is work getting stuck?" at a glance. The largest segment is your primary bottleneck. Statuses that don't map to any configured workflow stage appear under "Other" — if this segment is large, consider updating your workflow stage configuration in Settings.

## Issue Type Breakdown

Shows how many carry-over tickets belong to each issue type (using the exact types from Jira — Story, Bug, Task, Improvement, or any custom types your team uses). This reveals whether certain work types carry over disproportionately. If bugs consistently carry over more than stories, your team may be underestimating bug complexity or deprioritizing them during the sprint.

## Carry-Over Destination

This section tracks the outcome of tickets that carried over from the previous sprint into the current one. Four outcomes are possible: "Completed" means the ticket was finished this sprint. "Carried again" means it's still not done — it will roll over yet again. "Removed" means it was pulled out of the sprint mid-way. "Dropped" means it wasn't even included in this sprint (deprioritized or moved elsewhere). A healthy pattern shows most prior carry-over completing. If "carried again" dominates, the same work keeps rolling without progress — investigate why.

## Zombie Tickets

Zombie tickets are work items that persist across multiple sprints without reaching "done." A ticket earns the zombie label when it appears in 3 or more sprints. These are the strongest signal of systemic problems: the ticket might be blocked by an external dependency, too large to complete in a sprint, poorly defined, or simply deprioritized every sprint without being removed from the backlog. Each zombie should be discussed in a retrospective — decide whether to break it down, escalate the blocker, or remove it from the sprint entirely.

## Zombie Trajectory

The trajectory shows the final status of a zombie ticket in each sprint it appeared in, ordered chronologically. Look for stall patterns: "In Progress → In Progress → In Progress" means nobody is making progress. "In Progress → Testing → Testing → Testing" means the ticket progresses to testing but gets stuck there. "In Progress → Testing → In Progress → Testing" suggests the ticket is bouncing back from QA. Each pattern points to a different root cause — capacity constraints, quality issues, or unclear requirements.

## Average Carry-Over Rate (Multi-Sprint)

Averages the carry-over rate across all sprints in the selected range. Use this as a baseline — if your average is above 20%, carry-over is a structural issue, not a one-sprint anomaly. Compare with individual sprint values in the trend chart to distinguish chronic problems from isolated spikes.

## Total Zombie Tickets (Multi-Sprint)

The total number of unique zombie tickets across the selected sprints. A high count relative to your team size suggests systemic problems with work item sizing, dependency management, or sprint planning discipline. Zero zombies is the goal — it means no work lingers across sprint boundaries for extended periods.
