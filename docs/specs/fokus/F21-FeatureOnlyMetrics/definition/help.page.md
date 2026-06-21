# Feature-Only Delivery Metrics — Guide

## SP Completed Card

Shows the total feature story points your team completed this sprint. Bug story points are shown separately as a secondary annotation below the main number. This separation lets you answer "how many features did we deliver?" without bug work inflating the number. If stakeholders ask about velocity or delivery capacity, use this feature-only number. The bug annotation preserves awareness of total effort — use it to understand capacity allocation, not delivery output.

## Bug SP Annotation

The "(+X bug SP)" annotation below the SP Completed number shows how many story points of bug work were completed alongside features. A high bug SP relative to feature SP suggests the team spent significant capacity on reactive work. Compare this to the Bug Disruption Rate card to see how much of that bug work was mid-sprint additions versus committed bugs. If this number is consistently high, investigate root causes in the Bug Ratio tab on the Developers page.

## Completion %

Measures how well the team delivered against its feature commitment: feature SP completed divided by feature SP committed. Bugs are excluded from both numerator and denominator because they are typically unplanned — including them would distort the commitment-delivery signal. A sprint with 80% feature completion and heavy bug work tells a different story than 80% with no bugs. This metric feeds the health score's Completion sub-score, so the health score now reflects feature delivery health specifically.

## Burnup Chart — Scope SP Line

The orange scope line tracks cumulative feature scope across sprint days. It starts with committed feature SP on day 1 and steps up or down as feature tickets are added or removed. Bug tickets are excluded — their impact is shown separately by the red bug area. The gap between the scope line and the completed line represents unfinished feature work, giving you a clean read on feature delivery progress without bug churn distorting the picture.

## Burnup Chart — Completed SP Line

The green completed line tracks cumulative feature SP completed over the sprint. Each feature ticket that reaches a done status adds its SP to this line. Bug completions are not included — they reduce the red bug area instead. Watch for the green line converging toward the orange line: the closer they get, the more feature commitment was delivered.

## Burnup Chart — Bug SP Area

The red shaded area shows remaining (open) bug story points on each day of the sprint. It grows when new bugs enter the sprint and shrinks when bugs are completed or removed. This is the dedicated surface for tracking bug burden over time. If the area stays flat or grows despite completions, new bugs are outpacing resolutions — raise this in the retrospective.

## Developer Throughput — SP Assigned

Shows the feature story points assigned to each developer this sprint. Bug tickets are excluded because bug assignment is typically reactive and not part of sprint commitment. To see a developer's full workload including bugs, check the Leaderboard tab which shows both feature and bug SP with a toggle.

## Developer Throughput — SP Completed

Shows the feature story points completed by each developer. This measures feature delivery contribution. A developer with low feature SP but high bug work (visible on the Bug Ratio tab) may need workload rebalancing. Use this alongside the Leaderboard tab's breakdown to understand the full picture before drawing conclusions about individual productivity.

## Developer Throughput — Completion %

Feature SP completed as a percentage of feature SP assigned for each developer. This measures how well each person delivered against their feature assignment. A developer at 60% feature completion who also fixed 30 SP of bugs had different capacity constraints than one at 60% with no bug work. Cross-reference with the Bug Ratio tab for the full picture.

## Developer Throughput — Tickets Done

Count of feature tickets completed by this developer. Bug tickets are excluded. This complements the SP view — a developer completing many small feature tickets shows up here even if their SP total is modest. For bug ticket counts, see the Leaderboard tab or Bug Ratio tab.

## Developer Throughput — Tickets Carried Over

Count of feature tickets assigned to this developer that were not completed by sprint end. Bug tickets are excluded. A high carry-over count may indicate overcommitment on features, unexpected complexity, or capacity diverted to bug work. Check the developer's bug workload on the Bug Ratio tab for context before flagging carry-over as a concern.
