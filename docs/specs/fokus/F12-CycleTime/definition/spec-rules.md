# F12-CycleTime — Spec Rules (mine-from-spec)

Spec-stamp: docs/specs/fokus/F12-CycleTime/definition/spec.md @ sha256:9bab0e18211e (2026-07-03)

Product of the `mine-verify-cover` spec arm (`mine-from-spec` mode, Mine→Verify depth):
3 clean-room miners read ONLY the stamped spec above; a fresh skeptic consolidated their
outputs with agreement counts and verified every rule against the spec text alone. This
documents what the SPEC commits to — not what the code does. No production source, tests,
or code-arm KB (`docs/kb/cycle-time.md`) were read by any stage.

- consolidated rules: 78 — verified: 75 / ambiguous: 3
- agreement distribution: 3/3 × 55, 2/3 × 11, 1/3 × 12

## Consolidated rules

### SR-1: cycle-start-stage-default
- statement: The cycle time start stage setting defaults to the second configured workflow stage (the first non-backlog stage) when not explicitly configured.
- boundary: none
- citation: "Cycle time start stage — which workflow stage begins the cycle time measurement. Defaults to the second configured workflow stage (first non-backlog stage)."
- agreement: 3/3
- verdict: verified

### SR-2: cycle-start-stage-must-reference-configured-stage
- statement: The configured cycle time start stage must reference a stage present in the configured workflow stages list.
- boundary: none
- citation: "Must reference a stage in the configured workflow stages list."
- agreement: 3/3
- verdict: verified

### SR-3: cycle-end-stage-default
- statement: The cycle time end stage setting defaults to the first configured done status when not explicitly configured.
- boundary: none
- citation: "Cycle time end stage — which workflow stage ends the cycle time measurement. Defaults to the first configured done status."
- agreement: 3/3
- verdict: verified

### SR-4: cycle-end-stage-must-reference-stage-or-done-status
- statement: The configured cycle time end stage must reference either a stage in the configured workflow stages list or a done status.
- boundary: none
- citation: "Must reference a stage in the configured workflow stages list or a done status."
- agreement: 3/3
- verdict: verified

### SR-5: sprint-selector-defaults-most-recent-closed
- statement: On page load, the Cycle Time sprint selector defaults to the most recent closed sprint.
- boundary: none
- citation: "The sprint selector defaults to the most recent closed sprint"
- agreement: 2/3
- verdict: verified

### SR-6: scatter-plot-reference-lines-p50-p85
- statement: The single-sprint scatter plot displays horizontal reference lines at P50 (dashed) and P85 (solid).
- boundary: none
- citation: "Horizontal reference lines at P50 (dashed) and P85 (solid)"
- agreement: 2/3
- verdict: verified

### SR-7: scatter-plot-hover-content
- statement: Hovering a scatter plot dot shows the ticket key, summary, total cycle time, and per-stage breakdown.
- boundary: none
- citation: "Hovering a dot shows: ticket key, summary, total cycle time, and per-stage breakdown"
- agreement: 1/3
- verdict: verified

### SR-8: rework-badge-display
- statement: Tickets with rework are visually flagged with a small rework badge (e.g., "2 re-entries") on the scatter plot and in ticket tables.
- boundary: none
- citation: "Tickets with rework show a small rework badge (e.g., \"2 re-entries\")."
- agreement: 2/3
- verdict: verified

### SR-9: stage-funnel-bottleneck-widest-segment
- statement: The bottleneck stage is the widest segment of the stage funnel and must be visually identifiable as such.
- boundary: none
- citation: "The widest segment is the bottleneck — visually obvious"
- agreement: 1/3
- verdict: ambiguous
- reason: The only commitment is presentational ("visually obvious" / AC "visually identifiable") with no concrete affordance defined; the testable content (segment width proportional to average time) is implied, never stated.

### SR-10: developer-breakdown-alphabetical-sort
- statement: The per-developer breakdown table is sorted alphabetically by developer name, not ranked by performance.
- boundary: none
- citation: "Sorted alphabetically — not ranked"
- agreement: 3/3
- verdict: verified

