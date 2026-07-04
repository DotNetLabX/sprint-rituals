# F13-BugRatio — Spec Rules (mine-from-spec)

Spec-stamp: docs/specs/fokus/F13-BugRatio/definition/spec.md @ sha256:0ae232302d55 (2026-07-04)

Product of the `mine-verify-cover` spec arm (`mine-from-spec` mode, Mine→Verify depth):
3 clean-room miners read ONLY the stamped spec above; a fresh skeptic consolidated their
outputs with agreement counts and verified every rule against the spec text alone. This
documents what the SPEC commits to — not what the code does. No production source, tests,
or code-arm KB (`docs/kb/bug-ratio.md`) were read by any stage.

- consolidated rules: 42 — verified: 41 / ambiguous: 1
- agreement distribution: 3/3 × 30, 2/3 × 4, 1/3 × 8

## Consolidated rules

### SR-1: bug-ratio-alert-threshold-setting
- statement: The bug ratio alert threshold is a configurable app setting, a percentage in the range 0–100 with default 50; a developer is flagged when their completed-work bug ratio is at or above this threshold for consecutive sprints.
- boundary: Range written as "(0–100, default 50)"; comparison against it is inclusive ">=" (repaired 2026-07-04 per owner verdict — was "exceeds"; endpoint inclusivity of the range is still not stated — see findings).
- citation: "Bug ratio alert threshold: a percentage (0–100, default 50). When a developer's completed-work bug ratio is at or above this threshold (>=, inclusive) for consecutive sprints, an alert flag appears on the Developers page."
- agreement: 3/3
- layer: settings
- criticality: core
- verdict: verified

### SR-2: bug-ratio-consecutive-sprint-count-setting
- statement: The bug ratio consecutive sprint count is a configurable app setting, an integer in the range 1–10 with default 2, specifying how many consecutive sprints a developer must exceed the threshold before the alert activates.
- boundary: Range written as "(1–10, default 2)" (endpoint inclusivity not stated — see findings).
- citation: "Bug ratio consecutive sprint count: an integer (1–10, default 2). How many consecutive sprints a developer must exceed the threshold before the alert activates."
- agreement: 3/3
- layer: settings
- criticality: core
- verdict: verified

### SR-3: settings-reuse-existing-endpoints
- statement: Both new settings live on the existing app settings entity and are managed through the existing settings read/save endpoints; no new settings endpoints are introduced.
- boundary: none
- citation: "Both properties are added to the app settings entity alongside existing health thresholds. Managed through the existing settings read/save endpoints — no new settings endpoints needed."
- agreement: 1/3
- layer: api
- criticality: core
- verdict: verified

### SR-4: view-mode-selection-by-sprint-range
- statement: Selecting a range value ("Last 3", "Last 5", "All") in the sprint selector shows the multi-sprint trend view; selecting a single sprint shows the single-sprint detail view.
- boundary: Mode dispatch is range vs single-sprint selection; no other trigger.
- citation: "Selecting a range (Last 3/5/All) shows the multi-sprint trend view (Flow 1)" / "Selecting a single sprint shows the single-sprint detail view (Flow 2)"
- agreement: 1/3
- layer: ui
- criticality: core
- verdict: verified

### SR-5: query-param-mutual-exclusivity
- statement: The `sprintId` and `last` query parameters are mutually exclusive; a request supplying both returns 400.
- boundary: Both present → 400; exactly one, or neither, is valid.
- citation: "`sprintId` (int, optional) — single sprint. Mutually exclusive with `last`." / "400: Both `sprintId` and `last` provided simultaneously"
- agreement: 3/3
- layer: api
- criticality: core
- verdict: verified

### SR-6: default-last-5-closed-sprints
- statement: When neither `sprintId` nor `last` is provided, the endpoint defaults to the last 5 closed sprints (multi-sprint mode); the UI sprint selector has the same default.
- boundary: Applies only when both params are absent; default value is exactly 5.
- citation: "**Default behavior:** When neither `sprintId` nor `last` is provided, defaults to `last=5`." / "The sprint selector defaults to \"Last 5\" closed sprints"
- agreement: 3/3
- layer: api
- criticality: core
- verdict: verified

