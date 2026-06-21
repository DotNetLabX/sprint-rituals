# Transition-Based Sprint Scope — Guide

## Committed SP (Active)

Shows the total feature story points your team actively started this sprint. A ticket counts as "started" when it first transitions to the cycle time start stage (configured in Settings) or any stage after it during the sprint's date range. If your start stage is "In Progress," only tickets that moved to In Progress (or beyond) during this sprint count. Tickets that stayed in To Do are not included — they were in the sprint but never engaged with. Carry-over tickets from previous sprints are also excluded because their start transition happened in an earlier sprint.

## Committed SP (Total)

The total feature scope this sprint engaged with, including tickets that were later removed. Total = Active + Removed. Use this to see the full picture of what the sprint touched, even if some items were pulled out mid-sprint.

## Added SP

Feature story points added to the sprint after it started that also entered the development pipeline. A ticket only counts as "added" if it was brought into the sprint mid-sprint AND transitioned to the cycle time start stage. Tickets added mid-sprint that never leave To Do are not counted as scope changes — they are unstarted backlog, not disruption. This gives a more accurate picture of actual mid-sprint disruption.

## Completed SP

Feature story points that first reached the cycle time end stage (or beyond) during this sprint. This includes carry-over work from previous sprints that finished here — if a ticket was started in Sprint 26 but reached Testing in Sprint 27, Sprint 27 gets the completion credit. Each ticket's completion belongs to exactly one sprint, so there is no double-counting across sprints.

## Carry-Over Rate

The percentage of active scope that was not completed within the sprint. Carry-over tickets are those that started this sprint (transitioned to the cycle time start stage) but did not reach the cycle time end stage by sprint end. A high carry-over rate means work is taking longer than a single sprint to complete. As carry-over decreases, the numbers naturally converge — most tickets start and finish in the same sprint, making the metrics straightforward.

## Disruption Rate

Added feature SP divided by committed feature SP. Only counts added tickets that actually entered the cycle (transitioned to the start stage). This filters out noise from tickets that were added to the sprint backlog but never started — true disruption is work that entered the pipeline mid-sprint and consumed team capacity.

## Bug SP (Bar Chart)

In the multi-sprint bar chart, bug story points completed per sprint are shown as separate bars alongside the feature bars. This keeps feature delivery metrics clean while still showing the bug workload. Compare the relative size of bug bars to feature bars to see how much capacity bugs are consuming each sprint.

## Scope Line (Burnup)

In the single-sprint burnup chart, the scope line tracks cumulative feature story points that have entered the development pipeline — tickets that transitioned to the cycle time start stage (or beyond) on each day. The line grows as the team picks up work throughout the sprint. The gap between this line and the completed line represents work in progress.

## Completed Line (Burnup)

In the single-sprint burnup chart, the completed line tracks cumulative feature story points that reached the cycle time end stage on each day. Watch for the completed line approaching the scope line — when they converge, the team has finished everything it started. A persistent gap at sprint end becomes the carry-over for the next sprint.
