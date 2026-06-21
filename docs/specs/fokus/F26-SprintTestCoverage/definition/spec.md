# Sprint Test Coverage

**Traces to:** `docs/product/v2.md` §5.1, §5.2, §6.2, §6.3
**Source:** Scratch
**Dependencies:** F25 (Xray Integration & Ticket Test Enrichment)
**Status:** Done
**Plan:** `docs/specs/F26-SprintTestCoverage/delivery/plan.md`

---

## Purpose

Surfaces sprint-level test coverage, execution, and pass rate metrics on the Dashboard. Answers "How well tested is this sprint?" by aggregating Xray test execution data synced by F25. Adds a Quality sub-score to the composite health score so testing health is visible alongside delivery health. Every metric has a tooltip explaining its formula and how it contributes to the health score — transparency first, so the team can understand the QA data before it starts influencing decisions.

## Entities

No new entities (AppSettings gains new fields — see Settings Extensions below). F26 reads from entities introduced by F25:
- **TestExecution** — Xray test execution issue, linked to sprint tickets via TestExecutionLink
- **TestExecutionLink** — pivot between TEs and tickets (Tests = coverage relationship, Blocks = bug discovery)
- **TestRun** — individual pass/fail result per test execution
- **TestSet** — feature area grouping (context only, not used for metrics)

### Settings Extensions

The existing AppSettings entity gains new fields, visible in the Settings UI only when Xray is enabled:

**QA Health Thresholds** — three threshold pairs (green/amber) controlling RAG coloring on QA metric cards:

| Metric | Green (default) | Amber (default) | Polarity |
|--------|----------------|-----------------|----------|
| Coverage Rate | ≥ 80% | ≥ 50% | Higher is better |
| Execution Rate | ≥ 80% | ≥ 50% | Higher is better |
| Pass Rate | ≥ 90% | ≥ 70% | Higher is better |

**Quality Health Weight** — integer, 0–100, default 20. How much the Quality sub-score contributes to the composite health score. This weight is additive — the existing delivery weights (Completion/Disruption/CarryOver) continue to sum to 100 among themselves, and the Quality weight is added on top. totalWeight for the composite formula = delivery weight sum + Quality weight (e.g., 100 + 20 = 120 at defaults). The existing sum-to-100 validation for delivery weights is unchanged.

**Quality Sub-Score Weights** — two integers controlling how Coverage Rate and Pass Rate combine within the Quality sub-score:
- Coverage Weight: default 50, minimum 1
- Pass Rate Weight: default 50, minimum 1
- At least one weight must be ≥ 1 (both cannot be zero — prevents division by zero in the sub-score formula)

## User Flows

```
Flow 1: View QA Metrics on Dashboard
1. User navigates to Dashboard with a sprint selected
2. Below the delivery metrics section, system displays a "Test Quality" section
3. Section contains three metric cards: Coverage Rate, Execution Rate, Pass Rate
4. Each card shows: percentage value, delta vs prior sprint (arrow + signed number, colored by polarity), 4-sprint sparkline, RAG indicator based on configurable thresholds
5. Each card has an info tooltip explaining its formula
6. Below the cards: "Bugs found: N" annotation
7. Below annotation: two expandable sections
8. User expands "Untested tickets (N)" — sees ticket key, summary, assignee, SP for each uncovered ticket
9. User expands "Failing tickets (N)" — sees ticket key, summary, assignee, SP, failed/total runs for each failing ticket
```

```
Flow 2: Health Score with Quality Sub-Score
1. User views Dashboard health score badge
2. Composite health score includes Quality sub-score when Xray is enabled and sprint has QA data
3. Health score tooltip/breakdown shows all sub-scores: Completion, Disruption, Carry-Over, Quality — each with its weight and computed score
4. Quality line shows its internal breakdown: Coverage Rate contribution + Pass Rate contribution with their configured weights
5. When Xray is disabled or sprint has no QA data, Quality row is absent — health score uses delivery sub-scores only
```

```
Flow 3: Configure QA Health Settings
1. User navigates to Settings > Health section
2. When Xray is enabled, a "Quality" sub-group appears below existing delivery thresholds
3. User configures:
   a. Coverage Rate thresholds (green/amber percentages)
   b. Execution Rate thresholds (green/amber percentages)
   c. Pass Rate thresholds (green/amber percentages)
   d. Quality Health Weight (contribution to composite health score)
   e. Coverage / Pass Rate internal weights (how they combine within the Quality sub-score)
4. System saves settings; Dashboard recalculates on next load
```

