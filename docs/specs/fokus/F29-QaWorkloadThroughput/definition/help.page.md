# QA Workload & Throughput — Help Guide

## Total TEs Card

This card shows how many Test Execution issues from Xray are linked to tickets in the selected sprint(s). Each TE represents a testing session — it may contain multiple individual test runs inside it. A higher count generally means more testing activity, though the number of TEs alone doesn't indicate quality. Compare with Runs Completed and Pass Rate for a fuller picture. In single-sprint mode, the delta shows how TE volume changed versus the prior sprint.

## Total Runs Completed Card

This card counts individual test runs that reached a final result — either pass or fail. Runs still in progress (TODO, EXECUTING) or cancelled (ABORTED) are not counted. This is the team's raw testing throughput. A declining trend may indicate reduced testing capacity or fewer stories requiring testing. In single-sprint mode, the delta shows whether the team executed more or fewer tests than the prior sprint.

## Team Pass Rate Card

The percentage of completed test runs that passed across all testers. Only runs with a definitive pass or fail are included — in-progress and aborted runs are excluded. The RAG color uses the same thresholds as the Dashboard's pass rate card (configurable in Settings). A low pass rate may indicate code quality issues, environmental problems, or tests that need updating. Track this alongside the Dashboard's sprint-level pass rate for consistency.

## Workload Distribution Chart

This horizontal bar chart shows each team member's testing volume, split into pass (green) and fail (red) segments. People are sorted by total runs from highest to lowest. Use this to quickly spot imbalances — if one person's bar is dramatically longer than others, testing work may not be well distributed. The chart aggregates across all selected sprints in multi-sprint mode. In single-sprint mode, it shows only the current sprint's data.

## Execution Throughput Trend Chart

This line chart tracks each person's completed test runs per sprint over the selected range. It helps identify trends: is someone's testing throughput increasing, decreasing, or stable? A sudden drop might indicate reassignment, absence, or a shift in sprint composition. Compare individual trends against the team's overall testing volume to understand whether changes are person-specific or team-wide. This chart only appears in multi-sprint mode — a single sprint provides only one data point.

## TEs Owned Column

The number of Test Execution issues assigned to this person in the selected sprint(s). TE ownership is determined by the Xray assignee field — it reflects who is responsible for the testing session, regardless of who actually executed individual test runs inside it. If ownership and execution frequently differ, consider reviewing TE assignment practices.

## Runs Completed Column

The total number of test runs this person executed that reached a final result (pass or fail). Attribution uses who actually ran the test (Executed By field in Xray). When this field is not recorded, the system falls back to the TE owner. This metric reflects actual testing effort — a person with high Runs Completed is actively executing tests, not just assigned to them.

## Pass Column

The number of test runs executed by this person that passed. A high pass count combined with a high pass rate indicates effective testing of well-built features. A high pass count with a low pass rate means this person is doing a lot of testing but finding many failures — which could indicate they're assigned to problematic areas or are thorough testers catching real issues.

## Fail Column

The number of test runs executed by this person that failed. Failed runs are not inherently negative — they represent quality gates catching issues before release. A high fail count may indicate the person is testing complex or risky features, or that the code they're testing has quality issues. Compare with Bugs Found to see how many failures led to bug reports.

## Pass Rate Column

The percentage of this person's completed runs that passed. Color-coded using the same thresholds as the Dashboard pass rate: green (90%+), amber (70-89%), red (below 70%) at default settings. A consistently low pass rate for one person may warrant investigation — are they assigned to particularly buggy features, or are there environmental issues affecting their test runs? Compare across team members to identify patterns.

## Stories Covered Column

The number of unique sprint tickets that this person helped test — counted by having at least one test run on a TE linked to that ticket. This shows testing breadth: is this person covering many stories or focused deeply on a few? A person with high Runs Completed but low Stories Covered is doing deep testing on fewer tickets. A person with the reverse is doing broad but shallow coverage.

## Bugs Found Column

The number of unique bugs discovered through test executions owned by this person. Bug discovery is attributed to the TE owner (not the run executor), since the TE owner is responsible for the testing session that surfaced the defect. Bugs are identified via Xray's "Blocks" issue links from TEs. A higher count here is valuable — it means testing is catching issues before they reach users.

## Workload Balance Flag

A warning badge appears next to a person's name when they handle more than 50% of all test executions for two or more consecutive sprints. This flags a concentration anti-pattern — when one person becomes the testing bottleneck, the team is vulnerable to delays if that person is unavailable. Consider redistributing testing assignments or cross-training team members. The alert is computed from the full sprint history, not just the sprints currently displayed.