### SR-11: outlier-table-sorted-descending
- statement: The outlier table lists tickets exceeding 2x the sprint median, sorted by cycle time descending.
- boundary: none
- citation: "An outlier table lists tickets exceeding 2x the sprint median, sorted by cycle time descending."
- agreement: 3/3
- verdict: verified

### SR-12: p85-trend-one-point-per-sprint
- statement: The multi-sprint P85 trend line chart shows one point per sprint, plotting P85 cycle time over time.
- boundary: none
- citation: "A P85 trend line chart appears: one point per sprint, showing P85 cycle time over time."
- agreement: 2/3
- verdict: verified

### SR-13: sprint-summary-row-navigation
- statement: Clicking a row in the per-sprint summary table navigates to the single-sprint view for that sprint.
- boundary: none
- citation: "Clicking a row navigates to the single-sprint view for that sprint"
- agreement: 1/3
- verdict: verified

### SR-14: subteam-filter-scopes-everything
- statement: When a sub-team is selected, all computations — metric cards, scatter plot, stage funnel, breakdowns, outlier detection — restrict to tickets assigned to developers in that sub-team, and percentile lines recalculate for the filtered set.
- boundary: none
- citation: "When a sub-team is selected, all computations — metric cards, scatter plot, stage funnel, breakdowns, outlier detection — restrict to tickets assigned to developers in that sub-team. Percentile lines recalculate for the filtered set."
- agreement: 3/3
- verdict: verified

### SR-15: subteam-all-removes-filter
- statement: Selecting "All" in the sub-team filter removes the sub-team scoping entirely.
- boundary: none
- citation: "Selecting \"All\" removes the filter"
- agreement: 2/3
- verdict: verified

### SR-16: sprint-range-selection-view-switch
- statement: Selecting a range (Last 3/5/All) shows the multi-sprint trend view; selecting a single sprint shows the single-sprint detail view.
- boundary: none
- citation: "Selecting a range shows the multi-sprint trend view (Flow 2)" / "Selecting a single sprint shows the single-sprint detail view (Flow 1)"
- agreement: 2/3
- verdict: verified

### SR-17: url-reflects-sprint-selection
- statement: The URL updates to reflect the current sprint selection and is linkable.
- boundary: none
- citation: "The URL updates to reflect the selection" / "URL updates to reflect sprint selection and is linkable"
- agreement: 2/3
- verdict: verified

### SR-18: cycle-starts-dropdown-population
- statement: The "Cycle starts at" settings dropdown lists all configured workflow stages and defaults to the second stage (first non-backlog stage).
- boundary: none
- citation: "\"Cycle starts at\" — lists all configured workflow stages. Defaults to the second stage (first non-backlog stage)"
- agreement: 1/3
- verdict: verified

### SR-19: cycle-ends-dropdown-population
- statement: The "Cycle ends at" settings dropdown lists all configured workflow stages plus done statuses and defaults to the first done status.
- boundary: none
- citation: "\"Cycle ends at\" — lists all configured workflow stages plus done statuses. Defaults to the first done status"
- agreement: 1/3
- verdict: verified

### SR-20: boundaries-saved-via-dedicated-endpoint
- statement: Cycle time boundaries are saved via a dedicated endpoint (returning 200 on success), not the main settings save, to avoid full-replacement conflicts.
- boundary: none
- citation: "This is a dedicated endpoint following the same pattern as excluded-from-scope statuses — it does not go through the main settings save, avoiding full-replacement conflicts." / "200: Boundaries saved."
- agreement: 3/3
- verdict: verified

### SR-21: boundaries-recalculate-on-next-visit
- statement: After boundaries are saved, the Cycle Time page recalculates using the new boundaries on the next visit.
- boundary: none
- citation: "The Cycle Time page recalculates using the new boundaries on next visit"
- agreement: 2/3
- verdict: verified

### SR-22: percentile-toggle-options
- statement: The percentile toggle on the single-sprint scatter plot offers four options: P50, P75, P85, P90.
- boundary: none
- citation: "On the single-sprint scatter plot, a toggle control offers: P50, P75, P85, P90"
- agreement: 2/3
- verdict: verified

### SR-23: percentile-toggle-default-p85
- statement: The default percentile toggle selection is P85.
- boundary: none
- citation: "The default is P85"
- agreement: 3/3
- verdict: verified

