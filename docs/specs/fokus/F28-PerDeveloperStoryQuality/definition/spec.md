# Per-Developer Story Quality

**Traces to:** `docs/product/v2.md` §5.4
**Source:** Scratch
**Dependencies:** F25 (Xray Integration & Ticket Test Enrichment), F26 (Sprint Test Coverage — QA metric patterns, health thresholds)
**Status:** Done
**Plan:** `docs/specs/F28-PerDeveloperStoryQuality/delivery/plan.md`

---

## Deviations from Source

**Median instead of mean for warning flag.** v2.md §5.4 says "developer with coverage rate below team average for 2+ consecutive sprints." This spec uses **team median** instead. Rationale: in small teams (6–10 developers), a single outlier with 100% coverage inflates the mean, causing more developers to be flagged than warranted. The median represents the typical developer and is more robust to outlier distortion. This was discussed during spec shaping and confirmed by external research (LinearB recommends median or P75 over mean for team comparisons).

## Purpose

Fokus v2 adds sprint-level QA metrics (F26) but does not break them down per developer. Scrum Masters need to know whose stories are well-tested and whose are not — not for blame, but for the same capacity and coverage conversations that the Throughput and Bug Ratio tabs already enable. A developer with consistently low test coverage may have stories that are hard to test, under-specified acceptance criteria, or simply lower QA priority. This feature surfaces that signal so the team can act on it in retro.

## Entities

No new entities. F28 reads from existing data:

- **Test Execution** and **Test Execution Link** (from F25) — coverage relationship between TEs and tickets
- **Test Run** (from F25) — pass/fail results per execution
- **Ticket** — assignee attribution, sprint membership, story points
- **Developer** — identity, sub-team, active status
- **Sprint Membership** — sprint scope (active scope tickets per the transition-based model from F23)

No settings extensions. F28 reuses the QA health thresholds from F26 for RAG coloring on Coverage % and Pass Rate % cells.

## User Flows

```
Flow 1: View Per-Developer Quality for a Sprint
1. User navigates to the Developers page
2. User selects the "Quality" tab (4th tab, after Leaderboard). URL updates to ?tab=quality.
3. The page loads with the most recent closed sprint selected
4. A table displays one row per active developer with: name, sub-team, stories, covered, coverage %, pass rate %, untested, bugs found
5. Coverage % and Pass Rate % cells show RAG coloring using the existing QA health thresholds from F26 settings
6. Coverage % and Pass Rate % show delta indicators (arrow + signed number) comparing to the prior sprint, colored green for improvement, red for regression (higher is better for both)
7. Bugs Found shows a delta indicator with neutral polarity (no color — more bugs found is neither inherently good nor bad)
8. Coverage % and Pass Rate % show 4-sprint sparklines
9. Developers with zero stories in the sprint appear with all metrics at zero — not hidden
10. Developers whose coverage % has been below the team median for 2 or more consecutive sprints (ending at the selected sprint) show an amber warning icon on their row
11. Hovering the warning icon shows a tooltip: "Coverage below team median for N consecutive sprints"
```

```
Flow 2: View Quality Trend Across Sprints
1. User changes the sprint selector to "Last 3", "Last 5", or "All"
2. The table updates to show averaged values across the selected range — no delta indicators, no sparklines, no warning flags
3. A multi-line chart appears below the table showing Coverage % per developer across the selected sprints
4. Each developer is a separate line on the chart
5. Hovering a data point shows the exact coverage % for that developer and sprint
```

```
Flow 3: Filter by Sub-Team
1. User selects a sub-team from the sub-team filter
2. The table filters to show only developers in that sub-team
3. The chart updates to show only those developers' trend lines
4. The team median for the warning flag is recalculated using only the filtered sub-team
```

```
Flow 4: Empty States
1a. Xray disabled → "Quality" tab not rendered in the tab bar
1b. Xray enabled, sprint not synced with QA data → Tab shows "Sync sprint to load QA data" prompt
1c. Xray enabled, synced, developer has zero stories → Row shows 0% coverage, 0% pass rate, 0 untested, 0 bugs found
```

## API Surface

