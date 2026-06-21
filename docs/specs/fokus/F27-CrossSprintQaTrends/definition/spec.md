# Cross-Sprint QA Trends

**Traces to:** `docs/product/v2.md` §5.3
**Source:** Scratch
**Dependencies:** F25 (Xray Integration & Ticket Test Enrichment), F26 (Sprint Test Coverage — per-sprint QA metrics, health thresholds), F13 (Bug Ratio — per-sprint bug SP percentage for defect correlation)
**Status:** Done
**Plan:** `docs/specs/F27-CrossSprintQaTrends/delivery/plan.md`

---

## Deviations from Source

**Stacked panels instead of dual-axis chart.** v2.md §5.3 describes a single multi-line chart with coverage rate, pass rate, and execution rate. This spec splits the visualization into two stacked panels: one for percentage metrics (coverage, pass, execution rates) and one for count metrics (TE volume, bugs found). Rationale: dual-axis charts are an established anti-pattern in data visualization (per Stephen Few, Edward Tufte) because independently scalable axes create misleading visual correlations. Stacked panels with aligned X-axes provide the same comparison without the visual distortion.

**N+1 lag for defect correlation.** v2.md §5.3 says "Defect correlation — sprints with low coverage rate vs. bug ratio from v1 (F13)." This spec implements this as an N+1 lagged correlation: coverage rate in sprint N is compared against bug ratio in sprint N+1. Rationale: industry research shows low test coverage in one sprint leads to escaped defects manifesting as bug work in the following sprint — the "defect leakage" pattern. Same-sprint correlation shows observation; N+1 shows a leading indicator and is more actionable for sprint planning.

**Dedicated QA sidebar page.** v2.md §6.1 envisions a "QA" sidebar page for workload, throughput, and timeline. F29 (QA Workload) was specced as a Developers page tab and F30 (Test Execution Timeline) as a Sprints page section. This spec creates the dedicated QA sidebar page for F27's cross-sprint trends — the team-level "are we improving?" question deserves its own focused view rather than being embedded in a per-sprint or per-developer page.

**No summary cards.** The Dashboard already shows F26's current-sprint QA metric cards. The QA page is about trends over time, not current state. Summary cards would be redundant.

**Fixed-range sprint selector only.** v2.md §5.3 says "same sprint selector and sub-team filter as v1 (cross-cutting concerns C2, C3)." The v1 sprint selector supports both single-sprint and multi-sprint modes. This spec offers only fixed multi-sprint ranges (Last 3, Last 5, Last 10, All) — no single-sprint selection. Rationale: a trend page requires multiple data points; a single sprint is not a trend. The sub-team filter follows the v1 pattern unchanged.

## Purpose

F26 shows per-sprint QA health on the Dashboard (coverage rate, execution rate, pass rate) with 4-sprint sparklines. Scrum Masters need the longer view — "are we getting better at quality over time?" — and the causal link — "does low coverage lead to more bugs?" This feature provides a dedicated QA page with full cross-sprint trend charts and a defect correlation overlay that compares test coverage against bug ratio with an N+1 sprint lag, making the relationship between testing investment and defect leakage visible for retrospectives and stakeholder conversations. Neither Xray nor Zephyr Scale provides sprint-scoped aggregated QA trends natively — this fills a gap that currently requires external BI tools.

## Entities

No new entities. No settings extensions. F27 reads from existing data:

- **TestExecution** and **TestExecutionLink** (from F25) — coverage relationship between TEs and tickets
- **TestRun** (from F25) — pass/fail results per execution
- **Ticket** — sprint membership, issue type (for bug ratio computation)
- **Sprint** — sprint identity and date boundaries
- **SprintMembership** — sprint scope (active scope tickets per F23 transition-based model)

F27 reuses the QA health thresholds from F26 for RAG coloring on trend chart data points. No new thresholds.

## User Flows

