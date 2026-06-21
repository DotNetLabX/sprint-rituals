# Per-Developer Story Quality — Help

## Stories
The total number of feature stories assigned to this developer within the sprint's active scope. This uses the same scoping rules as the Throughput tab — stories that entered the workflow, were not removed, and are not bugs or excluded statuses. Attribution is based on who was assigned the story at the time of sync.

## Covered
The number of this developer's stories that have at least one linked test execution (excluding cancelled executions). A covered story means QA has created and linked a test execution to verify it — though the tests may not have been run yet.

## Coverage %
The percentage of this developer's stories that have linked test executions. A developer with 80% coverage has 4 out of 5 stories covered by testing. Low coverage may indicate QA capacity constraints, stories that are difficult to test, or stories that entered the sprint late. Use this metric in retro to discuss coverage gaps — not to judge individual developers.

## Pass Rate %
The percentage of test runs that passed across all test executions linked to this developer's stories. Only completed runs (pass or fail) are counted — pending, in-progress, and aborted runs are excluded. A low pass rate with high coverage suggests the stories are being tested but issues are being found. A high pass rate with low coverage suggests what is tested works well, but much of the scope is untested.

## Untested
The number of this developer's stories with no linked test executions at all. These are the coverage gaps — stories that QA has not started testing. Prioritize untested stories with higher story points, as they represent the largest unverified scope. This count is simply Stories minus Covered.

## Bugs Found
The number of unique bugs discovered during testing of this developer's stories. These are bugs linked to test executions via defect traceability links — they were found because QA tested the story and discovered an issue. A higher bugs-found count is not necessarily negative — it may mean QA is being thorough on complex stories. Compare with coverage to get the full picture: high bugs with high coverage means QA is catching issues early (good); high bugs with low coverage may indicate deeper quality concerns.

## Below Median Warning
An amber warning icon appears when a developer's coverage % has been below the team median for two or more consecutive sprints. The team median is the middle value — half the team is above, half below. This flag identifies developers whose stories are persistently under-tested relative to their peers. It is designed as a retrospective signal, not a performance indicator. Common causes include: stories in areas that are harder to test, late-sprint scope additions that QA could not reach, or QA capacity not distributed evenly. When a sub-team filter is active, the median is recalculated for that sub-team only.

## Coverage % Trend Chart
A multi-line chart showing each developer's coverage % across the selected sprints. Use this chart to spot trends: is a developer's coverage improving over time, declining, or staying flat? Consistent upward trends suggest the team is getting better at testing that developer's scope. Persistent low lines may warrant a conversation about why certain areas receive less QA attention. The chart appears only in multi-sprint view (Last 3, Last 5, or All).