| Method | Route | Auth | Request body | Response | Status codes |
|--------|-------|------|-------------|----------|-------------|
| GET | /api/analytics/developer-quality | Authenticated | — | DeveloperQualityResponse | 200, 400 |

**Query parameters:**
- `sprintId` (int, optional) — single sprint. Mutually exclusive with `last`.
- `last` (int, optional) — last N closed sprints. Mutually exclusive with `sprintId`.
- `subTeam` (string, optional) — filter developers by sub-team name.

Default behavior: when neither `sprintId` nor `last` is provided, returns data for all closed sprints that have QA data. The frontend pre-selects the most recent closed sprint on page load.

**Note on sprint list:** In multi-sprint mode, the sprint list may differ from other Developers tabs (Throughput, Bug Ratio) because it is filtered to sprints with QA data. Xray may have been enabled after several sprints were already synced. The `sprints` response field communicates which sprints are included so the frontend can render accordingly.

**Response shape:**

Top level:
- `hasQaData` — boolean (false when Xray disabled or no sprints have QA data)
- `sprints` — ordered list of sprint summaries (id, name, start date, end date) included in this response. In single-sprint mode, always contains the requested sprint regardless of QA data (hasQaData indicates status). In multi-sprint mode, filtered to only sprints that have QA data.
- `developers` — list of developer quality entries

Per developer entry:
- Developer identity: display name, sub-team, avatar URL, account ID
- Per-sprint breakdown (one entry per sprint in the range):
  - Sprint ID
  - Stories: total feature stories assigned to this developer in sprint active scope
  - Covered: stories with at least one non-cancelled linked TE
  - Coverage %: covered / stories x 100
  - Pass Rate %: PASS runs / (PASS + FAIL) runs across all TEs on this developer's stories
  - Untested: stories with zero linked TEs
  - Bugs Found: unique bug tickets linked via Blocks from TEs on this developer's stories
- Delta values (only present when a single sprint is selected):
  - Coverage % delta and direction (higher is better)
  - Pass Rate % delta and direction (higher is better)
  - Bugs Found delta and direction (neutral — no polarity coloring)
  - Stories, Covered, and Untested do not have deltas — they are derived from the same underlying data, so triple-delta on correlated columns adds noise without insight
- Sparkline points (only present when a single sprint is selected):
  - Coverage % sparkline: up to 4 trailing sprints with QA data
  - Pass Rate % sparkline: up to 4 trailing sprints with QA data
- Warning flag (only present when a single sprint is selected):
  - `belowMedianStreak`: integer, number of consecutive sprints (ending at the selected sprint) where this developer's coverage % was below the team median. Null when streak is 0 or 1.

**Error conditions:**
- 400: both `sprintId` and `last` provided simultaneously
- 400: `sprintId` does not match any synced sprint
- 400: `last` is less than 1
- 200 with `hasQaData: false` and empty developers list when Xray is disabled — not an error
- 200 with empty results when no closed sprints exist

## Business Rules

### Metric Formulas

1. **Stories** = feature tickets assigned to this developer in the sprint's active scope. Uses the same ticket set as delivery metrics — tickets that transitioned to the cycle time start boundary or beyond, not removed, not bugs, not excluded-from-scope statuses. Attribution is by assignee at sync time.

2. **Covered** = stories (per rule 1) that have at least one non-cancelled linked TE where the link type is Tests.

3. **Coverage %** = Covered / Stories x 100.

4. **Pass Rate %** = test runs with PASS status / test runs with PASS or FAIL status, across all non-cancelled TEs linked to this developer's stories. TODO, EXECUTING, and ABORTED runs are excluded from both numerator and denominator. Same formula structure as F26 BR3, but scoped to this developer's stories rather than all sprint TEs.

5. **Untested** = Stories - Covered. The count of this developer's stories with zero linked TEs.

6. **Bugs Found** = count of unique bug tickets (IssueType = "Bug") linked via Blocks links from non-cancelled TEs that test this developer's stories. A bug linked from multiple TEs on the same developer's stories counts once per developer.

### Aggregation Rules