```
Flow 1: View QA Trends
1. User clicks "QA" in the sidebar
2. System displays the QA Trends page with the sprint selector defaulting to "Last 10"
3. Top section: "Quality Trends" panel — a multi-line chart showing Coverage Rate (blue), Pass Rate (green), Execution Rate (gray) across the selected sprint range
4. X-axis: sprint names in chronological order. Y-axis: percentage (0–100%)
5. Hover on a data point shows the exact value for that metric and sprint
6. Below the quality trends chart: "Testing Volume" panel — a grouped bar chart showing TE Count and Bugs Found per sprint
7. X-axis aligned with the quality trends chart above. Y-axis: count
8. Hover shows exact values per sprint
```

```
Flow 2: View Defect Correlation
1. Below the testing volume panel, a "Defect Correlation" section displays
2. Two stacked panels with aligned sprint X-axis:
   a. Top panel: Coverage Rate line (blue) across sprints
   b. Bottom panel: Bug Ratio line (red, from F13) across sprints, offset by one sprint (N+1 lag)
3. Visual offset annotation connects coverage in sprint N to bug ratio in sprint N+1 (e.g., a subtle connecting line or arrow between the panels)
4. When 6 or more sprints exist in the intersection of QA data and bug ratio data, a Pearson r badge appears showing the correlation coefficient (e.g., "r = −0.72")
5. Badge tooltip explains: "Correlation between test coverage and next-sprint bug ratio. Negative values indicate higher coverage predicts fewer bugs."
6. When fewer than 6 intersecting sprints exist, the badge is hidden and a note reads: "Need 6+ sprints with both QA and bug data for correlation analysis"
```

```
Flow 3: Change Sprint Range
1. User changes the sprint selector to "Last 3", "Last 5", "Last 10", or "All"
2. All three sections (quality trends, testing volume, defect correlation) update to the selected range
3. The correlation badge recalculates; it may appear or disappear based on whether 6+ intersecting sprints exist in the new range
```

```
Flow 4: Filter by Sub-Team
1. User selects a sub-team from the toolbar filter
2. All metrics recalculate scoped to tickets assigned to developers in that sub-team
3. Coverage rate, pass rate, execution rate, TE volume, bugs found — all recomputed for the filtered population
4. Bug ratio in the correlation section also scopes to the sub-team
5. Pearson r recalculates for the filtered data
```

```
Flow 5: Empty States
5a. Xray disabled → "QA" sidebar entry hidden entirely
5b. Xray enabled, fewer than 2 sprints with QA data → page shows "Not enough data to show trends. Sync at least 2 sprints with QA data."
5c. Xray enabled, 2+ sprints with QA data but zero sprints with bug ratio data → Quality Trends and Testing Volume panels render normally; Defect Correlation section shows "No bug ratio data available for correlation"
5d. Xray enabled, sprint synced with Xray but zero TEs → that sprint shows as 0% coverage, 0% pass rate, 0 TE volume (real data point, not skipped)
5e. Sprint before Xray was enabled → excluded from all charts (no data, not zero)
```

## API Surface

| Method | Route | Auth | Request body | Response | Status codes |
|--------|-------|------|-------------|----------|-------------|
| GET | /api/analytics/qa-trends | Authenticated | — | QaTrendsResponse | 200 |

**Query parameters:**
- `last` (int, optional) — last N closed sprints with QA data. Default: 10.
- `subTeam` (string, optional) — filter by sub-team name.

No `sprintId` parameter — F27 is always multi-sprint. The sprint selector offers "Last 3", "Last 5", "Last 10", "All". "All" is expressed by omitting the `last` parameter.

**QaTrendsResponse:**

Top level:
- `hasQaData` — boolean (false when Xray disabled or fewer than 2 sprints with QA data)
- `sprints` — ordered list of sprint summaries (id, name, startDate, endDate) included in this response, filtered to only sprints with QA data

Quality trend data (one entry per sprint):
- `qualityTrends` — list of:
  - `sprintId` — integer
  - `sprintName` — string
  - `coverageRate` — decimal (0–100)
  - `passRate` — decimal (0–100)
  - `executionRate` — decimal (0–100)