### SR-24: percentile-toggle-updates-reference-line-and-card
- statement: Switching the percentile toggle changes the highlighted reference line on the scatter plot and updates the P85 metric card to show the selected percentile instead.
- boundary: none
- citation: "Switching the toggle changes the highlighted reference line on the scatter plot and updates the P85 metric card to show the selected percentile instead"
- agreement: 2/3
- verdict: verified

### SR-25: p50-line-always-visible
- statement: The P50 reference line remains visible (dashed) regardless of the percentile toggle selection.
- boundary: none
- citation: "P50 line always remains visible (dashed) regardless of toggle selection"
- agreement: 3/3
- verdict: verified

### SR-26: cycle-time-endpoints-no-auth
- statement: All three cycle time endpoints (GET /api/analytics/cycle-time, GET and PUT /api/settings/cycle-time-boundaries) require no authentication.
- boundary: none
- citation: "| GET | /api/analytics/cycle-time | None | ... | 200, 400 |" / "| GET | /api/settings/cycle-time-boundaries | None | -- | Boundary settings | 200 |" / "| PUT | /api/settings/cycle-time-boundaries | None | { startStage, endStage } | Updated boundaries | 200, 400 |"
- agreement: 1/3
- verdict: verified

### SR-27: get-boundaries-response-content
- statement: GET /api/settings/cycle-time-boundaries returns the configured start and end stage plus the full list of available stages (workflow stages + done statuses) for populating the dropdowns.
- boundary: none
- citation: "Returns the configured start stage and end stage, plus the full list of available stages (from workflow stages + done statuses) for populating the dropdowns."
- agreement: 1/3
- verdict: verified

### SR-28: get-boundaries-always-200
- statement: GET /api/settings/cycle-time-boundaries always returns 200, regardless of configuration state.
- boundary: none
- citation: "200: Always succeeds."
- agreement: 3/3
- verdict: verified

### SR-29: get-boundaries-defaults-when-unconfigured
- statement: When no boundaries are configured, GET /api/settings/cycle-time-boundaries returns the defaults (second workflow stage as start, first done status as end).
- boundary: none
- citation: "When no boundaries are configured, returns the defaults (second workflow stage, first done status)."
- agreement: 3/3
- verdict: verified

### SR-30: get-boundaries-empty-when-no-stages
- statement: When no workflow stages are configured, GET /api/settings/cycle-time-boundaries returns an empty available-stages list.
- boundary: none
- citation: "When no workflow stages are configured, returns empty available stages."
- agreement: 3/3
- verdict: verified

### SR-31: put-boundaries-400-invalid-stage
- statement: PUT /api/settings/cycle-time-boundaries returns 400 when the start stage or end stage does not match a configured workflow stage or done status.
- boundary: any unmatched stage name → 400
- citation: "400: Start stage or end stage does not match a configured workflow stage or done status."
- agreement: 3/3
- verdict: verified

### SR-32: put-boundaries-400-start-not-preceding-end
- statement: PUT /api/settings/cycle-time-boundaries returns 400 when the start stage does not precede the end stage in the configured order.
- boundary: spec word is "must precede" — an equal or later start stage does not precede, so → 400
- citation: "Start stage must precede end stage in the configured order."
- agreement: 3/3
- verdict: verified

### SR-33: sprintid-last-mutually-exclusive-400
- statement: The `sprintId` and `last` query parameters are mutually exclusive; providing both returns 400.
- boundary: both present simultaneously → 400
- citation: "`sprintId` (int, optional) — single sprint. Mutually exclusive with `last`." / "400: Both `sprintId` and `last` provided simultaneously"
- agreement: 3/3
- verdict: verified

### SR-34: last-below-one-400
- statement: GET /api/analytics/cycle-time returns 400 when `last` is less than 1.
- boundary: last < 1 → 400 (spec states only the failure side; last ≥ 1 valid by implication)
- citation: "400: `last` is less than 1"
- agreement: 3/3
- verdict: verified

### SR-35: subteam-param-optional-empty-means-all
- statement: The `subTeam` query parameter filters to developers in that sub-team; omitting it or passing empty returns unfiltered data.
- boundary: none
- citation: "`subTeam` (string, optional) — filter to developers in this sub-team. Omit or pass empty for all."
- agreement: 2/3
- verdict: verified

