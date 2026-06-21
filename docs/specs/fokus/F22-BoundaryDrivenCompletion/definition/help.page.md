# Boundary-Driven Completion — Guide

## SP Completed

Shows the total feature story points your team completed this sprint. "Completed" means the ticket's final status reached the cycle time end stage (configured in Settings) or any stage after it in the workflow. For example, if your cycle time end stage is "Testing," tickets in Testing, Done, or Closed all count as completed. Change the end stage in Settings to shift what counts — set it to "Done" for end-to-end completion, or "Testing" to measure dev throughput.

## Completion %

The percentage of committed feature work that was completed. Calculated as feature SP completed divided by feature SP committed. Both the numerator and denominator exclude bugs — bug work has its own dedicated surfaces. The completion boundary follows the cycle time end stage setting: if it's set to "Testing," any committed feature ticket that reached Testing or beyond counts toward completion.

## Completed SP (Bar Chart)

In the multi-sprint Scope Change chart, this green bar shows the total completed story points for that sprint — including both features and bugs. This gives the full picture of how much work crossed the finish line. The completion definition follows the cycle time end stage: tickets whose final status is at or past that stage are counted. Compare this to the committed (gray) bar to see how close each sprint came to delivering its full scope.

## Completed SP (Burnup)

In the single-sprint burnup chart, the green line tracks cumulative feature-only completed story points day by day. Bug completions are shown separately in the red area. A ticket counts as completed on the day it first transitions to the cycle time end stage (or beyond) during the sprint. The gap between the orange scope line and the green completed line represents unfinished feature work.

## Carry-Over Rate

The percentage of sprint scope that was not completed — tickets that did not reach the cycle time end stage by sprint end. A high carry-over rate means work is rolling between sprints, which may indicate estimation issues, scope creep, or blocked tickets. Check the status distribution chart to see where carry-over tickets are stuck in the workflow. Adjusting the cycle time end stage in Settings changes what counts as completed, which inversely affects carry-over.

## Cycle Time End Stage

This setting controls what "completed" means across all analytics. It defines the workflow stage where work is considered done. Tickets whose final status is at this stage or any stage after it in the workflow are counted as completed. Set it to "Testing" to measure dev throughput (work handed off to QA counts as dev-complete). Set it to "Done" to measure end-to-end completion. This single setting affects the Dashboard, Sprints, Developers, Carry-Over, Bug Ratio, Epic Progress, and Cycle Time pages.
