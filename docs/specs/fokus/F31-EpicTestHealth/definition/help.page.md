# Epic Test Health — Help Pages

## Average Test Coverage Card
The Average Test Coverage card shows the arithmetic mean of test coverage percentages across all epics in the current view (Active or Completed, after sub-team filter). An epic's coverage is the proportion of its feature tickets that have at least one linked Test Execution from Xray. Epics with zero feature tickets are excluded from the average. If coverage is low across many epics, it may indicate that QA capacity isn't keeping pace with development, or that certain epics are being prioritized for testing over others. Use this alongside the per-epic coverage column to identify which epics need attention.

## Coverage % Column
Coverage Rate measures what proportion of an epic's feature tickets have been tested at least once via a linked Test Execution. Bug tickets are excluded from the denominator because they represent defects, not features requiring coverage. A low coverage rate doesn't necessarily mean poor quality — it may mean the epic's stories haven't reached the testing phase yet, or that the team has deprioritized manual testing for that area. Compare coverage rate against the SP completion bar to see the gap between "built" and "tested." RAG coloring uses the same thresholds configured in Settings > Health > Quality.

## Pass Rate % Column
Pass Rate aggregates all individual test run results across the epic's linked Test Executions. Only completed runs (PASS or FAIL) count — runs still in TODO, EXECUTING, or ABORTED status are excluded. A declining pass rate may indicate that the epic's complexity is increasing, that test scenarios aren't matching the implementation, or that recent changes introduced regressions. Pass Rate is meaningful only for epics with some test coverage — an epic with 0% coverage will show 0% pass rate, which is a coverage gap, not a quality signal.

## Bugs Found Column
Bugs Found counts unique bug tickets that were discovered through Test Executions linked to this epic. These are bugs connected via Xray's "Blocks" link type — they represent defects found during structured testing, not all bugs filed against the epic. A high bug count paired with high coverage may actually be positive — it means testing is effective at finding issues before release. A low bug count with low coverage is ambiguous: either there are few bugs, or they haven't been found yet.

## Coverage Progress Bar
The coverage progress bar provides a visual comparison between development progress (SP completion, top bar) and testing progress (coverage rate, bottom bar). A large gap between the two bars suggests testing is trailing development. In a healthy delivery flow, the coverage bar should trend toward the SP bar over the course of the epic. Note: the SP bar includes imputed estimates for unestimated tickets, while the coverage bar counts only feature tickets — the scales are conceptually similar (both 0-100%) but measure different things.

## Test Status Badge
Each ticket in the expanded epic detail shows its individual test status. Passed (green) means all test runs across all linked Test Executions are PASS. Failed (red) means at least one run is FAIL — the worst result wins. In Progress (amber) means testing has started but isn't complete (TODO or EXECUTING runs, no FAIL). No Tests (gray) means no Test Execution has been linked to this ticket. Bug tickets don't show a test status because they are defects, not test targets. Use this view to quickly identify which stories in an epic still need testing attention.

## Ticket Pass Rate
Shows the pass rate for this specific ticket's linked Test Executions. Calculated the same way as the epic-level pass rate but scoped to one ticket. Useful for identifying the exact stories dragging down an epic's overall pass rate. A ticket with a low pass rate and multiple test runs may indicate a complex story with hard-to-verify requirements, or a story that was changed after tests were written.

## Ticket Bugs Found
Shows how many bugs were discovered specifically through testing this ticket's linked Test Executions. Connected via the "Blocks" link type in Xray. Tickets with high bug counts from testing may need extra review before the epic is considered shippable. This count is independent of any other bugs filed against the ticket outside of the TE workflow.
