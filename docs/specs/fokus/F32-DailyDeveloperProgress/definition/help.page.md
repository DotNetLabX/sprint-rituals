# Daily Developer Progress — Guide

## Daily Progress Tab
The Daily Progress tab shows how each developer is tracking through their sprint work day by day. Each developer with assigned tickets gets a card showing their burnup chart, completion stats, and any stalled tickets. Use this view during standups or mid-sprint check-ins to spot developers who may need support, scope adjustment, or blocker removal. This tab only shows data for the active sprint — historical sprint progress is available through the Throughput tab.

## Alert Banner
The alert banner appears at the top of the page and lists developers who are currently behind their expected pace. Each entry shows the developer's name, how many story points they are behind, and the equivalent number of days. A developer is flagged when their cumulative completed SP falls more than one full day behind the pace line. The banner is hidden during the first 2 days of the sprint (grace period) since it is normal for no tickets to be completed yet. If a sub-team filter is active, only developers in that sub-team are evaluated.

## Burnup Chart
Each developer card includes a mini burnup chart. The solid line shows cumulative story points completed over sprint days. The dashed diagonal line shows the expected pace — where the developer should be if they complete work at a steady rate adjusted for their capacity. The Y-axis is scaled to the developer's capacity-adjusted assigned SP, so the chart shows proportional progress. When the actual line falls significantly below the pace line, the developer may need attention. Click or hover on any day point to see which tickets were completed that day. Note: unlike the Throughput tab, this chart includes all ticket types (features, bugs, and tasks) to give a complete picture of daily work output.

## Expected Pace Line
The expected pace line is a straight diagonal from zero to the developer's capacity-adjusted assigned SP. It is calculated as: (assigned SP x capacity%) / sprint calendar days. A developer at 50% capacity with 10 SP assigned over a 10-day sprint has an expected pace of 0.5 SP per day. The line uses calendar days, so weekends appear as flat spots on the actual completion line — this is expected and not a cause for concern.

## Behind-Pace Indicator
A visual warning on a developer's card indicating they have fallen more than one full day behind their expected pace. This does not mean they are underperforming — common causes include blocked tickets, scope complexity, or tickets that are nearly done but haven't transitioned status yet. Use this as a prompt to check in during standup, not as a performance metric.

## Stall Badge
The stall badge shows the number of tickets assigned to this developer that are in progress but have had no status transition for 2 or more business days (Monday through Friday). Stalled tickets often indicate blockers, unclear requirements, or work that has been abandoned without updating the board. A developer can be on pace overall and still have a stalled ticket — the two signals are independent. Click the badge to see which specific tickets are stalled and how long they have been idle.

## SP Completed / Assigned
Shows the developer's total completed story points out of their total assigned story points in the active sprint. Unlike the Throughput tab, this includes all ticket types — features, bugs, and tasks — because daily progress should reflect all work being done, not just feature delivery.

## Completion Percent
The percentage of assigned story points that have been completed so far. At 100%, the developer has finished all their assigned work in the sprint. This number can exceed 100% if the developer completes carry-over tickets from prior sprints that were not in their initial assignment.

## Day Drill-Down
When you click or hover on a day point in the burnup chart, a tooltip shows all tickets completed on that day. Each ticket displays its Jira key, summary, story points, and issue type (Story, Bug, Task). Days with no completions explicitly show "No completions" so you can distinguish between zero completions and missing data.

## Stalled Ticket — Days Since Last Transition
For each stalled ticket, this shows the number of business days (Monday through Friday) since the ticket's most recent status change in Jira. A ticket that last changed status on Friday would show 2 days stalled on the following Tuesday. Weekends are not counted. Consider reaching out to the developer or checking the ticket for blockers if the stall count is high.

## Grace Period
During the first 2 calendar days of the sprint, the behind-pace alert banner is hidden and no warning indicators appear on developer cards. This prevents false alarms while the team is ramping up, attending sprint planning, or working on tickets that haven't reached completion yet. The burnup charts and completion stats are still visible during the grace period — only the alerting is suppressed.