### SR-7: subteam-param-empty-means-all
- statement: The `subTeam` query parameter filters developers by sub-team name; omitting it or passing an empty value applies no sub-team filter.
- boundary: Empty string is treated the same as an absent parameter.
- citation: "`subTeam` (string, optional) — filter developers by sub-team name. Omit or pass empty for all."
- agreement: 2/3
- layer: api
- criticality: core
- verdict: verified

### SR-8: multi-sprint-response-top-level
- statement: In multi-sprint mode the response top level contains `sprints` (ordered summaries), `teamMetrics` (ratio from totals, total bug SP, total non-bug SP, total completed SP, per-sprint trend entries), `issueTypeBreakdown` (totaled across selected sprints), and `developers`.
- boundary: none
- citation: "`sprints` — ordered list of sprint summaries (id, name, start date, end date)" / "`teamMetrics` — team-level aggregates: bug ratio % (computed from totals, not averaged), total bug SP, total non-bug SP, total completed SP, and per-sprint trend data (one entry per sprint: bug ratio %, bug SP, non-bug SP, completed SP)" / "`issueTypeBreakdown` — completed ticket counts by raw Jira issue type, totaled across all selected sprints (type name, ticket count, SP total)" / "`developers` — list of per-developer bug ratio entries (see below)"
- agreement: 1/3
- layer: api
- criticality: core
- verdict: verified

### SR-9: multi-sprint-per-developer-entry
- statement: In multi-sprint mode each developer entry contains identity (display name, sub-team, avatar URL), totals across the selected range (bug SP, non-bug SP, total completed SP, bug ratio %, bug/non-bug ticket counts), a per-sprint breakdown with the same metrics per sprint, and alert status.
- boundary: none
- citation: "Developer identity: display name, sub-team, avatar URL" / "Totals across the selected range: bug SP, non-bug SP, total completed SP, bug ratio %, bug ticket count, non-bug ticket count" / "Per-sprint breakdown (one entry per sprint): sprint ID, bug SP, non-bug SP, total completed SP, bug ratio %, bug ticket count, non-bug ticket count" / "Alert status: whether the alert is active, current consecutive sprint count above threshold"
- agreement: 1/3
- layer: api
- criticality: core
- verdict: verified

### SR-10: single-sprint-response-top-level
- statement: In single-sprint mode the response top level contains `sprint` metadata, `teamMetrics` (bug ratio %, bug SP, non-bug SP, completed SP, each with delta vs prior sprint carrying value, direction, polarity), `issueTypeBreakdown` for that sprint, and `developers`.
- boundary: none
- citation: "`sprint` — sprint metadata (id, name, start date, end date)" / "`teamMetrics` — team-level: bug ratio %, bug SP, non-bug SP, completed SP, each with delta vs prior sprint (value, direction, polarity)" / "`issueTypeBreakdown` — completed ticket counts by raw Jira issue type for this sprint"
- agreement: 1/3
- layer: api
- criticality: core
- verdict: verified

### SR-11: single-sprint-per-developer-entry
- statement: In single-sprint mode each developer entry contains identity, the six metrics (bug SP, non-bug SP, total completed SP, bug ratio %, bug/non-bug ticket counts), per-metric delta values, and alert status.
- boundary: none
- citation: "Metrics: bug SP, non-bug SP, total completed SP, bug ratio %, bug ticket count, non-bug ticket count" / "Delta values (current minus prior sprint): bug SP delta, non-bug SP delta, bug ratio % delta, bug ticket count delta, non-bug ticket count delta, each with direction and polarity" / "Alert status: whether the alert is active, current consecutive sprint count above threshold"
- agreement: 1/3
- layer: api
- criticality: core
- verdict: verified

### SR-12: alert-status-fields-in-both-modes
- statement: Every per-developer entry, in both multi-sprint and single-sprint modes, includes alert status: whether the alert is active and the current consecutive sprint count above threshold.
- boundary: none
- citation: "Alert status: whether the alert is active, current consecutive sprint count above threshold" (stated identically under both response modes)
- agreement: 2/3
- layer: api
- criticality: core
- verdict: verified

