# F30 Test Execution Timeline — Help Pages

Guide page content for F30 UI elements. Each section explains what the element shows, how to interpret it, and what actions to take.

---

## Test Execution Burnup Chart

The test execution burnup chart shows when your team's testing actually happened during the sprint. The X-axis is sprint days, and the Y-axis is the cumulative count of completed test runs. Three lines track progress: green for passing tests, red for failing tests, and gray for the combined total.

A healthy sprint shows steady upward progress starting in the first half of the sprint — testing keeps pace with development. If the lines stay flat for most of the sprint and then spike sharply in the last few days, your team is back-loading QA work, which increases the risk of discovering critical issues too late to fix within the sprint.

Look for the gap between the green (PASS) and gray (total) lines. A wide gap means many tests are failing. If that gap appears early, the team has time to fix issues. If it appears only in the last days, failures may carry over to the next sprint.

## Planning Zone (Chart Shading)

The planning zone covers the first days of the sprint (matching your configured planning window, default 2 days). Test runs appearing in this zone indicate the team is testing early — often re-running regression tests or validating carried-over work from the previous sprint.

Some test activity here is healthy and indicates shift-left testing practices. However, a large volume of test runs in the planning zone before development has started may indicate the team is catching up on testing from the previous sprint rather than testing proactively.

## Execution Zone (Chart Shading)

The execution zone covers the core sprint days between the planning window and the last 2 days. This is where the majority of test execution should happen — developers complete stories and QA validates them in a continuous flow.

Steady test execution throughout this zone indicates good development-to-testing handoff. Long flat periods followed by bursts suggest developers are batching work or QA is waiting for multiple stories before starting testing.

## Testing Crunch Zone (Chart Shading)

The testing crunch zone highlights the last 2 calendar days before the sprint ends. A disproportionate amount of testing in this zone is a warning sign — it means the team is cramming QA into the final hours of the sprint.

When more than 50% of all test runs land in this zone, the Testing Crunch flag fires. This pattern often leads to superficial testing, missed edge cases, and defects escaping to production. If you see this pattern consistently, consider whether sprint scope is too ambitious, whether development is finishing too late for adequate testing, or whether QA capacity needs rebalancing.

## Scope Change Markers

Vertical markers on the burnup chart show days when tickets were added to or removed from the sprint. Each marker displays the net story point change for that day (e.g., "+5 SP" or "-3 SP").

These markers help you see the relationship between delivery disruptions and testing delays. A common pattern: scope additions in the middle of the sprint push testing later because the team must develop the new work first. If you see scope markers on days 5-6 and testing activity spiking only on days 9-10, the disruption is directly causing the testing crunch.

Use this correlation to make the case for scope stability — data showing that mid-sprint additions cause late testing is more persuasive than a general "scope changes are bad" argument.

## Testing Crunch Flag

The testing crunch flag fires when more than 50% of all completed test runs (PASS or FAIL) within the sprint boundary happened in the last 2 days. It appears in the Dashboard flags section alongside other sprint health flags.

This flag is a leading indicator of quality risk. Sprints with heavy testing crunch often have lower pass rates (from F26) and higher carry-over rates — there simply is not enough time to fix issues found at the last minute.

When this flag fires consistently across multiple sprints, it signals a systemic process issue rather than a one-time scheduling problem. Discuss it in retrospectives with the specific data: which tickets were tested late, and why.

## Testing Crunch — Ticket List

This expandable list shows every ticket whose test runs completed in the last 2 sprint days. Each entry includes the ticket key, summary, assignee, story points, and the count of late test runs.

The list is sorted by late run count descending — tickets with the most test runs crammed into the final days appear first. Use this to identify which stories consistently end up being tested late. If the same team members' stories appear here repeatedly, it may indicate a development pacing issue rather than a QA scheduling issue.

## Post-Sprint Testing

This metric shows what percentage of the sprint's test runs completed after the sprint officially closed. Post-sprint testing means the team did not finish QA within the sprint boundary — the definition of done was not met during the sprint.

A small amount of post-sprint testing (under 10%) may be acceptable for edge cases, but consistently high post-sprint testing undermines the sprint commitment model. It means the team is reporting work as "done" in sprint reviews before testing confirms it actually works.

If this metric is high, investigate whether sprint duration is sufficient for the amount of work committed, whether testing is starting too late in the sprint, or whether the team needs to reduce scope to allow time for QA.

## Post-Sprint Testing — Ticket List

This expandable list shows tickets whose test runs completed after the sprint closed. Each entry includes the ticket key, summary, assignee, story points, and the count of post-sprint test runs.

Use this list to identify which stories slipped past the sprint boundary for testing. If specific story types (large features, complex integrations) consistently appear here, consider whether they need to be committed earlier in the sprint or broken into smaller pieces that can be tested within the sprint window.

## Completed but Untested by Sprint Close

This section lists tickets that completed development before the sprint ended but received no test results by sprint close. Unlike F26's "untested tickets" (which flags tickets with no linked Test Executions at all), this catches a subtler gap: the ticket has linked Test Executions, but those TEs were not actually executed in time.

This is one of the most actionable signals in the Test Execution Timeline. A ticket that finished development on day 5 of a 10-day sprint had 5 days available for testing — if it still was not tested by sprint close, something went wrong in the handoff or prioritization. The list is sorted by dev-done date ascending so the worst cases (tickets that had the most time available for testing but still were not tested) appear first.

When this section appears consistently, investigate whether QA is aware of which stories are dev-complete and ready for testing. Often, the issue is not capacity but visibility — QA does not know a story is ready until the developer mentions it in standup, by which time several days may have passed. Improving the development-to-QA handoff signal (moving tickets to a "Ready for Testing" status, for example) directly reduces this list.

## Dev-to-Test Gap (Median)

The dev-to-test gap measures how many calendar days pass between a ticket being marked as development-complete (reaching the cycle time end boundary) and receiving its first test result (PASS or FAIL). The median across all eligible tickets in the sprint is the headline number.

A low median gap (0-1 days) indicates tight development-to-QA handoff — stories are tested almost immediately after development finishes. A high median gap (3+ days) indicates QA queue buildup — finished stories sit waiting for testing.

This metric is a direct measure of flow efficiency in the testing phase. Improving it usually requires either earlier QA involvement (testing stories as they complete rather than in batches) or better capacity balancing between development and QA.

Negative values are possible and healthy — they mean testing started before the ticket officially reached the completion boundary, indicating proactive QA practices.

## Dev-to-Test Gap — Delta

The delta shows how the median gap changed compared to the previous sprint. A green downward arrow means the gap is shrinking — your team is testing faster after development completes. A red upward arrow means the gap is growing — QA queue time is increasing.

Track this trend across sprints. A consistently growing gap is an early warning that QA capacity is not keeping pace with development throughput. Address it before it leads to testing crunch and post-sprint testing.

## Dev-to-Test Gap — Ticket Table

This table shows the detailed breakdown for every ticket that has both a development completion date and at least one test result. Each row shows when development finished, when the first test ran, and the calendar day gap between them.

The table is sorted by gap descending — tickets that waited the longest for testing appear first. Use this to identify bottleneck patterns: Are certain types of tickets (large features, specific assignees, specific sub-teams) consistently waiting longer for testing? Are there specific sprint days where a batch of tickets completed development but testing did not start until days later?

This data turns vague "testing feels slow" complaints into specific, actionable evidence for retrospectives and process improvements.