```
Flow 4: Empty States
1a. Xray disabled → "Test Quality" section not rendered. Health score delivery-only.
1b. Xray enabled, sprint not yet synced → Section shows "Sync sprint to load QA data" prompt.
1c. Xray enabled, synced, zero TEs found → Cards show 0%, ticket lists empty, Quality sub-score is 0.
```

## API Surface

| Method | Route | Auth | Request body | Response | Status codes |
|--------|-------|------|-------------|----------|-------------|
| GET | /api/sprints/{sprintId}/qa-metrics | Authenticated | — | QaMetricsResponse | 200, 404 |
| GET | /api/sprints/{sprintId}/qa-metrics/untested | Authenticated | — | UntestedTicketsResponse | 200, 404 |
| GET | /api/sprints/{sprintId}/qa-metrics/failing | Authenticated | — | FailingTicketsResponse | 200, 404 |

All three endpoints accept optional `?subTeam=X` query parameter.

**QaMetricsResponse:**
- hasQaData: boolean (false when Xray disabled or sprint not synced — UI uses this to choose empty state)
- coverageRate: MetricCard (value, delta, direction, sparklinePoints[])
- executionRate: MetricCard (value, delta, direction, sparklinePoints[])
- passRate: MetricCard (value, delta, direction, sparklinePoints[])
- bugsFound: integer
- qualitySubScore: decimal (0–100)
- qualityBreakdown: { coverageScore, coverageWeight, passRateScore, passRateWeight } (for health score tooltip)
- untestedCount: integer
- failingCount: integer

MetricCard follows existing pattern: value (decimal), delta (decimal, nullable), direction (up|down|flat|null), sparklinePoints (list of {sprintName, value}, up to 4 trailing sprints).

**UntestedTicketsResponse:**
- tickets: list of { ticketKey, summary, assigneeName, storyPoints }

**FailingTicketsResponse:**
- tickets: list of { ticketKey, summary, assigneeName, storyPoints, failedRunCount, totalRunCount }

**Error conditions:**
- 404 when sprintId does not exist
- 200 with hasQaData=false and zero values when Xray is disabled or no QA data — not an error

**Settings endpoints (extensions to existing):**
- GET /api/settings gains: qaHealthThresholds (coverage, execution, passRate green/amber), qualityHealthWeight, qualitySubScoreWeights (coverageWeight, passRateWeight)
- PUT /api/settings/health-config (existing endpoint) gains the QA health fields: qaHealthThresholds, qualityHealthWeight, qualitySubScoreWeights. Extends the existing health config save — not a separate endpoint. All gated behind Xray enabled in the UI; stored regardless in the database.

**Existing health score response extension:**
- The existing sprint summary response's health score gains: qualitySubScore (decimal, nullable — null when Quality excluded), qualityRag (string, nullable), and qualityBreakdown (object, nullable). These fields are null when Xray is disabled or no QA data exists, preserving backward compatibility for clients that don't read them.

## Business Rules

### Metric Formulas

1. **Coverage Rate** = (feature tickets with at least one non-Cancelled linked TE where linkType = Tests) / (total feature tickets in sprint active scope) × 100. The denominator uses the same ticket set as delivery metrics — tickets that transitioned to the cycle time start boundary or beyond, not removed, not bugs, not excluded-from-scope statuses.

2. **Execution Rate** = (covered tickets where at least one linked TE has a test run with status PASS or FAIL) / (covered tickets) × 100. Denominator is covered tickets from rule 1's numerator. A TE is "executed" when it has at least one terminal-status run (PASS or FAIL). TODO, EXECUTING, and ABORTED runs do not count as executed. *Note: v2.md §5.2 defines Execution Rate using the TE's Jira issue status ("status Done"). This spec uses test run terminal status instead, because it measures actual execution evidence — a TE can have Jira status "Done" with only ABORTED runs (no real execution), or Jira status "In Progress" with completed PASS/FAIL runs. Test run status is the more reliable signal.*