### SR-13: delta-current-minus-prior
- statement: In single-sprint mode, per-developer delta values are computed as the current sprint's value minus the prior sprint's value, per metric, each carrying direction and polarity.
- boundary: Subtraction order is current − prior (explicit only for the per-developer entry; teamMetrics say only "delta vs prior sprint" — see findings).
- citation: "Delta values (current minus prior sprint): bug SP delta, non-bug SP delta, bug ratio % delta, bug ticket count delta, non-bug ticket count delta, each with direction and polarity"
- agreement: 2/3
- layer: domain-calc
- criticality: core
- verdict: verified

### SR-14: error-invalid-sprint-id
- statement: The API returns 400 when the supplied `sprintId` does not match any synced closed sprint.
- boundary: Match requires an existing, synced, closed sprint; anything else (including a synced but non-closed sprint) → 400.
- citation: "400: `sprintId` does not match any synced closed sprint"
- agreement: 3/3
- layer: api
- criticality: core
- verdict: verified

### SR-15: error-last-below-one
- statement: The API returns 400 when `last` is less than 1.
- boundary: `last` < 1 → 400 (spec states only the failure side; `last` = 1 being valid is implied, not stated).
- citation: "400: `last` is less than 1"
- agreement: 3/3
- layer: api
- criticality: core
- verdict: verified

### SR-16: empty-results-when-no-closed-sprints
- statement: When no closed sprints exist, the endpoint returns 200 with empty results rather than an error.
- boundary: Absence of any closed sprint → 200 + empty payload, not 400.
- citation: "200 with empty results: No closed sprints exist (not an error — just no data yet)"
- agreement: 3/3
- layer: api
- criticality: edge
- verdict: verified

### SR-17: bug-classification-by-issue-type
- statement: A ticket is a bug if and only if its Jira issue type equals "Bug" by exact, case-sensitive match; every other issue type is non-bug. There is no configurable classification in v1 — the split uses the raw Jira issue type.
- boundary: Exact, case-sensitive match on the literal "Bug"; any other casing or value is non-bug.
- citation: "A ticket is a bug if its issue type is \"Bug\" (exact match, case-sensitive — Jira consistently returns this casing). All other issue types (Story, Task, Sub-task, Improvement, custom types) are non-bug." / "No configurable classification rules in v1 — the binary split uses the raw Jira issue type."
- agreement: 3/3
- layer: domain-calc
- criticality: golden
- verdict: verified

### SR-18: completed-tickets-only
- statement: Only tickets whose final status in the sprint matches a value in the done statuses list count toward bug ratio metrics; assigned-but-unfinished tickets contribute nothing.
- boundary: Inclusion requires final status ∈ done statuses list.
- citation: "Bug ratio measures completed work only — tickets whose final status in the sprint matches a value in the done statuses list. Assigned-but-unfinished tickets do not contribute to the ratio."
- agreement: 3/3
- layer: domain-calc
- criticality: golden
- verdict: verified

### SR-19: bug-ratio-formula-per-developer-sprint
- statement: Per developer per sprint, bug ratio % = (sum of SP on completed bug tickets / sum of SP on all completed tickets) x 100.
- boundary: none (zero denominator governed by SR-24)
- citation: "**Bug ratio = Bug SP / Total Completed SP x 100.** Per developer per sprint: sum of SP on completed bug tickets divided by sum of SP on all completed tickets."
- agreement: 3/3
- layer: domain-calc
- criticality: golden
- verdict: verified

### SR-20: bug-ratio-formula-team-level
- statement: At the team level, bug ratio % = (sum of all developers' completed bug SP / sum of all developers' total completed SP) x 100.
- boundary: none (the "x 100" comes from the headline formula; the team-level sentence itself does not restate it)
- citation: "At the team level: sum of all developers' completed bug SP divided by sum of all developers' total completed SP."
- agreement: 3/3
- layer: domain-calc
- criticality: golden
- verdict: verified