### SR-36: no-params-defaults-all-closed-sprints
- statement: When neither `sprintId` nor `last` is provided, the endpoint returns data for all closed sprints (multi-sprint mode).
- boundary: none
- citation: "When neither `sprintId` nor `last` is provided, returns data for all closed sprints (multi-sprint mode)."
- agreement: 3/3
- verdict: verified

### SR-37: frontend-default-sends-sprintid
- statement: The frontend pre-selects the most recent closed sprint on page load and sends `sprintId`, overriding the API's bare default.
- boundary: none
- citation: "The frontend controls the default experience — it pre-selects the most recent closed sprint on page load and sends `sprintId`."
- agreement: 1/3
- verdict: verified

### SR-38: sprintid-returns-single-last-returns-multi
- statement: Supplying `sprintId` returns the single-sprint response shape; supplying `last=N` returns the multi-sprint response shape.
- boundary: none
- citation: "GET /api/analytics/cycle-time with `sprintId` returns single-sprint data" / "GET /api/analytics/cycle-time with `last=N` returns multi-sprint data"
- agreement: 1/3
- verdict: verified

### SR-39: sprintid-unmatched-400
- statement: The API returns 400 when `sprintId` does not match any synced closed sprint.
- boundary: unmatched or non-closed sprintId → 400
- citation: "400: `sprintId` does not match any synced closed sprint"
- agreement: 3/3
- verdict: verified

### SR-40: sprint-null-when-no-closed-sprints
- statement: In the single-sprint response, the `sprint` field is null when no closed sprints exist.
- boundary: none
- citation: "`sprint` — sprint metadata (id, name, start date, end date). Null when no closed sprints exist."
- agreement: 3/3
- verdict: verified

### SR-41: null-sprint-is-200-not-error
- statement: A 200 response with null sprint data (no closed sprints exist) is a valid non-error outcome.
- boundary: none
- citation: "200 with null sprint: no closed sprints exist (not an error)"
- agreement: 3/3
- verdict: verified

### SR-42: metriccards-null-when-sprint-null
- statement: In the single-sprint response, `metricCards` is null whenever `sprint` is null.
- boundary: none
- citation: "`metricCards` — four metric card objects (see below). Null when sprint is null."
- agreement: 3/3
- verdict: verified

### SR-43: scatterplot-empty-when-no-completed-tickets
- statement: The `scatterPlot` list is empty when there are no completed tickets.
- boundary: none
- citation: "`scatterPlot` — list of ticket data points (see below). Empty when no completed tickets."
- agreement: 3/3
- verdict: verified

### SR-44: stagefunnel-empty-when-no-completed-tickets
- statement: The `stageFunnel` list is empty when there are no completed tickets.
- boundary: none
- citation: "`stageFunnel` — list of stage duration entries (see below). Empty when no completed tickets."
- agreement: 3/3
- verdict: verified

### SR-45: issuetypebreakdown-empty-when-no-completed-tickets
- statement: The `issueTypeBreakdown` list is empty when there are no completed tickets.
- boundary: none
- citation: "`issueTypeBreakdown` — list of issue type summary entries. Empty when no completed tickets."
- agreement: 3/3
- verdict: verified

### SR-46: developerbreakdown-empty-when-no-completed-tickets
- statement: The `developerBreakdown` list is empty when there are no completed tickets.
- boundary: none
- citation: "`developerBreakdown` — list of developer summary entries. Empty when no completed tickets."
- agreement: 3/3
- verdict: verified

### SR-47: outliers-empty-when-none-exceed-threshold
- statement: The `outliers` list is empty when no tickets exceed the outlier threshold.
- boundary: none
- citation: "`outliers` — list of outlier ticket entries. Empty when none exceed the threshold."
- agreement: 3/3
- verdict: verified

### SR-48: rework-count-zero-when-no-rework
- statement: A scatter plot data point's rework count is 0 when the ticket had no rework.
- boundary: 0 is the floor value
- citation: "Rework count (number of times the ticket re-entered any measured stage; 0 if no rework)"
- agreement: 3/3
- verdict: verified