3. **Pass Rate** = (test runs with PASS status) / (test runs with PASS or FAIL status, across all non-Cancelled TEs in the sprint) × 100. TODO, EXECUTING, and ABORTED runs are excluded from both numerator and denominator.

4. **Bugs Found** = count of unique bug tickets linked via Blocks links from non-Cancelled TEs in the sprint. A bug linked from multiple TEs counts once.

### Aggregation Rules

5. **Worst-result at ticket level:** If a ticket has multiple TEs and any TE has a FAIL run, the ticket's overall status is FAIL for the failing tickets list. Both pass and fail counts are tracked per ticket.

6. **Worst-result at TE level:** If a TE has multiple test runs and any run is FAIL, the TE is considered FAIL regardless of other passing runs.

7. **Cancelled TEs** (those with Jira status indicating cancellation, stored with IsCancelled = true) are excluded from all metric computations — coverage, execution, pass rate, bugs found, and ticket lists.

8. **Sprint scoping:** A TE belongs to a sprint through its linked tickets' sprint membership. When a TE links to tickets in multiple sprints, it is attributed to the most recent sprint for aggregation purposes.

9. **Sub-tasks:** Sub-tasks with their own TE links use those links. Sub-tasks without own TEs inherit parent ticket's coverage status for aggregation.

10. **TE linked to multiple tickets:** Creates one TestExecutionLink per ticket. Each link counts independently — the TE contributes coverage to all linked tickets.

### Division by Zero

11. Zero feature tickets in sprint active scope → Coverage Rate = 0%.
12. Zero covered tickets → Execution Rate = 0%.
13. Zero executed test runs (PASS + FAIL) → Pass Rate = 0%.

### Validation

14. **Quality sub-score weights** must each be ≥ 1. Both cannot be zero (division by zero in the sub-score formula). The save endpoint rejects requests where both are zero.

15. **Quality Health Weight** must be 0–100. Setting it to 0 shows QA metrics on the dashboard without affecting the health score — useful for observation before integration.

### Health Score Integration

16. **Quality sub-score** = (coverageScore × coverageWeight + passRateScore × passRateWeight) / (coverageWeight + passRateWeight). Both coverageScore and passRateScore use the higher-is-better interpolation: value ≥ green threshold → 100; amber ≤ value < green → linear 50–99; value < amber → linear 0–49, clamped to 0.

17. **Additive weight model:** The Quality Health Weight is additive to the existing delivery weights. Delivery weights (Completion + Disruption + CarryOver) continue to sum to 100 among themselves — the existing sum-to-100 validation is unchanged. totalWeight = delivery sum + Quality weight. At defaults: 100 + 20 = 120.

18. **Composite health score** gains Quality as a fourth sub-score: (completionScore × completionWeight + disruptionScore × disruptionWeight + carryOverScore × carryOverWeight + qualityScore × qualityWeight) / totalWeight. totalWeight = sum of all active weights. Existing composite RAG bands (green ≥ 75, amber ≥ 40, red < 40) unchanged.

19. **Graceful exclusion:** When Xray is disabled OR sprint has no QA data (hasQaData = false), qualityWeight is excluded from totalWeight. The composite formula reverts to delivery-only without any user action.

20. **Execution Rate** has its own RAG thresholds for card coloring but does NOT contribute to the Quality sub-score. Only Coverage Rate and Pass Rate feed the sub-score.

### UI & Display

21. **Delta calculation:** Current sprint value minus previous sprint value. Direction: up if delta > 0, down if delta < 0, flat if delta = 0. All three QA metrics are higher-is-better (green arrow up, red arrow down). Delta is null when no prior sprint has QA data.

22. **Sparkline:** Trailing 4-sprint window ending at the selected sprint. Only includes sprints that have been QA-synced. Shows fewer points when fewer than 4 sprints have QA data.

23. **Sub-team filter:** When subTeam is specified, all QA metric denominators and numerators scope to tickets assigned to developers in that sub-team. Same filtering pattern as delivery metrics.

24. **Feature flag gate:** When Xray is disabled, the entire QA section is hidden, QA endpoints return hasQaData=false with zero values, and the health score excludes Quality. Previously synced QA data remains in the database and reappears when Xray is re-enabled.

25. **Untested ticket list** sorted by SP descending (highest unprotected impact first).