### SR-21: removed-tickets-excluded
- statement: Tickets removed from the sprint (removal timestamp present) are excluded from all bug ratio metrics.
- boundary: Presence of a removal timestamp is the exclusion condition, regardless of status or SP.
- citation: "Tickets removed from the sprint (removal timestamp present) do not count toward any bug ratio metrics, matching the throughput pattern."
- agreement: 3/3
- layer: domain-calc
- criticality: core
- verdict: verified

### SR-22: excluded-from-scope-statuses-excluded
- statement: Tickets whose final status matches an excluded-from-scope status are excluded from all bug ratio calculations.
- boundary: Final status ∈ excluded-from-scope statuses list → excluded from all calculations.
- citation: "Tickets whose final status matches an excluded-from-scope status are excluded from all bug ratio calculations, consistent with the cross-cutting behavior introduced by F10."
- agreement: 3/3
- layer: domain-calc
- criticality: core
- verdict: verified

### SR-23: no-story-points-excluded-from-sp-metrics
- statement: Tickets with no story points are excluded from SP calculations (bug SP, non-bug SP, bug ratio %) but count in ticket-based metrics (bug ticket count, non-bug ticket count, issue type breakdown counts); exception — a completed bug ticket with no story points receives the Default SP per bug setting when it is configured (> 0), with the exclusion applying unchanged at the setting's default of 0.
- boundary: Presence vs absence of a story point value; the DefaultSpPerBug exception applies to bug tickets only, gated on the setting being > 0 (amended 2026-07-04 per owner verdict — the mined v1 spec predated the setting). Whether SP = 0 counts as "no story points" is still not stated (see findings).
- citation: "**Tickets with no story points are excluded from SP metrics.** They count in ticket-based metrics (bug ticket count, non-bug ticket count, issue type breakdown counts) but are absent from SP calculations (bug SP, non-bug SP, bug ratio %). Exception (amended 2026-07-04): when the Default SP per bug setting is configured (> 0), a completed **bug** ticket with no story points receives that default in SP calculations; at the setting's default of 0, the exclusion above applies unchanged."
- agreement: 3/3
- layer: domain-calc
- criticality: golden
- verdict: verified

### SR-24: zero-completed-sp-yields-zero-ratio
- statement: When a developer has no completed tickets with story points in a sprint, their bug ratio is 0% — not undefined or null.
- boundary: Zero completed SP (zero denominator) → exactly 0%, never null/error.
- citation: "**Zero completed SP produces zero bug ratio.** When a developer has no completed tickets with story points in a sprint, their bug ratio is 0% — not undefined or null."
- agreement: 3/3
- layer: domain-calc
- criticality: golden
- verdict: verified

### SR-25: multi-sprint-ratio-of-totals
- statement: The bug ratio across multiple sprints is total bug SP across all selected sprints divided by total completed SP across all selected sprints — a ratio of totals, not an average of per-sprint ratios — for both team-level and per-developer aggregated ratios.
- boundary: Applies whenever more than one sprint is aggregated.
- citation: "The bug ratio across multiple sprints is total bug SP across all selected sprints divided by total completed SP across all selected sprints. This applies to both team-level and per-developer aggregated ratios. This prevents high-SP sprints from skewing an average-of-ratios calculation." / "bug ratio % (computed from totals, not averaged)"
- agreement: 3/3
- layer: domain-calc
- criticality: golden
- verdict: verified

### SR-26: per-sprint-breakdown-still-provided
- statement: Despite ratio-of-totals aggregation, individual per-sprint breakdown data is still returned for trend chart rendering.
- boundary: none
- citation: "Individual per-sprint breakdown data is still provided for the trend chart."
- agreement: 3/3
- layer: api
- criticality: core
- verdict: verified

### SR-27: alert-uses-full-sprint-history
- statement: The consecutive-sprint alert is evaluated against all available sprint data, not just the sprints in the current view; this holds in both multi-sprint and single-sprint views.
- boundary: Evaluation window is always the full history regardless of the selected range.
- citation: "The consecutive sprint threshold is evaluated against all available sprint data, not just the sprints in the current view. If viewing \"Last 3\" and a developer has been above threshold for 4 consecutive sprints (including one outside the view), the alert still fires." / "Alert badges still visible on developers who meet the consecutive sprint threshold (computed from full sprint history, not just the selected sprint)"
- agreement: 3/3
- layer: domain-calc
- criticality: core
- verdict: verified