Testing volume data (one entry per sprint):
- `testingVolume` — list of:
  - `sprintId` — integer
  - `sprintName` — string
  - `teCount` — integer (non-cancelled TEs in the sprint)
  - `bugsFound` — integer (unique bugs linked via Blocks from sprint TEs)

Defect correlation data:
- `defectCorrelation` — object, nullable (null when no intersecting sprints exist):
  - `dataPoints` — list of:
    - `sprintId` — integer (the sprint whose coverage is measured)
    - `sprintName` — string
    - `coverageRate` — decimal (coverage rate in this sprint)
    - `nextSprintBugRatio` — decimal, nullable (bug ratio in the following sprint; null for the most recent sprint)
    - `nextSprintName` — string, nullable
  - `pearsonR` — decimal, nullable (correlation coefficient; null when fewer than 6 data points with both values)
  - `dataPointCount` — integer (number of sprint pairs with both coverage and next-sprint bug ratio)

**Error conditions:**
- 200 with `hasQaData: false` and empty arrays when Xray is disabled — not an error
- 200 with `hasQaData: false` when fewer than 2 sprints have QA data
- `last` values less than 2 are treated as 2 (minimum for trend display)

## Business Rules

### Quality Trend Metrics

1. **Coverage Rate per sprint.** Same formula as F26 BR1: feature tickets with at least one non-cancelled linked TE (linkType = Tests) / total feature tickets in sprint active scope × 100. Recomputed per sprint in the range.

2. **Pass Rate per sprint.** Same formula as F26 BR3: PASS runs / (PASS + FAIL) runs across all non-cancelled TEs in the sprint × 100.

3. **Execution Rate per sprint.** Same formula as F26 BR2: covered tickets with at least one terminal-status (PASS or FAIL) run / covered tickets × 100.

4. **Cancelled TEs excluded.** Consistent with F26 BR7 — TEs marked as cancelled are excluded from all F27 computations.

5. **Sprint inclusion rule.** A sprint appears in the trend charts only when it has been synced with QA data (at least one non-cancelled TestExecutionLink exists for tickets in that sprint). Sprints before Xray was enabled are excluded — they have no QA data, which is different from having zero coverage.

6. **Zero-TE sprints.** A sprint that was synced with Xray but has zero non-cancelled TEs shows 0% coverage, 0% pass rate, 0% execution rate, 0 TE count — this is a valid data point (the team did no testing that sprint) and is plotted on the chart.

### Testing Volume Metrics

7. **TE Count per sprint.** Count of unique non-cancelled TestExecutions linked to tickets in the sprint via TestExecutionLink (linkType = Tests).

8. **Bugs Found per sprint.** Count of unique bug tickets (IssueType = "Bug") linked via Blocks links from non-cancelled TEs in the sprint. A bug linked from multiple TEs counts once. Same formula as F26 BR4.

### Defect Correlation

9. **N+1 lag model.** For each sprint N in the range, the correlation pairs sprint N's coverage rate with sprint N+1's bug ratio. The most recent sprint in the range has no N+1 data — its `nextSprintBugRatio` is null and it is excluded from the Pearson r computation.

10. **Bug ratio source.** Bug ratio reuses the existing F13 computation: effective bug SP as a percentage of effective completed SP in the sprint. "Effective SP" applies the configured default SP per bug fallback for unestimated bug tickets, and "completed" uses transition-based attribution (tickets that reached the completion boundary during the sprint). The exact formula is: effectiveBugSp / effectiveCompletedSp × 100 (where effectiveCompletedSp = effectiveBugSp + effectiveNonBugSp for completed tickets; 0% when effectiveCompletedSp is zero). This is computed from Jira data (not Xray), so it exists for all synced sprints regardless of Xray enablement. The implementation should reuse the existing bug ratio service to guarantee identical numbers between this correlation chart and the Developers page Bug Ratio tab.

11. **Intersection requirement.** A data point in the correlation section requires both: (a) sprint N has QA data (coverage rate exists), and (b) sprint N+1 has been synced (bug ratio exists). Sprints where either side is missing are excluded from the correlation chart and the Pearson r computation.