7. **Worst-result at ticket level.** If a developer's story has multiple TEs and any TE has a FAIL run, the story's status is FAIL. Both pass and fail counts are tracked. Same rule as F26.

8. **Cancelled TEs excluded.** TEs with cancelled status are excluded from all per-developer computations — coverage, pass rate, bugs found. Same rule as F26.

9. **TE linked to multiple developers' stories.** A TE that tests stories assigned to different developers contributes coverage to each developer independently. The TE's test runs count in each developer's pass rate.

10. **Sub-tasks.** Sub-tasks with their own TE links use those links. Sub-tasks without own TEs inherit parent ticket's coverage status for aggregation. Same structural rule as F26, with per-developer scoping: a sub-task is attributed to its own assignee, not the parent's assignee. If sub-task X (assigned to Dev A) has no TEs but its parent (assigned to Dev B) has TEs, the inherited coverage counts for Dev A's row — Dev A's story is considered covered.

### Division by Zero

11. Zero stories for a developer in the sprint → Coverage % = 0%, Pass Rate % = 0%.

12. Zero covered stories for a developer → Pass Rate % = 0%.

13. Zero executed test runs (PASS + FAIL) for a developer's stories → Pass Rate % = 0%.

### Warning Flag

14. **Team median computation.** The team median coverage % is computed from all active developers who have at least 1 story in the sprint. Developers with zero stories are excluded from the median calculation (they have no meaningful coverage signal).

15. **Consecutive sprint streak.** The warning flag activates when a developer's coverage % has been strictly below the team median for 2 or more consecutive closed sprints, ending at the selected sprint. A sprint where the developer has zero stories breaks the streak (excluded from comparison). A sprint with no QA data also breaks the streak.

16. **Sub-team scoping.** When a sub-team filter is active, the team median is recalculated using only developers in that sub-team. The streak is also evaluated against the sub-team median.

17. **Warning flag is single-sprint only.** The flag is computed and displayed only in single-sprint view. In multi-sprint view, no flags are shown.

### RAG Coloring

18. **Reuses F26 thresholds.** Coverage % cells use the Coverage Rate green/amber thresholds from QA health settings (default: green >= 80%, amber >= 50%). Pass Rate % cells use the Pass Rate green/amber thresholds (default: green >= 90%, amber >= 70%). Values below amber are red. This is per-cell coloring, not a flag.

### UI & Display

19. **Tab visibility.** The Quality tab is hidden entirely when Xray is disabled. When Xray is enabled but the selected sprint has no QA data, the tab shows an empty state prompt.

20. **Delta calculation.** Same pattern as all existing tabs. Current sprint value minus the immediately prior closed sprint's value. Direction: up if delta > 0, down if delta < 0, flat if delta = 0. Coverage % and Pass Rate % are higher-is-better (green arrow up, red arrow down). Bugs Found uses neutral polarity (no color — more bugs found is neither inherently good nor bad, same as SP Assigned in Throughput). Delta is null when the immediately prior closed sprint has no QA data — the system does not skip backward to find an older sprint with data.

21. **Sparkline.** Trailing 4-sprint window ending at the selected sprint. Only includes sprints that have QA data. Fewer points when fewer than 4 qualifying sprints exist. Same pattern as F26.

22. **Multi-sprint averaging.** When viewing multiple sprints, each metric is averaged across the selected range. The server returns per-sprint breakdowns (same as Throughput); the frontend computes averages from the breakdowns. Coverage % average is the mean of per-sprint coverage percentages, not a re-computation from totals. Sprints where a developer has zero stories are excluded from their average (a 0-story sprint has no meaningful coverage signal — including it as 0% would misleadingly drag down the average). The frontend detects zero-story sprints via the Stories field in each breakdown. Same principle as Throughput excluding 0% capacity sprints from rolling average.

23. **Sorting.** Table is sortable by clicking column headers. Default sort: Coverage % ascending (lowest coverage first — surfaces the developers most in need of attention).

24. **Only active developers.** Developers marked as inactive are excluded from all results — table, chart, and median computation.

25. **Sub-team filter applies to developers, not tickets.** Filtering by sub-team shows developers in that sub-team with all their stories — same pattern as Throughput tab.

