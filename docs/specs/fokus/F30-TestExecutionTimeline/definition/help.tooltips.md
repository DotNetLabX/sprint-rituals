# F30 Test Execution Timeline — Tooltips

Tooltip text for info icons on F30 UI elements. Each tooltip must be under 150 characters.

---

## Test Execution Burnup Chart

Cumulative test runs completed per sprint day. Green = PASS, Red = FAIL, Gray = Total. Shows when testing happened.

## Planning Zone (Chart Shading)

Sprint days 1-2 (configurable). Testing here indicates shift-left practices — proactive QA during planning.

## Execution Zone (Chart Shading)

Core sprint days between planning and the last 2 days. Most testing should happen here.

## Testing Crunch Zone (Chart Shading)

Last 2 days before sprint end. High test volume here signals back-loaded QA — a sprint health risk.

## Scope Change Markers

Vertical markers showing when tickets were added or removed mid-sprint. Compare with testing activity nearby.

## Testing Crunch Flag

Fires when >50% of test runs completed in the last 2 sprint days. Shows percentage and affected ticket count.

## Testing Crunch — Ticket List

Tickets whose test runs completed in the last 2 sprint days. Sorted by late run count — worst offenders first.

## Post-Sprint Testing

Percentage of test runs that completed after the sprint closed. These tests missed the sprint boundary.

## Post-Sprint Testing — Ticket List

Tickets with test runs after sprint close. Sorted by post-sprint run count descending.

## Completed but Untested by Sprint Close

Tickets that finished development before sprint end but had no test results by close. A testing gap signal.

## Dev-to-Test Gap (Median)

Median calendar days between dev completion and first test result. Lower is better — less QA queue time.

## Dev-to-Test Gap — Delta

Change vs prior sprint. Green down arrow = gap shrinking (faster testing). Red up arrow = gap growing.

## Dev-to-Test Gap — Ticket Table

Per-ticket breakdown: when dev finished, when first test ran, and the gap between them. Longest waits first.