12. **Pearson r computation.** Computed from all data points where both coverage rate (sprint N) and bug ratio (sprint N+1) are non-null. The coefficient ranges from −1 to +1. A negative value indicates higher coverage correlates with lower next-sprint bug ratio (the expected healthy pattern). Only computed when 6 or more complete data points exist — below this threshold, the sample is too small for meaningful correlation.

13. **Pearson r display.** Rounded to two decimal places (e.g., "r = −0.72").

### Sub-Team Filter

14. **Sub-team scopes all computations.** When a sub-team is selected: coverage rate, pass rate, and execution rate are recomputed using only tickets assigned to developers in that sub-team. TE count and bugs found scope to TEs linked to those tickets. Bug ratio in the correlation section also scopes to the sub-team's tickets. Same pattern as F26 BR23.

### Sprint Selector

15. **Default range: last 10.** The page loads with `last=10`. The sprint selector offers "Last 3", "Last 5", "Last 10", and "All".

16. **"All" returns all QA-synced sprints.** Only sprints that have QA data (per BR5), not all sprints in the system.

### Division by Zero

17. Zero feature tickets in a sprint → Coverage Rate = 0% for that data point.

18. Zero covered tickets in a sprint → Execution Rate = 0%.

19. Zero terminal runs in a sprint → Pass Rate = 0%.

20. Zero completed SP in a sprint → Bug Ratio = 0% for correlation purposes.

21. Fewer than 2 QA-synced sprints → `hasQaData: false`, page shows empty state.

22. Fewer than 6 complete data points → Pearson r is null, badge hidden.

### UI & Display

23. **Quality Trends chart.** Multi-line chart with three series: Coverage Rate (blue), Pass Rate (green), Execution Rate (gray). Y-axis: 0–100%. X-axis: sprint names in chronological order. Smooth curves. Tooltip on hover shows all three values for the hovered sprint.

24. **Testing Volume chart.** Grouped bar chart with two series: TE Count and Bugs Found. Each sprint has two side-by-side bars. Y-axis: count (auto-scaled). X-axis aligned with the quality trends chart above.

25. **Chart alignment.** Both the quality trends and testing volume charts share the same sprint X-axis. Visual alignment allows the user to correlate "coverage dropped in sprint 26" with "but TE volume also dropped in sprint 26." Charts are stacked vertically within card wrappers.

26. **Defect Correlation panels.** Two stacked panels with aligned sprint X-axis. Top panel: Coverage Rate line (blue). Bottom panel: Bug Ratio line (red), with sprint labels shifted by one position to visualize the N+1 lag. A subtle visual connector (dashed line or arrow annotation) links each coverage point to its corresponding next-sprint bug ratio point.

27. **Pearson r badge.** Small badge (pill shape) positioned in the correlation section header. Background color: green when r < −0.3 (coverage inversely correlated with bugs — healthy), amber when −0.3 ≤ r ≤ 0.3 (weak or no correlation), red when r > 0.3 (coverage positively correlated with bugs — unexpected). Tooltip explains the interpretation.

28. **RAG coloring on data points.** Individual data points on the quality trends chart use RAG coloring based on F26's QA health thresholds. Coverage Rate points: green ≥ 80%, amber ≥ 50%, red < 50%. Pass Rate points: green ≥ 90%, amber ≥ 70%, red < 70%. Execution Rate points: same thresholds as Coverage Rate. The line color is the series color; the data point marker is RAG-colored.

29. **Sidebar entry.** "QA" appears in the sidebar when Xray is enabled, gated behind the Xray feature flag.

30. **Feature flag gate.** When Xray is disabled: "QA" sidebar entry is hidden, endpoint returns `hasQaData: false` with empty arrays.

## Acceptance Criteria

### QA Sidebar Page

- [ ] "QA" sidebar entry appears when Xray is enabled
- [ ] "QA" sidebar entry hidden when Xray is disabled
- [ ] Page loads with sprint selector defaulting to "Last 10"
- [ ] Sprint selector offers "Last 3", "Last 5", "Last 10", "All"
- [ ] Sub-team filter available in the page toolbar

