# Epic Test Health

**Traces to:** `docs/product/v2.md` §5.7
**Source:** Scratch
**Dependencies:** F25 (Xray Integration & Ticket Test Enrichment), F14 (Epic Progress — existing Epics page), F26 (Sprint Test Coverage — QA metric patterns, health thresholds)
**Status:** Done
**Plan:** `docs/specs/F31-EpicTestHealth/delivery/plan.md`

---

## Deviations from Source

**Feature tickets only in denominator.** v2.md §5.7 says "covered stories / total stories in epic x 100." This spec restricts the denominator to feature tickets only (excluding bugs). Rationale: bugs don't need test coverage — they ARE defects. Including them would inflate the denominator with tickets that will never have TEs, making coverage rates misleadingly low. This matches the denominator pattern used in F26 (sprint coverage) and F28 (per-developer coverage).

**No sync prompt on empty state.** v2.md §6.3 specifies a "Sync sprint to load QA data" prompt when Xray is enabled but not synced. The Epics page has no sprint selector — it aggregates cross-sprint — so a sync prompt is not actionable here. QA columns show "—" instead.

## Purpose

F14 gives Scrum Masters epic delivery progress — SP completion, velocity, projection. But it says nothing about quality. A fast-completing epic with 30% test coverage is a different risk profile than one at 90%. F31 adds the quality dimension to the Epics page by surfacing test coverage, pass rate, and bugs found per epic — derived bottom-up from each epic's stories and their linked Test Executions. Since Xray Test Plans don't link to epics (audit confirmed: all 5 are standalone with no issue links), this bottom-up derivation from stories → TEs is the only reliable path.

## Entities

No new entities. No settings extensions. F31 reads from existing data:

- **TestExecution** and **TestExecutionLink** (from F25) — coverage relationship between TEs and tickets
- **TestRun** (from F25) — pass/fail results per execution
- **Ticket** — epic key, issue type, assignee, sprint membership
- **Developer** — sub-team membership (for sub-team filter)

F31 reuses the QA health thresholds from F26 for RAG coloring on Coverage % and Pass Rate % cells. No new thresholds needed.

## User Flows

```
Flow 1: View Epic Test Health (Extension to F14 Landing)
1. User navigates to "Epics" in the sidebar
2. When Xray is enabled and at least one epic has QA data, a 4th summary card appears: "Average Test Coverage" showing the mean coverage % across visible epics (per active/completed toggle)
3. The epic table shows three additional columns after the existing columns: Coverage %, Pass Rate %, Bugs Found
4. Each epic row shows a dual progress bar: top bar = SP completion (existing), bottom bar = test coverage %
5. Coverage % and Pass Rate % cells show RAG coloring using the existing QA health thresholds from F26 settings
6. Bugs Found shows an integer count (no RAG coloring)
7. Epics with zero feature tickets (bug-only epics) show "—" in all QA columns
```

```
Flow 2: Expand Epic Detail with Test Status
1. User clicks the expand control on an epic row
2. The ticket table shows three additional columns after the existing columns: Test Status, Pass Rate, Bugs Found
3. Test Status shows a badge: Passed (green), Failed (red), In Progress (amber), No Tests (gray)
4. Pass Rate shows the percentage of PASS runs across the ticket's linked TEs
5. Bugs Found shows the count of bugs linked via Blocks from the ticket's TEs
6. Bug tickets in the expanded table do not show QA columns (they are not test targets)
```

```
Flow 3: Toggle Active/Completed with QA
1. User switches between Active and Completed toggle
2. QA columns appear on both views — completed epics retain their test coverage data
3. The "Average Test Coverage" summary card recalculates for the visible set
4. Completed epics with low coverage surface a quality gap: "we shipped this but only tested 40%"
```

```
Flow 4: Filter by Sub-Team with QA
1. User selects a sub-team from the sub-team filter
2. QA columns recalculate scoped to tickets assigned to developers in that sub-team
3. Coverage denominator restricts to feature tickets assigned to the sub-team's developers
4. The "Average Test Coverage" summary card recalculates for the filtered, toggled set
```

