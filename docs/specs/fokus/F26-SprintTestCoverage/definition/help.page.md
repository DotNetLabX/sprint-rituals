# F26 — Sprint Test Coverage: Guide Pages

Guide page content for each UI element. Explains interpretation and recommended actions.

---

## Coverage Rate

Coverage Rate measures what percentage of your sprint's feature tickets have at least one linked test execution. A ticket is "covered" if any non-cancelled test execution is linked to it with a "Tests" relationship in Jira. The denominator is the same set of feature tickets used by delivery metrics — tickets that entered the sprint's active scope (transitioned past the cycle time start boundary), excluding bugs and excluded-from-scope statuses. If Coverage Rate is low, check whether test executions are being linked to stories in Jira, or whether some stories genuinely have no test plan. Consider prioritizing untested tickets with the highest story points for the greatest risk reduction.

## Execution Rate

Execution Rate measures what percentage of covered tickets have had their tests actually run to completion. A test execution is considered "executed" when at least one of its test runs has a terminal status (PASS or FAIL). Tests still in TODO, EXECUTING, or ABORTED state don't count. The denominator is covered tickets (from Coverage Rate's numerator), not all tickets. If Execution Rate is significantly lower than Coverage Rate, it means tests are linked but not yet run — a sign that QA execution is lagging behind test planning. Check whether test runs are blocked or if the sprint is still in progress.

## Pass Rate

Pass Rate measures the percentage of executed test runs that passed across all test executions in the sprint. Only test runs with PASS or FAIL status are counted — TODO, EXECUTING, and ABORTED runs are excluded from both numerator and denominator. This is computed at the test run level (not the ticket level), so a ticket with 10 passing runs and 1 failing run contributes 10 passes and 1 fail. A declining Pass Rate across sprints may indicate increasing code complexity, insufficient code review, or regression issues. Investigate the Failing Tickets list to identify which stories are driving failures.

## Bugs Found

Bugs Found counts the unique bug tickets discovered through test execution, identified by "Blocks" links from test executions in Jira. If a single bug is linked from multiple test executions, it counts once. This number provides context on how productive testing was at finding defects. A high bugs-found count alongside a high pass rate may indicate good test effectiveness — tests are passing where expected and catching real issues. A high bugs-found count with a low pass rate suggests broader quality problems.

## Untested Tickets

The Untested Tickets list shows all feature tickets in the sprint's active scope that have no linked test executions. These are your coverage blind spots — stories shipping without any formal test validation. The list is sorted by story points descending, putting the highest-risk untested work at the top. Not every ticket necessarily needs a test execution (small config changes, documentation), but high-SP feature tickets without tests warrant attention. Use this list in sprint retrospectives to discuss whether coverage gaps were intentional or overlooked.

## Failing Tickets

The Failing Tickets list shows feature tickets where at least one linked test execution has a FAIL test run. For each ticket, you can see the failed run count and total run count to gauge severity. Sorted by failed run count descending — tickets with the most failures appear first. A ticket with many failing runs across multiple test executions is a stronger signal than a single isolated failure. Use this list to prioritize bug fixes and re-testing before sprint close.

## Quality Sub-Score

The Quality sub-score combines Coverage Rate and Pass Rate into a single 0–100 score that feeds the composite health score. Each metric is scored using the same interpolation as delivery sub-scores: at or above the green threshold = 100, between amber and green = 50–99 (linear), below amber = 0–49 (linear). The two scores are then combined using configurable weights (default 50/50). Execution Rate intentionally does not contribute to the sub-score — it's an intermediate signal, not a final quality outcome. The Quality sub-score's contribution to the overall health score is controlled by the Quality Health Weight setting (default 20). When Xray is disabled or no QA data exists, Quality is excluded from the health score entirely.

## Quality Health Weight

This setting controls how much the Quality sub-score influences the composite health score. The Quality weight is additive — delivery weights (Completion, Disruption, CarryOver) continue to sum to 100 among themselves, and Quality is added on top. At the default of 20, the total is 120, so Quality carries about 17% of the composite. Set to 0 to see QA metrics on the dashboard without affecting the health score — useful when first adopting test coverage tracking. Increase the weight as you gain confidence in the QA data quality. The delivery weight sum-to-100 rule is unchanged.

## Coverage Rate Thresholds

These thresholds control the RAG (Red/Amber/Green) coloring on the Coverage Rate card and the Coverage component of the Quality sub-score. Default: green at ≥ 80% (strong coverage), amber at ≥ 50% (moderate coverage), red below 50% (significant coverage gaps). Adjust based on your team's maturity — a team new to test execution linking might start with lower thresholds (e.g., green at 60%) and raise them over time.

## Execution Rate Thresholds

These thresholds control the RAG coloring on the Execution Rate card. Default: green at ≥ 80%, amber at ≥ 50%. Note that Execution Rate does not feed the Quality sub-score — these thresholds only affect the card's visual indicator. A low execution rate with high coverage means tests are planned but not yet run, which is expected mid-sprint but concerning at sprint close.

## Pass Rate Thresholds

These thresholds control the RAG coloring on the Pass Rate card and the Pass Rate component of the Quality sub-score. Default: green at ≥ 90% (most tests passing), amber at ≥ 70% (notable failures), red below 70% (widespread failures). Pass Rate thresholds are typically set higher than Coverage thresholds because once tests are written and run, a high pass rate is the expected norm — failures should be the exception.

## Coverage / Pass Rate Weights

These weights control how Coverage Rate and Pass Rate combine within the Quality sub-score. Default is 50/50 (equal weight). Each weight must be at least 1. If your team's primary challenge is getting tests written at all, bias toward Coverage (e.g., 60/40). If coverage is strong but test quality is the concern, bias toward Pass Rate (e.g., 40/60). The weights are relative — 50/50 and 1/1 produce the same result.