### SR-49: stage-funnel-entry-includes-percentage
- statement: Each stage funnel entry carries the stage name, average duration in days (decimal), and percentage of total cycle time.
- boundary: none
- citation: "Stage funnel entry: - Stage name - Average duration in days (decimal) - Percentage of total cycle time"
- agreement: 1/3
- verdict: verified

### SR-50: completed-tickets-only
- statement: A ticket is included in cycle time calculations only if its final status in the sprint matches a value in the done statuses list; carried-over, removed, or still-in-progress tickets are excluded entirely.
- boundary: inclusion requires final-status membership in the done statuses list
- citation: "A ticket is included in cycle time calculations only if its final status in the sprint matches a value in the done statuses list. Tickets that carried over, were removed, or are still in progress are excluded entirely."
- agreement: 3/3
- verdict: verified

### SR-51: cycle-time-between-boundaries-inclusive
- statement: Cycle time is the accumulated duration spent in workflow stages between the configured start and end stage, inclusive of both; the clock starts when the ticket first enters the start stage and stops when it first enters a stage beyond the end stage (or the done status).
- boundary: start and end stages are both inclusive (spec word: "(inclusive)"); stop trigger is first entry beyond end stage or done status
- citation: "For each ticket, cycle time is the accumulated duration spent in workflow stages between the configured start stage and end stage (inclusive). The clock starts when the ticket first enters the start stage and stops when it first enters a stage beyond the end stage (or the done status)."
- agreement: 3/3
- verdict: verified

### SR-52: per-stage-time-accumulation
- statement: Time is accumulated per stage, not wall-clock: the stage clock starts on entry and stops on exit; a re-entry restarts the clock and adds the additional time; total cycle time is the sum across all measured stages.
- boundary: none
- citation: "When a ticket enters a stage, the clock for that stage starts. When it transitions out, the clock stops. If the ticket re-enters that stage later (rework), the clock restarts and the additional time is added. Total cycle time is the sum of time across all measured stages."
- agreement: 3/3
- verdict: verified

### SR-53: rework-count-re-entries
- statement: A ticket's rework count is the total number of re-entries across all measured stages; the spec's examples count In Progress → Code Review → In Progress → Code Review → Testing as 1 re-entry and In Progress → Code Review → In Progress → Code Review → In Progress → Testing as 2.
- boundary: per the AC, entering a stage 3 times yields 2 re-entries (N entries → N−1) — but see reason
- citation: "When a ticket transitions back into a previously visited stage, the re-entry is counted as rework. Each ticket carries a rework count — the total number of re-entries across all measured stages. Example: a ticket that goes In Progress → Code Review → In Progress → Code Review → Testing has 1 rework re-entry (it re-entered In Progress once). A ticket that goes In Progress → Code Review → In Progress → Code Review → In Progress → Testing has 2 rework re-entries."
- agreement: 3/3
- verdict: ambiguous
- reason: The general definition ("re-entry into a previously visited stage") counts the second Code Review entry in example 1 (total = 2), yet the spec states 1 — only a backward-transitions-only reading reconciles the definition, both examples, and the AC, and the spec never says that.

### SR-54: never-entered-start-stage-excluded
- statement: A ticket that never entered the configured start stage (e.g., went directly from a pre-start stage to done) has no measurable cycle time and is excluded from all calculations.
- boundary: entry into the start stage is a hard prerequisite for inclusion
- citation: "If a ticket's status transitions show it went directly from a pre-start stage (e.g., backlog) to done without ever entering the configured start stage, it has no measurable cycle time and is excluded from all calculations."
- agreement: 3/3
- verdict: verified

### SR-55: outlier-threshold-2x-median
- statement: A ticket is classified as an outlier when its cycle time exceeds twice the median cycle time of its sprint.
- boundary: strict > per the spec's word "exceeds"; exactly 2x median does not exceed and is not an outlier
- citation: "Any ticket whose cycle time exceeds twice the median cycle time of its sprint is classified as an outlier."
- agreement: 3/3
- verdict: verified

### SR-56: percentiles-completed-tickets-only
- statement: P50, P75, P85, and P90 are computed from the set of completed tickets that have measurable cycle time; excluded tickets do not affect percentile calculations.
- boundary: none
- citation: "P50 (median), P75, P85, and P90 are computed from the set of completed tickets that have measurable cycle time. Excluded tickets (incomplete, never entered start stage) do not affect percentile calculations."
- agreement: 3/3
- verdict: verified