## Acceptance Criteria

### Quality Tab — Table

- [ ] "Quality" tab appears as the 4th tab on the Developers page when Xray is enabled
- [ ] "Quality" tab is hidden when Xray is disabled
- [ ] Table displays one row per active developer: name, sub-team, stories, covered, coverage %, pass rate %, untested, bugs found
- [ ] Coverage % cell has RAG coloring using F26 Coverage Rate thresholds
- [ ] Pass Rate % cell has RAG coloring using F26 Pass Rate thresholds
- [ ] Single-sprint view shows delta indicators on Coverage %, Pass Rate %, and Bugs Found vs prior sprint
- [ ] Coverage % and Pass Rate % deltas are colored green for improvement, red for regression (higher is better)
- [ ] Bugs Found delta uses neutral polarity (no color)
- [ ] Stories, Covered, and Untested columns do not show deltas
- [ ] Single-sprint view shows 4-sprint sparklines on Coverage % and Pass Rate %
- [ ] Developers with zero stories appear with all-zero metrics, not hidden
- [ ] Table is sortable by column headers
- [ ] Default sort is Coverage % ascending

### Warning Flag

- [ ] Amber warning icon appears on developers whose coverage % is below team median for 2+ consecutive sprints
- [ ] Warning icon tooltip shows "Coverage below team median for N consecutive sprints"
- [ ] Developers with zero stories in a sprint are excluded from the team median
- [ ] Sub-team filter recalculates median using only filtered developers
- [ ] Warning flag only appears in single-sprint view
- [ ] A sprint with no QA data breaks the consecutive streak

### Multi-Sprint View

- [ ] Multi-sprint view shows averaged values across the selected range, excluding sprints where a developer has zero stories
- [ ] No delta indicators, sparklines, or warning flags in multi-sprint view
- [ ] Multi-line chart shows Coverage % per developer across the selected sprints
- [ ] Each developer is a separate line on the chart
- [ ] Chart hover shows exact coverage % for that developer and sprint

### Empty States

- [ ] Xray disabled: Quality tab not rendered
- [ ] Xray enabled, sprint not synced: tab shows "Sync sprint to load QA data" prompt
- [ ] Xray enabled, synced, developer has zero stories: row shows 0% values

### API

- [ ] GET /api/analytics/developer-quality returns 200 with per-developer quality data and hasQaData indicator
- [ ] Endpoint accepts ?sprintId, ?last, and ?subTeam query parameters
- [ ] 400 returned when both sprintId and last are provided
- [ ] 400 returned when sprintId does not match a synced sprint
- [ ] 400 returned when last is less than 1
- [ ] 200 with hasQaData=false when Xray is disabled
- [ ] Response includes belowMedianStreak for single-sprint requests
- [ ] Response includes sparkline points for single-sprint requests
- [ ] Response includes delta values (Coverage %, Pass Rate %, Bugs Found) for single-sprint requests

### Cross-Cutting

- [ ] Sub-team filter works on all quality metrics
- [ ] Cancelled TEs excluded from all computations
- [ ] Delta is null when no prior sprint has QA data
- [ ] Sparkline shows fewer points when fewer than 4 sprints have QA data

## Out of Scope

- **QA workload per person** — deferred to F29 (QA Workload & Throughput). F28 shows quality of a developer's stories, not who executed the tests.
- **Execution rate per developer** — v2 §5.4 does not include it, and F26 already covers it at sprint level. Per-developer execution rate adds complexity without clear insight.
- **Expandable row with per-story detail** — the F26 untested/failing ticket lists already show story-level detail at sprint level. Adding per-developer drill-down is a future enhancement.
- **Configurable warning threshold** — the below-median flag uses a fixed 2-sprint consecutive window. Making the window configurable adds settings complexity for minimal benefit. If the team finds 2 sprints too sensitive or too lenient, this can be revisited.
- **Pass Rate trend chart** — multi-sprint chart shows Coverage % only. Pass Rate trend could be added later as a toggle. Coverage is the more actionable metric (you can push for more testing).

## Open Questions

None — all resolved during discussion.