```
Flow 5: Empty States
1a. Xray disabled → QA columns not rendered, QA summary card not shown, dual progress bar shows SP bar only
1b. Xray enabled, no QA data synced → QA columns show "—", summary card shows "—", coverage progress bar not shown
1c. Xray enabled, epic has zero TEs on its feature tickets → Coverage 0%, Pass Rate 0%, Bugs Found 0
1d. Epic has only bug tickets → QA columns show "—" (no feature tickets to cover)
```

## API Surface

F31 extends the existing GET /api/analytics/epic-progress endpoint rather than creating a new one. When Xray is enabled, the response gains additional fields.

| Method | Route | Auth | Request body | Response | Status codes |
|--------|-------|------|-------------|----------|-------------|
| GET | /api/analytics/epic-progress | Authenticated | — | Extended EpicProgressResponse | 200 |

**Query parameters (unchanged from F14):**
- `subTeam` (string, optional) — filter to tickets assigned to developers in this sub-team.

**Response extensions (added to existing F14 response):**

Top level (new fields):
- `hasQaData` — boolean (false when Xray disabled or no epics have QA data)
- `averageTestCoverage` — decimal, nullable (mean coverage % across visible epics, null when no QA data or no feature tickets)

Per epic entry (new fields):
- `coverageRate` — decimal, nullable (coverage %, null when zero feature tickets in epic)
- `passRate` — decimal, nullable (pass rate %, null when zero feature tickets in epic)
- `bugsFound` — integer (count of unique bugs, 0 when none)
- `featureTicketCount` — integer (count of non-bug tickets in epic, used as coverage denominator)
- `coveredTicketCount` — integer (feature tickets with at least one non-cancelled linked TE)
- `coverageRag` — string, nullable (green/amber/red based on F26 thresholds, null when coverage unavailable)
- `passRateRag` — string, nullable (green/amber/red based on F26 thresholds, null when pass rate unavailable)