### SR-57: stage-funnel-within-boundaries
- statement: The stage funnel includes only workflow stages between the configured start and end stage; stages before the start or after the end are omitted; each segment shows the average time tickets spent in that stage.
- boundary: only stages "before the start or after the end" are omitted — the start and end stages themselves are neither, so they are included (consistent with SR-51's "(inclusive)")
- citation: "The funnel visualization includes only workflow stages between the configured start stage and end stage. Stages before the start or after the end are omitted. Each segment shows the average time tickets spent in that stage."
- agreement: 3/3
- verdict: verified

### SR-58: dominant-stage-highest-average
- statement: A developer's dominant stage is the stage with the highest average time across that developer's tickets.
- boundary: none (max selection; tie-break undefined — see findings)
- citation: "The dominant stage is the stage with the highest average time for a developer's tickets."
- agreement: 3/3
- verdict: verified

### SR-59: delta-prior-sprint-null
- statement: Delta compares to the prior closed sprint, defined as the closed sprint with the next-earlier start date; when no prior sprint exists, delta is null.
- boundary: no prior closed sprint → delta = null
- citation: "\"Prior\" is the closed sprint with the next-earlier start date. When no prior sprint exists, delta is null."
- agreement: 3/3
- verdict: verified

### SR-60: delta-polarity
- statement: Delta polarity is positive-down (lower is better) for median cycle time, P85 cycle time, and outlier count; positive-up (more is better) for throughput.
- boundary: none
- citation: "Delta polarity: median cycle time, P85 cycle time, and outlier count are positive-down (lower is better). Throughput is positive-up (more completed tickets is better)."
- agreement: 3/3
- verdict: verified

### SR-61: delta-polarity-color-green
- statement: Metric cards render favorable deltas as green — lower cycle time is green, higher throughput is green.
- boundary: none
- citation: "Metric cards show delta vs prior sprint with correct polarity (lower cycle time = green, higher throughput = green)"
- agreement: 1/3
- verdict: verified

### SR-62: multi-sprint-per-sprint-averaging
- statement: Multi-sprint metric cards use per-sprint averaging with equal weight per sprint: median card = average of per-sprint medians, P85 card = average of per-sprint P85 values, throughput = average tickets completed per sprint, outliers = average outlier count per sprint (so the multi-sprint P85 card may differ from a pooled P85).
- boundary: none
- citation: "Median cycle time card shows the average of per-sprint medians. P85 card shows the average of per-sprint P85 values. Throughput shows the average tickets completed per sprint. Outliers shows the average outlier count per sprint. ... Per-sprint averaging gives equal weight to each sprint regardless of ticket volume, preventing large sprints from dominating the aggregate. This means the multi-sprint P85 card may differ from a pooled P85 computed across all tickets combined."
- agreement: 3/3
- verdict: verified

### SR-63: multi-sprint-delta-most-recent-vs-previous
- statement: In multi-sprint mode, the metric card delta compares the most recent sprint to the sprint immediately before it, not averaged values.
- boundary: none
- citation: "Delta compares the most recent sprint to the one before it."
- agreement: 3/3
- verdict: verified

### SR-64: empty-state-when-no-workflow-stages
- statement: If workflow stages are not configured at all, the Cycle Time page shows an empty state directing the user to Settings to configure stages.
- boundary: none
- citation: "If workflow stages are not configured at all, the cycle time page shows an empty state directing the user to Settings to configure stages."
- agreement: 3/3
- verdict: verified

### SR-65: calendar-days-measurement
- statement: Duration is measured in calendar days, including weekends and holidays, not business days.
- boundary: none
- citation: "Duration is measured in calendar days (including weekends and holidays), not business days."
- agreement: 3/3
- verdict: verified

### SR-66: zero-completed-tickets-zero-or-null
- statement: When no completed tickets exist for a sprint, all metrics are zero, percentiles are null, and the stage funnel and breakdowns are empty.
- boundary: trigger = zero completed tickets
- citation: "When no completed tickets exist for a sprint, all metrics are zero and percentiles are null. The stage funnel and breakdowns are empty."
- agreement: 3/3
- verdict: ambiguous
- reason: "all metrics are zero" and "percentiles are null" conflict for the Median and P85 metric cards, which are both metrics and percentiles — the spec never says which value (0 or null) those cards take.

### SR-67: sprint-ordering-start-date
- statement: Sprints are ordered by start date ascending; "most recent closed sprint" means the closed sprint with the latest start date.
- boundary: none
- citation: "Sprints are ordered by start date ascending. \"Most recent closed sprint\" means the closed sprint with the latest start date."
- agreement: 3/3
- verdict: verified

### SR-68: no-story-points-included
- statement: Tickets without story points are included in all cycle time calculations, since cycle time is measured by duration, not story points.
- boundary: none
- citation: "Cycle time is measured by duration, not story points. Tickets without story points are included in all cycle time calculations — unlike SP-based metrics that exclude them."
- agreement: 3/3
- verdict: verified

### SR-69: sprint-start-clamping
- statement: When a ticket is in a measured stage at sprint start, time accumulation begins from the sprint start date, not the ticket's original (possibly pre-sprint) entry into that stage — e.g., a ticket that entered "In Progress" 5 days before the sprint shows 0 pre-sprint days.
- boundary: clamp lower bound = sprint start date
- citation: "When a ticket is in a measured stage at the sprint start, time accumulation begins from the sprint start date (not from when the ticket originally entered that stage, which may have been before the sprint)."
- agreement: 3/3
- verdict: verified

### SR-70: sprint-end-clamping
- statement: When a ticket is still in a measured stage at sprint end, time accumulation ends at the sprint end date, bounding measurement to the sprint window.
- boundary: clamp upper bound = sprint end date
- citation: "When a ticket is still in a measured stage at sprint end, time accumulation ends at the sprint end date. This bounds the measurement to the sprint window."
- agreement: 3/3
- verdict: verified

### SR-71: completion-date-earliest-done-in-window
- statement: A ticket's completion date (scatter plot X-axis) is the timestamp of its earliest status transition to a done status within the sprint window.
- boundary: earliest qualifying transition, within the sprint window
- citation: "A ticket's completion date is the timestamp of its earliest status transition to a done status within the sprint window. This is used for the scatter plot X-axis."
- agreement: 3/3
- verdict: verified

### SR-72: pre-sprint-done-excluded
- statement: If all of a ticket's done-status transitions predate the sprint window (already done when added to the sprint), the ticket is excluded — it has no in-sprint cycle to measure.
- boundary: exclusion requires ALL done-status transitions to predate the window
- citation: "If a ticket's done-status transitions all predate the sprint window (ticket was already done when added to the sprint), it is excluded — it has no in-sprint cycle to measure."
- agreement: 3/3
- verdict: verified

### SR-73: excluded-from-scope-no-effect
- statement: The excluded-from-scope statuses setting (F10) does not affect cycle time; inclusion is governed solely by done status and start-stage entry.
- boundary: none
- citation: "A ticket that completed the full workflow has a valid cycle time regardless of scope exclusion. Cycle time inclusion is governed solely by done status and start-stage entry (rules 1 and 5)."
- agreement: 3/3
- verdict: verified

### SR-74: current-stage-config-at-query-time
- statement: The stage funnel and per-stage breakdowns use the workflow stages configured at query time, not the historical configuration; renamed or reordered stages retroactively apply to historical sprints.
- boundary: none
- citation: "The stage funnel and per-stage breakdowns use the workflow stages configured at query time, not the stages that were configured when the sprint was active. If a user renames or reorders stages in Settings, historical sprints reflect the new configuration."
- agreement: 3/3
- verdict: verified

### SR-75: unrecognized-stages-grouped-as-other
- statement: Stage names from status transitions that don't match any current configured stage name are grouped under an "Other" segment in the funnel.
- boundary: none
- citation: "Stage names from status transitions are matched against current stage names — unrecognized stage names (from a removed or renamed stage) are grouped under an \"Other\" segment in the funnel."
- agreement: 3/3
- verdict: verified

### SR-76: percentile-toggle-single-sprint-only
- statement: The percentile toggle applies only in single-sprint mode; multi-sprint mode has no scatter plot.
- boundary: none
- citation: "The percentile toggle on the scatter plot is a single-sprint feature — multi-sprint mode has no scatter plot."
- agreement: 3/3
- verdict: verified

### SR-77: multi-sprint-trend-always-p85
- statement: In multi-sprint mode, the trend line always shows P85, regardless of any prior single-sprint toggle selection (median is only an optional addition).
- boundary: none
- citation: "In multi-sprint mode, the trend line always shows P85 (and optionally median)."
- agreement: 3/3
- verdict: verified

### SR-78: shared-pipeline-all-issue-types
- statement: Cycle time measures all issue types against the same shared workflow stage sequence; there are no per-issue-type pipelines.
- boundary: none
- citation: "one shared pipeline for all issue types (established by F6). Different types may follow different paths, but cycle time measures all types against the same stage sequence."
- agreement: 1/3
- verdict: verified

## Spec findings

- **Rework definition contradicts its own examples (BR 4).** "Transitions back into a previously visited stage" counts any return, but example 1 (IP → CR → IP → CR → Testing) is scored 1, ignoring the second entry into Code Review. Only an unstated backward-transitions-only reading reconciles the definition, both examples, and the AC ("2 re-entries for a ticket entering a stage 3 times").
- **BR 15 is internally inconsistent for percentile-valued metric cards.** "All metrics are zero and percentiles are null" — the Median and P85 cards are both; the spec never says whether those cards show 0 or null when a sprint has no completed tickets.
- **Sprint-end clamping applies to a population BR 1 excludes.** BR 18 clamps tickets "still in a measured stage at sprint end", but BR 1 admits only tickets whose final status is done — it is unclear which included ticket is ever end-clamped.
- **Done transition after the sprint window is undefined.** BR 19 excludes tickets whose done transitions all predate the window, but never defines behavior when the only done-status transition occurs after the sprint window (membership final status done, transition late).
- **Clock-stop vs rework interplay is unresolved (BR 2 vs BR 3/4).** The clock "stops when it first enters a stage beyond the end stage", yet rework rules restart stage clocks on re-entry — whether time re-accumulates if a ticket returns to a measured stage after first passing beyond the end is never stated.
- **Outlier equality is pinned only by a verb.** "Exceeds 2x" implies strict >, but the exactly-2x case is never explicitly addressed; likewise, a sprint median of 0 makes every positive cycle time an outlier — degenerate case unaddressed.
- **PUT ordering validation is undefined when the end stage is a done status.** "Start stage must precede end stage in the configured order" — done statuses are not part of the workflow-stage order, and no combined ordering is defined.
- **Start-stage default is undefined with exactly one workflow stage.** "Second configured workflow stage" doesn't exist; the empty state covers zero stages, not one. Similarly, "first done status" default is undefined when no done statuses are configured.
- **Throughput's "completed the full cycle" is undefined.** Flow 1 defines Throughput as "count of tickets that completed the full cycle" — it is never stated whether this equals the completed-with-measurable-cycle set (i.e., whether BR 5-excluded tickets count).
- **Dominant stage tie-break undefined.** Two stages with equal highest average time — no rule for which wins.
- **AC contradicts Flow 5 on end-dropdown population.** AC: "Boundary dropdowns populate from the configured workflow stages list"; Flow 5: "Cycle ends at" also lists done statuses.
- **"(and optionally median)" in BR 22 is a non-commitment** — whether the multi-sprint trend includes a median series is undefined.
- **Funnel percentage denominator undefined.** "Percentage of total cycle time" per stage entry — sum of stage averages vs average of per-ticket totals is never specified.
- **Invalid `subTeam` value behavior undefined.** Error conditions list nothing for a `subTeam` that matches no sub-team (400 vs 200-with-empty is unspecified).
- **Multi-sprint delta with `last=1` (or a single selected sprint in range mode) is undefined** — "the one before it" may fall outside the selected range; whether the delta uses it or is null is unstated.

## Provenance

- Mode: `mine-from-spec` (Mine→Verify depth only — no Cover, no mutation gate)
- Manifest: `spec.md` only (`help.tooltips.md` / `help.page.md` deliberately excluded)
- Stages: 3 parallel clean-room miners (Sonnet) → consolidate + skeptic (fresh context)
- Miner yields: 79 / 47 / 71 rules → 78 consolidated
- Run date: 2026-07-03