### Quality Trends Chart

- [ ] Multi-line chart displays Coverage Rate (blue), Pass Rate (green), Execution Rate (gray)
- [ ] Y-axis shows percentage (0–100%)
- [ ] X-axis shows sprint names in chronological order
- [ ] Hover tooltip shows all three metric values for the hovered sprint
- [ ] Individual data points use RAG coloring based on F26 QA health thresholds
- [ ] Chart updates when sprint range or sub-team filter changes

### Testing Volume Chart

- [ ] Grouped bar chart displays TE Count and Bugs Found per sprint
- [ ] X-axis aligned with the quality trends chart
- [ ] Hover tooltip shows TE count and bugs found for the hovered sprint
- [ ] Chart updates when sprint range or sub-team filter changes

### Defect Correlation

- [ ] Two stacked panels show Coverage Rate (top) vs Bug Ratio (bottom) with aligned sprint X-axis
- [ ] Bug Ratio panel is offset by one sprint to visualize N+1 lag
- [ ] Visual connector links coverage in sprint N to bug ratio in sprint N+1
- [ ] Pearson r badge appears when 6+ complete data points exist
- [ ] Pearson r badge hidden when fewer than 6 data points
- [ ] Badge tooltip explains the correlation interpretation
- [ ] Badge color: green for r < −0.3, amber for −0.3 to 0.3, red for r > 0.3
- [ ] Note shown when badge is hidden: "Need 6+ sprints with both QA and bug data for correlation analysis"
- [ ] Correlation section shows "No bug ratio data available for correlation" when no bug ratio data exists

### Empty States

- [ ] Xray disabled: sidebar entry hidden, endpoint returns hasQaData=false
- [ ] Fewer than 2 QA-synced sprints: page shows "Not enough data to show trends" prompt
- [ ] Sprint synced with zero TEs: shows as 0% on charts (not excluded)
- [ ] Sprint before Xray was enabled: excluded from charts entirely
- [ ] No intersecting sprints for correlation: correlation section shows fallback message

### API

- [ ] GET /api/analytics/qa-trends returns 200 with full trend data
- [ ] Endpoint accepts `last` and `subTeam` query parameters
- [ ] Default is `last=10` when no parameter provided
- [ ] `last` values less than 2 are treated as 2
- [ ] Returns 200 with hasQaData=false when Xray disabled
- [ ] Correlation includes Pearson r when 6+ complete data points exist
- [ ] Most recent sprint's nextSprintBugRatio is null

### Cross-Cutting

- [ ] Sub-team filter scopes all computations including bug ratio in correlation
- [ ] Cancelled TEs excluded from all computations
- [ ] Only sprints with QA data are included in charts (pre-Xray sprints excluded)

## Out of Scope

- **Same-sprint correlation toggle** — only N+1 lag is shown. Same-sprint correlation is less actionable and adds toggle complexity. Can be added later if user feedback demands it.
- **Scatter plot view** — with typical team history (10–20 sprints), there aren't enough data points for a meaningful scatter. Time-series panels are more readable.
- **Configurable lag period** — N+1 is the only supported lag. N+2 or custom offsets are deferred — industry consensus is that N+1 captures the primary defect leakage signal.
- **Per-developer trend breakdown** — F28 handles per-developer quality trends on the Developers page. The QA page is team-level only.
- **QA Workload on this page** — F29 QA Workload stays on the Developers page as specced. The QA page is trends-only.
- **Summary cards** — the Dashboard already shows F26's current-sprint QA metric cards. Duplicating them here adds no value.
- **Trend-line regression or forecast** — linear regression projecting future coverage/pass rate is deferred. The historical trend is sufficient for retro conversations.
- **Export/download** — chart export (PNG, CSV) is a cross-cutting concern, not F27-specific.
- **Test Set-based metrics** — TestSets are context only (F25 domain model). No TestSet-scoped analytics.

## Open Questions

None — all resolved during discussion.