26. **Failing ticket list** sorted by failed run count descending (worst failures first).

## Acceptance Criteria

### Dashboard — QA Section

- [ ] "Test Quality" section appears below delivery metrics when Xray is enabled and sprint has QA data
- [ ] Coverage Rate card shows percentage, delta vs prior sprint, 4-sprint sparkline, RAG color
- [ ] Execution Rate card shows percentage, delta vs prior sprint, 4-sprint sparkline, RAG color
- [ ] Pass Rate card shows percentage, delta vs prior sprint, 4-sprint sparkline, RAG color
- [ ] Each QA card has an info tooltip explaining its formula in plain language
- [ ] "Bugs found: N" annotation appears below the QA cards (hidden when 0)
- [ ] "Untested tickets (N)" expandable list shows ticket key, summary, assignee, SP — sorted by SP descending
- [ ] "Failing tickets (N)" expandable list shows ticket key, summary, assignee, SP, failed/total runs — sorted by failed runs descending
- [ ] Untested list is empty when all feature tickets have linked TEs
- [ ] Failing list is empty when all covered tickets have only PASS runs

### Health Score

- [ ] Composite health score includes Quality sub-score when Xray enabled and QA data exists
- [ ] Health score tooltip shows all sub-scores: Completion, Disruption, Carry-Over, Quality with their weights and scores
- [ ] Quality sub-score tooltip shows Coverage Rate and Pass Rate contributions with their configured weights
- [ ] Health score excludes Quality sub-score when Xray is disabled
- [ ] Health score excludes Quality sub-score when sprint has no QA data (not synced or zero TEs)
- [ ] Composite RAG bands (75/40) unchanged after Quality integration

### Settings

- [ ] "Quality" sub-group appears in Health settings when Xray is enabled
- [ ] "Quality" sub-group hidden when Xray is disabled
- [ ] Coverage Rate green/amber thresholds are configurable (default 80/50)
- [ ] Execution Rate green/amber thresholds are configurable (default 80/50)
- [ ] Pass Rate green/amber thresholds are configurable (default 90/70)
- [ ] Quality Health Weight is configurable (default 20)
- [ ] Coverage/Pass Rate internal weights are configurable (default 50/50, minimum 1 each)
- [ ] Quality Health Weight accepts 0 (QA metrics visible but health score unaffected)
- [ ] Save rejects request if both Coverage and Pass Rate weights are zero

### Empty States

- [ ] Xray disabled: entire QA section hidden, health score delivery-only
- [ ] Xray enabled, sprint not synced: QA section shows "Sync sprint to load QA data" prompt
- [ ] Xray enabled, synced, zero TEs: cards show 0%, lists empty, Quality sub-score is 0

### API

- [ ] GET /api/sprints/{sprintId}/qa-metrics returns 200 with all metric data and hasQaData indicator
- [ ] GET /api/sprints/{sprintId}/qa-metrics/untested returns untested ticket list
- [ ] GET /api/sprints/{sprintId}/qa-metrics/failing returns failing ticket list
- [ ] All QA endpoints accept ?subTeam=X and scope results accordingly
- [ ] All QA endpoints return 404 for non-existent sprint
- [ ] All QA endpoints return 200 with hasQaData=false when Xray disabled

### Cross-Cutting

- [ ] Cancelled TEs excluded from all computations
- [ ] Sub-team filter works on all QA metrics
- [ ] Delta is null when no prior sprint has QA data

## Out of Scope

- **Per-developer QA breakdown** — deferred to F28 (Per-Developer Story Quality). F26 is sprint-level only.
- **QA workload distribution** — deferred to F29 (QA Workload & Throughput). Who executed tests, volume per person.
- **Test execution timeline** — deferred to F30 (Test Execution Timeline). When tests ran relative to sprint lifecycle.
- **Epic-level test health** — deferred to F31 (Epic Test Health). Bottom-up epic coverage from stories.
- **Cross-sprint QA trends** — deferred to F27 (Cross-Sprint QA Trends). Multi-sprint trend lines.
- **Test Set-based metrics** — TestSet entities exist but are context only. No TestSet-scoped analytics.
- **Dedicated QA sidebar page** — deferred to F29. F26 adds a Dashboard section only.

## Open Questions

None — all resolved during discussion.