Per epic ticket entry (new fields):
- `testStatus` — string, nullable (Passed/Failed/InProgress/NoTests, null for bug tickets)
- `testPassRate` — decimal, nullable (pass rate % for this ticket's linked TEs, null for bug tickets or tickets with no executed runs)
- `testBugsFound` — integer, nullable (bugs linked from this ticket's TEs, null for bug tickets)
- `testRunSummary` — object, nullable ({ passed: int, failed: int, todo: int, executing: int, aborted: int }, null for bug tickets or tickets with no TEs)

**Error conditions:**
- 200 with hasQaData=false and null QA fields when Xray disabled — same endpoint, graceful degradation
- Existing 200 behavior for non-QA fields is unchanged

## Business Rules

### Metric Formulas

BR1. **Feature ticket scope.** The QA denominator for each epic is the set of non-bug tickets with that epic key. This matches F14's inclusive scope (all tickets with the epic key, including backlog-discovered ones) but excludes bug tickets (IssueType = "Bug"). Bug tickets are not test targets — they are defects.

BR2. **Coverage Rate** = feature tickets with at least one non-cancelled linked TE (where linkType = Tests) / total feature tickets in the epic x 100.

BR3. **Pass Rate** = test runs with PASS status / test runs with PASS or FAIL status, across all non-cancelled TEs linked to the epic's feature tickets x 100. TODO, EXECUTING, and ABORTED runs are excluded from both numerator and denominator. Same formula structure as F26 BR3 and F28 BR4, scoped per epic.

BR4. **Bugs Found** = count of unique bug tickets linked via Blocks links from non-cancelled TEs that test this epic's feature tickets. A bug linked from multiple TEs within the same epic counts once.

BR5. **Per-ticket test status** (in expanded detail):
   - **Passed** — ticket has linked TEs and all test runs across all TEs are PASS
   - **Failed** — ticket has linked TEs and at least one test run is FAIL
   - **In Progress** — ticket has linked TEs with TODO or EXECUTING runs but no FAIL runs
   - **No Tests** — ticket has no linked non-cancelled TEs
   - Bug tickets show no test status (null)

BR6. **Per-ticket pass rate** = PASS runs / (PASS + FAIL) runs across all non-cancelled TEs linked to this specific ticket. Same per-ticket formula as F25 ticket enrichment.

BR7. **Per-ticket bugs found** = unique bugs linked via Blocks from non-cancelled TEs linked to this specific ticket.

### Aggregation Rules

BR8. **Worst-result at ticket level.** If a ticket has multiple TEs and any TE has a FAIL run, the ticket's test status is Failed. Same rule as F26 BR5.

BR9. **Cancelled TEs excluded.** TEs with cancelled status (IsCancelled = true) are excluded from all epic-level computations — coverage, pass rate, bugs found, and per-ticket detail. Same rule as F26 BR7.

BR10. **TE linked to multiple tickets in the same epic.** Creates one TestExecutionLink per ticket. Each link counts independently — the TE contributes coverage to each linked ticket.

BR11. **TE linked to tickets across multiple epics.** The TE contributes to each epic independently. If TE X tests Story A (Epic Alpha) and Story B (Epic Beta), both epics gain coverage from that TE.

BR12. **Sub-tasks.** Sub-tasks with their own TE links use those links. Sub-tasks without own TEs inherit parent ticket's coverage status. Attribution follows the ticket's own epic key — if a sub-task has a different epic key than its parent, it counts toward its own epic.

### Division by Zero

BR13. Zero feature tickets in epic → Coverage Rate is null, Pass Rate is null, QA columns show "—".

BR14. Zero covered feature tickets → Pass Rate = 0% (no executed runs exist).

BR15. Zero executed test runs (PASS + FAIL) across the epic's TEs → Pass Rate = 0%.

### Summary Card

BR16. **Average Test Coverage** = arithmetic mean of coverage rates across epics visible in the current view (active or completed, after sub-team filter). Epics with null coverage (zero feature tickets) are excluded from the mean. When all visible epics have null coverage, the card shows "—".

BR17. **Summary card visibility.** The card appears only when Xray is enabled and at least one visible epic has a non-null coverage rate. When Xray is disabled, the card does not render (page shows 3 summary cards as in F14).

### Dual Progress Bar

BR18. **Coverage progress bar.** A second, thinner progress bar appears below the existing SP completion bar on each epic row. It fills to the epic's coverage rate %. The bar uses a distinct color from the SP bar (the QA accent color used in F26 metric cards). When coverage is null, the second bar does not render.

BR19. **SP bar unchanged.** The existing SP completion progress bar (including imputed SP segment) remains identical to F14's current implementation.

### RAG Coloring

BR20. **Reuses F26 thresholds.** Coverage % cells use the Coverage Rate green/amber thresholds from QA health settings (default: green >= 80%, amber >= 50%). Pass Rate % cells use the Pass Rate green/amber thresholds (default: green >= 90%, amber >= 70%). Values below amber are red. Same pattern as F28.

### Feature Flag Gate

BR21. **When Xray disabled:** QA columns are not rendered in the epic table, QA columns are not rendered in the expanded ticket detail, the dual progress bar shows only the SP bar, the QA summary card does not render, the API response includes hasQaData=false with null QA fields. The page looks and behaves exactly as F14 currently does.

BR22. **When Xray enabled but no QA data:** QA columns render with "—" values, summary card shows "—", coverage progress bar does not render.

### Sub-Team Filter

BR23. **Sub-team scopes QA metrics.** When a sub-team is selected, all QA computations restrict to feature tickets assigned to developers in that sub-team — coverage denominator, pass rate aggregation, bugs found count, and the expanded ticket detail. Same scoping behavior as F14's existing sub-team filter applied to QA data.

BR24. **Summary card recalculates.** The Average Test Coverage card uses coverage rates from the sub-team-scoped, toggle-scoped visible epics.

## Acceptance Criteria

### Epic Table — QA Columns

- [ ] Coverage %, Pass Rate %, Bugs Found columns appear on the epic table when Xray is enabled
- [ ] QA columns are hidden when Xray is disabled
- [ ] Coverage % cell shows RAG coloring using F26 Coverage Rate thresholds
- [ ] Pass Rate % cell shows RAG coloring using F26 Pass Rate thresholds
- [ ] Bugs Found shows integer count with no RAG coloring
- [ ] Bug-only epics show "—" in all QA columns
- [ ] QA columns appear on both Active and Completed views

### Dual Progress Bar

- [ ] A second thin progress bar appears below the existing SP completion bar on each epic row
- [ ] The coverage bar fills to the epic's coverage rate %
- [ ] The coverage bar uses a visually distinct color from the SP bar
- [ ] When coverage is null (zero feature tickets), the coverage bar does not render
- [ ] The existing SP completion bar remains unchanged

### Summary Card

- [ ] A 4th summary card "Average Test Coverage" appears when Xray is enabled and at least one epic has QA data
- [ ] Card shows the arithmetic mean of coverage rates across visible epics
- [ ] Card recalculates when the active/completed toggle changes
- [ ] Card recalculates when the sub-team filter changes
- [ ] Card is hidden when Xray is disabled
- [ ] Card shows "—" when all visible epics have null coverage

### Expanded Ticket Detail

- [ ] Test Status, Pass Rate, Bugs Found columns added to the ticket detail table when Xray is enabled
- [ ] Test Status shows a badge: Passed (green), Failed (red), In Progress (amber), No Tests (gray)
- [ ] Pass Rate shows percentage of PASS runs for the ticket's linked TEs
- [ ] Bugs Found shows count of bugs linked from the ticket's TEs
- [ ] Bug tickets do not show QA columns (null values)
- [ ] Ticket detail QA columns hidden when Xray is disabled

### Empty States

- [ ] Xray disabled: page looks and behaves exactly as F14 (no QA elements)
- [ ] Xray enabled, no QA data: QA columns show "—", summary card shows "—", no coverage bar
- [ ] Xray enabled, epic has zero TEs on feature tickets: Coverage 0%, Pass Rate 0%, Bugs Found 0

### API

- [ ] GET /api/analytics/epic-progress response includes hasQaData, averageTestCoverage, and per-epic QA fields when Xray is enabled
- [ ] Per-epic response includes coverageRate, passRate, bugsFound, featureTicketCount, coveredTicketCount, coverageRag, passRateRag
- [ ] Per-ticket response includes testStatus, testPassRate, testBugsFound, testRunSummary
- [ ] QA fields are null when Xray is disabled (hasQaData=false)
- [ ] Sub-team filter scopes all QA computations
- [ ] Bug tickets have null QA fields in the per-ticket response

### Cross-Cutting

- [ ] Cancelled TEs excluded from all computations
- [ ] Sub-team filter applies to all QA metrics (coverage, pass rate, bugs found, summary card)
- [ ] Feature tickets only in coverage denominator (bugs excluded)
- [ ] RAG coloring uses F26 QA health thresholds

## Out of Scope

- **Execution Rate per epic** — F26 has it at sprint level. Per-epic execution rate adds a column without a clear action. Coverage and Pass Rate are sufficient.
- **Deltas and sparklines** — F14's Epics page is cumulative with no sprint selector (F14 §BR18). Without a sprint to compare against, deltas and sparklines don't apply. Velocity and projection inherently reflect trends.
- **Column sorting on QA columns** — F14 deferred column sorting to v2 (F14 Out of Scope). F31 inherits that decision.
- **Epic-level health score** — sprint health score already has a Quality sub-score from F26. An epic-level health score is a separate concept not in v2 scope.
- **Test Set-based analytics** — Test Sets are context only (F25 domain model). No TestSet-scoped analytics in F31.
- **Weighted average for summary card** — F14's "Average Completion" card uses weighted average (by adjusted total SP). The QA summary card uses arithmetic mean for simplicity — epic size doesn't clearly correlate with coverage difficulty. Could be revisited if the team finds arithmetic mean misleading.
- **Per-story drill-down into test run details** — F31 shows per-story test status within the expanded epic. Drilling further into individual test runs (per TE, per run) is not in scope — users go to Xray for that level of detail.
- **Coverage trend per epic across sprints** — would require sprint-scoped epic coverage computation. The cumulative view is sufficient for v2.

## Open Questions

None — all resolved during discussion.