### SR-28: alert-streak-counted-backward-with-reset
- statement: Starting from the most recent closed sprint and counting backward, the streak is the number of consecutive sprints in which the developer's bug ratio is at or above the threshold (>=, inclusive); a sprint strictly below the threshold ends the streak.
- boundary: Qualifying condition is inclusive ">=" — a sprint exactly at the threshold counts toward the streak (repaired 2026-07-04 per owner verdict; the mined v1 wording was strict "exceeds", diverging from the code's deliberately-pinned ">=").
- citation: "Starting from the most recent closed sprint, count backward how many consecutive sprints the developer's bug ratio is at or above the threshold (>=, inclusive — a sprint exactly at the threshold counts toward the streak)." / "A gap (one sprint strictly below the threshold) ends the streak."
- agreement: 3/3
- layer: domain-calc
- criticality: golden
- verdict: verified

### SR-29: alert-active-when-count-meets-or-exceeds-setting
- statement: The alert is active when the consecutive above-threshold streak count meets or exceeds the configured consecutive sprint count.
- boundary: Inclusive comparison — streak ≥ configured count ("meets or exceeds").
- citation: "If this count meets or exceeds the configured consecutive sprint count, the alert is active."
- agreement: 3/3
- layer: domain-calc
- criticality: golden
- verdict: verified

### SR-30: zero-sp-sprint-breaks-streak
- statement: A sprint with no completed SP-bearing tickets yields a 0% bug ratio, which is below any positive threshold and therefore resets the developer's consecutive count.
- boundary: Completed SP = 0 for the sprint; the spec's stated reasoning covers only positive thresholds ("below any positive threshold") — threshold = 0 edge unaddressed (see findings).
- citation: "A sprint with no completed SP-bearing tickets results in 0% bug ratio, which is below any positive threshold, resetting the consecutive count."
- agreement: 3/3
- layer: domain-calc
- criticality: edge
- verdict: verified

### SR-31: only-active-developers
- statement: Developers marked as inactive are excluded from all bug ratio results.
- boundary: Binary gate on the developer's active flag; no partial inclusion.
- citation: "Developers marked as inactive are excluded from all bug ratio results."
- agreement: 3/3
- layer: api
- criticality: core
- verdict: verified

### SR-32: zero-ticket-developers-visible
- statement: Active developers with zero completed tickets in the selected sprints appear in the table with all-zero values; they are never hidden.
- boundary: Zero completed tickets in the selection → inclusion with zeroed values, not omission.
- citation: "Active developers with zero completed tickets in the selected sprints appear in the table with all-zero values. They are never hidden — consistent with the Throughput tab behavior (F9)."
- agreement: 3/3
- layer: domain-calc
- criticality: core
- verdict: verified

### SR-33: delta-single-sprint-only
- statement: Delta values (vs the prior closed sprint) are provided only when viewing a single sprint; multi-sprint views carry no delta.
- boundary: Delta presence gated strictly on single-sprint vs multi-sprint mode.
- citation: "When viewing a single sprint, each metric includes a delta comparing to the prior closed sprint. When viewing multiple sprints, no delta is provided — the trend charts serve this purpose."
- agreement: 3/3
- layer: domain-calc
- criticality: core
- verdict: verified

### SR-34: delta-polarity
- statement: Delta polarity per metric: bug ratio % — lower is better (green down, red up); bug SP — lower is better; non-bug SP — higher is better; bug ticket count — lower is better; non-bug ticket count — higher is better.
- boundary: none (categorical assignment per metric)
- citation: "Bug ratio %: lower is better (green down, red up). Bug SP: lower is better. Non-bug SP: higher is better. Bug ticket count: lower is better. Non-bug ticket count: higher is better."
- agreement: 3/3
- layer: domain-calc
- criticality: core
- verdict: verified

### SR-35: subteam-filter-applies-to-developers
- statement: The sub-team filter selects developers in that sub-team (with all their tickets), not tickets; team-level metrics recalculate using only those developers' data.
- boundary: Membership match on the developer's sub-team; tickets are never filtered directly.
- citation: "**Sub-team filter applies to developers, not tickets.** Filtering by sub-team shows developers in that sub-team with all their tickets. Team-level metrics recalculate using only those developers' data."
- agreement: 3/3
- layer: domain-calc
- criticality: core
- verdict: verified

### SR-36: subteam-all-clears-filter
- statement: Selecting "All" in the sub-team filter removes the restriction, returning results for all developers.
- boundary: "All" is the sentinel that clears the filter.
- citation: "Selecting \"All\" removes the filter"
- agreement: 3/3
- layer: ui
- criticality: core
- verdict: verified

### SR-37: sprint-ordering-by-start-date
- statement: Sprints are ordered by start date ascending; "most recent closed sprint" is the closed sprint with the latest start date; "prior sprint" for delta is the closed sprint with the next-earlier start date.
- boundary: Ordering key is start date; no tie-break stated for identical start dates (see findings).
- citation: "Sprints are ordered by start date ascending. \"Most recent closed sprint\" means the closed sprint with the latest start date. \"Prior sprint\" for delta means the closed sprint with the next-earlier start date."
- agreement: 3/3
- layer: domain-calc
- criticality: core
- verdict: verified

### SR-38: issue-type-breakdown-completed-only
- statement: The issue type breakdown counts only tickets with a final status in the done statuses list, grouped by raw Jira issue type with casing preserved as-is from Jira.
- boundary: Inclusion requires final status ∈ done statuses list; type labels are not normalized.
- citation: "The breakdown counts tickets by raw Jira issue type (case-preserved as-is from Jira). Only tickets with a final status in the done statuses list are included."
- agreement: 3/3
- layer: domain-calc
- criticality: core
- verdict: verified

### SR-39: tab-structure-and-persisted-filters
- statement: Bug Ratio is the second tab on the Developers page, after Throughput; Throughput remains the default active tab; the sprint selector and sub-team filter persist across tabs.
- boundary: none
- citation: "Bug Ratio is the second tab on the Developers page, after Throughput. The sprint selector and sub-team filter persist across tabs. Throughput remains the default active tab."
- agreement: 3/3
- layer: ui
- criticality: core
- verdict: verified

### SR-40: no-rolling-average
- statement: Unlike Throughput (F9), Bug Ratio computes no 3-sprint rolling average; trend context comes only from the trend chart and the consecutive-sprint alert.
- boundary: none (negative constraint)
- citation: "**Rolling average for bug ratio** — unlike throughput (F9), no 3-sprint rolling average. The trend chart and consecutive-sprint alert provide trend context without the added complexity."
- agreement: 2/3
- layer: domain-calc
- criticality: edge
- verdict: verified

### SR-41: f9-throughput-discrepancy-accepted
- statement: F9 (Throughput) predates F10 and does not apply excluded-from-scope statuses, so the Throughput and Bug Ratio tabs may show different total ticket counts for the same sprint when exclusions are configured; aligning F9 is not a BugRatio requirement.
- boundary: none
- citation: "F9 (Developer Throughput) was implemented before F10 introduced excluded-from-scope statuses and does not apply them. This means the Throughput tab and Bug Ratio tab may show different total ticket counts for the same sprint when exclusions are configured. Aligning F9 is a separate backlog item, not a BugRatio requirement."
- agreement: 1/3
- layer: ui
- criticality: edge
- verdict: ambiguous
- reason: Documents an accepted cross-feature discrepancy about F9's behavior ("may show different") — a scope acknowledgment, not a testable BugRatio commitment.

### SR-42: bug-priority-ignored
- statement: The ratio treats all bugs equally regardless of priority; there is no severity/priority split in v1.
- boundary: none — priority has no effect on classification or weighting.
- citation: "**Bug severity breakdown** — no split by priority (P1 vs P2 vs P3). The ratio treats all bugs equally regardless of priority."
- agreement: 1/3
- layer: domain-calc
- criticality: edge
- verdict: verified

## Spec findings

- **At-threshold sprint in alert evaluation:** ~~the qualifying condition is strict "exceeds", but the reset clause says "a gap (one sprint **below** threshold) resets the count." A sprint exactly at the threshold is neither.~~ **RESOLVED 2026-07-04** — owner verdict (M1 merge triage, `docs/kb/golden/BugRatioAnalyzer.md`): the code's inclusive `>=` is intended (matches the original Fokus); spec repaired to ">= inclusive", the equality case now named.
- **"Resets the count" vs backward counting:** the alert counts backward from the most recent sprint, where "reset" is an odd fit — effectively the count is the length of the consecutive exceeding run ending at the most recent closed sprint. "Resets to zero" is interpretation; the reset target is unstated.
- **Threshold boundary values:** the settings range permits threshold 0 and 100. At 100 the alert is unreachable (ratio cannot exceed 100%); at 0, the zero-SP justification ("0% ... below any positive threshold") does not apply — the streak still breaks via strict "exceeds", but the spec's stated reasoning covers only positive thresholds.
- **Setting range inclusivity:** "(0–100)" and "(1–10)" don't state endpoint inclusivity, and behavior on out-of-range saves is unspecified in this spec.
- **Delta with no prior sprint:** single-sprint view of the earliest closed sprint — "delta vs prior sprint" is undefined (null, zero, or omitted is never stated).
- **Delta subtraction order for teamMetrics:** "current minus prior sprint" is explicit only for per-developer deltas; teamMetrics deltas say only "delta vs prior sprint (value, direction, polarity)".
- **`last` exceeding available closed sprints:** behavior unstated (presumably clamps to all available; never committed).
- **Sprint ordering tie-break:** identical start dates have no stated tie-break for ordering, "most recent", or "prior sprint".
- **SP = 0 vs no story points:** whether a ticket with an explicit story-point value of 0 counts as "no story points" (SR-23) or as an SP-bearing ticket (SR-24/SR-30's "SP-bearing") is unstated; it affects denominator membership and streak-breaking.
- **Done ∩ excluded-from-scope:** precedence when a final status appears in both the done statuses list and the excluded-from-scope list is unstated; "excluded from all bug ratio calculations" implies exclusion wins, but it's not explicit.
- **Exclusions applied to issueTypeBreakdown:** the breakdown states only the done-status gate; whether removed tickets (SR-21) and excluded-from-scope statuses (SR-22) also apply is implied by "any bug ratio metrics"/"all bug ratio calculations", but never explicit.
- **Shape of "200 with empty results":** which mode's response shape is returned when no closed sprints exist (empty lists vs nulls) is unstated.
- **Unknown `subTeam` value:** a `subTeam` naming no existing sub-team is not among the 400 conditions — presumably 200 with empty developers, but unstated.
- **Zero denominator beyond per-developer-per-sprint:** 0% is defined for a developer within a sprint; team-level and multi-sprint-aggregate zero-denominator cases are covered only by the headline's generality ("Zero completed SP produces zero bug ratio"), not explicitly.

## Provenance

- Mode: `mine-from-spec` (Mine→Verify depth only — no Cover, no mutation gate)
- Manifest: `spec.md` only (`help.tooltips.md` / `help.page.md` deliberately excluded)
- Stages: 3 parallel clean-room miners (Sonnet) → consolidate + skeptic (fresh context)
- Miner yields: 27 / 33 / 38 rules → 42 consolidated
- Run date: 2026-07-03
- Facts (layer, criticality): added 2026-07-04 per the v1.21.0 consolidate-stage schema (operator pass, post-hoc — not part of the 2026-07-03 clean-room run)
- Delta re-check 2026-07-04: spec repaired per M1 merge owner verdicts (SR-28 ">=" inclusive; SR-23 DefaultSpPerBug exception) — stamp recomputed, SR-1/SR-23/SR-28 re-verified against the repaired spec; all other citations unchanged by the edits
