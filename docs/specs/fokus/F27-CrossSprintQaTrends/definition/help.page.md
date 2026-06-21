# Cross-Sprint QA Trends — Guide

## Quality Trends Chart

This chart tracks three quality indicators across your recent sprints. Look for sustained upward movement — a rising coverage rate means more of your stories are being tested before release. If pass rate dips while coverage stays flat, your existing tests may be catching regressions that would otherwise escape to production. A flat or declining execution rate alongside rising coverage suggests test executions are being created but not completed in time — a signal to discuss QA bandwidth in retro.

## Coverage Rate Line

Coverage rate measures how many of your feature stories have at least one linked test execution. A team trending upward here is expanding the safety net sprint over sprint. If coverage drops suddenly, check whether scope additions late in the sprint crowded out testing time — the Testing Volume chart below will show whether TE count dropped in the same sprint.

## Pass Rate Line

Pass rate shows the percentage of individual test runs that passed. A consistently high pass rate (above 90%) indicates stable software quality. A declining pass rate alongside stable coverage often means the codebase is becoming harder to test — consider whether recent architecture changes or technical debt are increasing failure rates.

## Execution Rate Line

Execution rate measures how many covered tickets actually had their test executions completed. A gap between coverage rate and execution rate means test executions exist but haven't been run to completion. Persistent gaps here may indicate that QA is falling behind development pace — consider whether the team needs to rebalance capacity.

## Testing Volume Chart

This chart shows absolute counts: how many test executions were linked to sprint tickets and how many bugs those executions found. Use it alongside the percentage charts above — a coverage rate increase is more meaningful when TE count is also growing (broader testing) than when it comes from fewer stories in scope. A rising bugs-found count is a positive signal: your tests are catching issues before they reach production.

## TE Count

The number of test execution issues linked to sprint tickets. This reflects testing effort in absolute terms. Compare against coverage rate — if coverage increases but TE count stays flat, it may mean you have fewer stories to cover rather than more testing being done. If TE count drops and coverage drops with it, the team may be under-investing in test creation.

## Bugs Found

Bugs discovered through test executions in each sprint. An upward trend means your tests are surfacing more defects before release — this is usually positive. Compare against the defect correlation section below: if bugs found in testing are rising but next-sprint bug ratios are falling, your testing investment is paying off.

## Defect Correlation

This section reveals whether your test coverage investment is paying off by comparing coverage in one sprint against the bug ratio in the following sprint. The N+1 lag reflects how escaped defects work: low coverage in sprint 5 means untested code ships, and those untested paths surface as bug work in sprint 6. If the coverage line drops and the bug ratio line rises one sprint later, you have evidence that testing gaps are causing downstream quality problems. Use this in retro to advocate for QA capacity — the data connects testing effort directly to defect outcomes.

## Pearson r Badge

The Pearson correlation coefficient quantifies the relationship between your test coverage and next-sprint bug ratio. A value near −1.0 means higher coverage strongly predicts fewer bugs in the next sprint — your testing is highly effective. A value near 0 means there is no clear relationship — other factors (code complexity, team changes, scope) may dominate. A positive value is unusual and suggests investigating confounding factors. The badge requires at least 6 sprints of overlapping data to produce a statistically meaningful result.
