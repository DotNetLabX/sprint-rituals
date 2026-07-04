# F8-SprintSummaryCard — Spec Rules (mine-from-spec)

Spec-stamp: docs/specs/fokus/F8-SprintSummaryCard/definition/spec.md @ sha256:83ccaa4627b0 (2026-07-03)

Product of the `mine-verify-cover` spec arm (`mine-from-spec` mode, Mine→Verify depth):
3 clean-room miners read ONLY the stamped spec above; a fresh skeptic consolidated their
outputs with agreement counts and verified every rule against the spec text alone. This
documents what the SPEC commits to — not what the code does. No production source, tests,
or code-arm KB (`docs/kb/health-score.md`) were read by any stage.

- consolidated rules: 58 — verified: 52 / ambiguous: 6
- agreement distribution: 3/3 × 49, 2/3 × 6, 1/3 × 3
- layer/criticality facts: added 2026-07-04 per the v1.21.0 consolidate schema (operator pass, post-hoc — facts only, rule content untouched). `domain-summary` marks domain rules homed in SprintSummaryAnalyzer, outside this merge's target class (HealthScoreCalculator).

## Consolidated rules

### SR-1: composite-rag-thresholds
- statement: The composite health score maps to a RAG status: green when the score is >= 75, amber when it is 40-74, red when it is < 40.
- boundary: Green inclusive at 75 (>=); Red strictly below 40; Amber stated as "40-74" — the score is a decimal, so values in (74, 75) are unassigned by the literal text ([40, 75) for amber is inferred, not stated).
- citation: "Green: composite score >= 75" / "Amber: composite score 40-74" / "Red: composite score < 40" / "**Composite RAG thresholds are fixed.** Green >= 75, Amber 40-74, Red < 40."
- agreement: 3/3
- layer: domain-calc
- criticality: golden
- verdict: ambiguous
- reason: Composite score is "(0-100, decimal)" but the amber band "40-74" leaves decimal scores in (74, 75) unassigned; miners' inclusive-band reading is inferred precision.

### SR-2: composite-rag-not-configurable
- statement: The composite RAG thresholds (75/40) are fixed constants in v1 and not user-configurable, unlike the per-metric thresholds and weights that feed the composite, which are user-configurable in app settings.
- boundary: none
- citation: "These are fixed in v1 (not user-configurable). The per-metric thresholds and weights that feed the composite are already user-configurable in app settings." / "These are not user-configurable in v1."
- agreement: 3/3
- layer: settings
- criticality: core
- verdict: verified

### SR-3: sprint-selector-defaults-most-recent
- statement: On opening the app or navigating to Dashboard, the sprint selector populates with closed sprints and defaults to the most recent closed sprint.
- boundary: none
- citation: "The sprint selector in the page toolbar populates with closed sprints, most recent selected by default" / "Opening the app navigates to Dashboard with the most recent closed sprint selected"
- agreement: 2/3
- layer: ui
- criticality: core
- verdict: verified

### SR-4: four-metric-cards
- statement: Exactly four metric cards are displayed: SP Completed/Committed, Completion %, Disruption Rate, and Carry-Over Rate.
- boundary: none
- citation: "Four metric cards in a row: SP Completed/Committed, Completion %, Disruption Rate, Carry-Over Rate" / "Four metric cards display: SP Completed/Committed, Completion %, Disruption Rate, Carry-Over Rate"
- agreement: 1/3
- layer: ui
- criticality: core
- verdict: verified

### SR-5: empty-state-when-no-closed-sprints
- statement: If no closed sprints exist, the Dashboard page shows the empty state defined by F7 instead of a summary card.
- boundary: zero closed sprints → empty state
- citation: "If no closed sprints exist, the page shows the empty state defined by F7"
- agreement: 3/3
- layer: ui
- criticality: edge
- verdict: verified

### SR-6: closed-sprints-ordered-descending
- statement: Closed sprints are listed ordered by start date descending (most recent first) in both the sprint selector dropdown and the GET /api/sprints/closed response, whose items carry (id, name, start date, end date, state).
- boundary: none
- citation: "Dropdown shows all closed sprints, ordered by start date descending (most recent first)" / "List of (id, name, start date, end date, state) ordered by start date descending"
- agreement: 3/3
- layer: api
- criticality: core
- verdict: verified

### SR-7: sprint-switch-recomputes-all-content
- statement: Selecting a different sprint updates all page content: health score, metric cards (including deltas and sparklines), epics, leaderboard, and flags.
- boundary: none
- citation: "All content on the page updates: health score, metric cards (including deltas and sparklines), epics, leaderboard, flags"
- agreement: 2/3
- layer: ui
- criticality: core
- verdict: verified

### SR-8: url-updates-on-sprint-switch
- statement: When the user switches the selected sprint, the page URL updates to reflect the newly selected sprint.
- boundary: none
- citation: "The URL updates to reflect the selected sprint"
- agreement: 3/3
- layer: ui
- criticality: core
- verdict: verified

### SR-9: sub-team-dropdown-options
- statement: The sub-team filter dropdown shows "All" (the default) plus one entry per distinct sub-team name from the developer roster.
- boundary: none
- citation: "Dropdown shows \"All\" (default) plus each distinct sub-team name from the developer roster"
- agreement: 2/3
- layer: ui
- criticality: core
- verdict: verified

### SR-10: sub-team-all-removes-filter
- statement: Selecting "All" in the sub-team filter removes any active sub-team restriction, reverting to unfiltered computation.
- boundary: none
- citation: "Selecting \"All\" removes the filter"
- agreement: 3/3
- layer: ui
- criticality: core
- verdict: verified

### SR-11: no-flags-message
- statement: When no flag types fire, the flags section shows the positive message "No flags this sprint" instead of flag content.
- boundary: zero flags fired → message shown
- citation: "If no flags fire, the section shows a positive message (\"No flags this sprint\")" / "Flags section shows \"No flags this sprint\" when no flags fire"
- agreement: 3/3
- layer: ui
- criticality: edge
- verdict: verified

### SR-12: sprintid-defaults-most-recent
- statement: The `sprintId` query parameter is optional; when omitted, the sprint summary defaults to the most recent closed sprint.
- boundary: omission is the trigger
- citation: "`sprintId` (int, optional) -- specific sprint. Defaults to the most recent closed sprint when omitted."
- agreement: 3/3
- layer: api
- criticality: core
- verdict: verified

### SR-13: subteam-param-omit-or-empty-means-all
- statement: The `subTeam` query parameter is optional; omitting it or passing an empty value applies no sub-team filter.
- boundary: omitted or empty string both mean unfiltered
- citation: "`subTeam` (string, optional) -- filter to developers in this sub-team. Omit or pass empty for all."
- agreement: 3/3
- layer: api
- criticality: core
- verdict: verified

### SR-14: no-closed-sprints-returns-200-null-sprint
- statement: When no closed sprints exist, the sprint-summary endpoint returns HTTP 200 with a null `sprint`; this is explicitly not an error condition.
- boundary: zero closed sprints → 200 + sprint = null
- citation: "200 with null sprint: no closed sprints exist (not an error)" / "`sprint` -- sprint metadata (id, name, start date, end date, duration in days, synced-at timestamp). Null when no closed sprints exist."
- agreement: 3/3
- layer: api
- criticality: edge
- verdict: verified

### SR-15: healthscore-metrics-null-when-sprint-null
- statement: In the sprint-summary response, `healthScore` and `metrics` are each null whenever `sprint` is null.
- boundary: sprint == null → healthScore == null and metrics == null
- citation: "`healthScore` -- composite health score object (see below). Null when sprint is null." / "`metrics` -- four metric card objects (see below). Null when sprint is null."
- agreement: 3/3
- layer: api
- criticality: edge
- verdict: verified

### SR-16: health-score-value-ranges
- statement: The composite health score is a decimal in 0-100; each of the three sub-scores (completion, disruption, carry-over) is also in 0-100.
- boundary: [0, 100] for composite and each sub-score
- citation: "Composite score (0-100, decimal)" / "Per-metric sub-scores: completion sub-score, disruption sub-score, carry-over sub-score (each 0-100)"
- agreement: 1/3
- layer: domain-calc
- criticality: core
- verdict: verified

### SR-17: delta-direction-domain
- statement: Delta direction takes one of the values up, down, flat, or null.
- boundary: value set enumerated; the condition distinguishing flat from up/down is not stated
- citation: "Delta direction (up, down, flat, or null)"
- agreement: 1/3
- layer: api
- criticality: core
- verdict: ambiguous
- reason: The value set is committed, but the spec never defines when direction is "flat" (exactly delta == 0? tolerance?), so the mapping cannot be tested from the spec alone.

### SR-18: sparkline-entry-shape
- statement: Each metric's sparkline data is a list of up to 4 entries, each with a sprint name and the metric value for that sprint.
- boundary: max 4 entries
- citation: "Sparkline data points (list of up to 4 entries, each with sprint name and value)"
- agreement: 2/3
- layer: api
- criticality: core
- verdict: verified

### SR-19: metric-display-value-format
- statement: Each metric card carries a formatted display string — examples show SP Completed/Committed as "24 / 30 SP" and rate metrics with one decimal place plus percent sign.
- boundary: none
- citation: "Display value (formatted string -- e.g., \"24 / 30 SP\", \"80.0%\", \"16.7%\")"
- agreement: 2/3
- layer: ui
- criticality: core
- verdict: ambiguous
- reason: Formats are given only as "e.g." illustrative examples; one-decimal percent rendering is miner-inferred, not a committed format contract.

### SR-20: topepics-empty-when-none
- statement: The `topEpics` list is empty (and the top-epics section hidden) when no epics had any SP completed in the selected sprint.
- boundary: zero qualifying epics → empty list
- citation: "`topEpics` -- list of epic progress objects, max 3. Empty list when no epics had progress." / "Top epics section is hidden when no epics had SP completed"
- agreement: 3/3
- layer: api
- criticality: edge
- verdict: verified

### SR-21: leaderboard-empty-when-no-developers
- statement: The `leaderboard` list is empty when there are no developers.
- boundary: zero developers → empty list
- citation: "`leaderboard` -- list of developer summary objects, sorted by SP completed descending. Empty list when no developers."
- agreement: 3/3
- layer: api
- criticality: edge
- verdict: verified

### SR-22: mid-sprint-disruption-null-when-none
- statement: The mid-sprint disruption flag field (total SP added after day 2, ticket count) is null when there are no mid-sprint additions.
- boundary: zero additions after day 2 → null (not zero values)
- citation: "Mid-sprint disruption: total SP added after day 2, ticket count. Null when no mid-sprint additions."
- agreement: 3/3
- layer: api
- criticality: edge
- verdict: verified

### SR-23: flags-has-any-flags-field
- statement: The flags object includes a boolean convenience field indicating whether any flag fired.
- boundary: none
- citation: "Has any flags (boolean convenience field)"
- agreement: 2/3
- layer: api
- criticality: core
- verdict: verified

### SR-24: invalid-sprintid-400
- statement: The sprint-summary endpoint returns HTTP 400 when a provided `sprintId` does not match any synced closed sprint.
- boundary: sprintId present and unmatched → 400
- citation: "400: `sprintId` provided but does not match any synced closed sprint"
- agreement: 3/3
- layer: api
- criticality: core
- verdict: verified

### SR-25: health-score-weighted-composite
- statement: The composite health score is the weighted average of three 0-100 sub-scores (completion, disruption, carry-over), each derived via the configurable threshold bands, using configurable weights defaulting to completion 40%, disruption 30%, carry-over 30%.
- boundary: none (weights configurable; 40/30/30 are defaults only)
- citation: "**Health score is a weighted composite of three sub-metrics.** Each sub-metric (completion rate, disruption rate, carry-over rate) maps to a 0-100 sub-score using the configurable threshold bands. The composite is their weighted average using the configurable weights (default: completion 40%, disruption 30%, carry-over 30%)."
- agreement: 3/3
- layer: domain-summary
- criticality: golden
- verdict: verified

### SR-26: completion-subscore-interpolation
- statement: For the completion sub-score (higher is better): at or above the green threshold the sub-score is 100; between amber and green it interpolates linearly from 50 to 99; below amber it interpolates linearly from 0 to 49.
- boundary: >= green → 100 (spec's "at or above", inclusive); "between amber and green" → 50-99; "below amber" → 0-49; the value at exactly the amber threshold is not assigned by the spec (50 is inferable only from the band endpoint).
- citation: "For completion (higher is better): at or above the green threshold scores 100; between amber and green interpolates linearly from 50 to 99; below amber interpolates linearly from 0 to 49."
- agreement: 3/3
- layer: domain-calc
- criticality: golden
- verdict: ambiguous
- reason: "Between amber and green" vs "below amber" leaves equality at the amber threshold unassigned; miners' amber→50 grouping is inferred, not stated.

### SR-27: disruption-carryover-subscore-interpolation
- statement: For the disruption and carry-over sub-scores (lower is better): at or below the green threshold the sub-score is 100; between green and amber it interpolates linearly from 99 to 50; above amber it interpolates linearly from 49 to 0, reaching 0 at twice the amber threshold.
- boundary: <= green → 100 (spec's "at or below", inclusive); "between green and amber" → 99-50; "above amber" (strict >) → 49-0 with floor reached at 2 × amber ("bottoming out"); the value at exactly the amber threshold is not assigned by the spec.
- citation: "For disruption and carry-over (lower is better): at or below the green threshold scores 100; between green and amber interpolates linearly from 99 to 50; above amber interpolates linearly from 49 to 0, bottoming out at twice the amber threshold."
- agreement: 3/3
- layer: domain-calc
- criticality: golden
- verdict: ambiguous
- reason: Same exactly-amber gap as SR-26 — equality at the amber threshold falls in neither "between green and amber" nor "above amber" by the literal text.

### SR-28: per-metric-rag-independent
- statement: Each metric card's own RAG color is computed from the per-metric thresholds configured in app settings, independently of the composite RAG.
- boundary: none
- citation: "**Per-metric RAG uses the configurable thresholds.** Each metric card shows its own RAG color based on the per-metric thresholds in app settings — independent of the composite RAG."
- agreement: 3/3
- layer: domain-calc
- criticality: golden
- verdict: verified

### SR-29: completed-uses-done-statuses
- statement: A ticket is "completed" if its final status in the sprint matches any value in the app settings done statuses list.
- boundary: none (set-membership test against configurable list)
- citation: "**\"Completed\" uses the done statuses list.** A ticket is completed if its final status in the sprint matches any value in the app settings done statuses list."
- agreement: 3/3
- layer: domain-summary
- criticality: golden
- verdict: verified

### SR-30: committed-sp-definition
- statement: Committed SP = sum of story points on memberships where the ticket was in the sprint at or before the sprint start date.
- boundary: "at or before" — inclusive of the start date
- citation: "**\"Committed\" means present at sprint start.** Committed SP = sum of story points on memberships where the ticket was in the sprint at or before the sprint start date."
- agreement: 3/3
- layer: domain-summary
- criticality: golden
- verdict: verified

### SR-31: added-sp-definition
- statement: Added SP = sum of story points on memberships added after sprint start where the ticket was not subsequently removed.
- boundary: "after sprint start" — strictly after (the complement of committed's inclusive "at or before"); later-removed memberships excluded
- citation: "**\"Added\" means mid-sprint, not removed.** Added SP = sum of story points on memberships added after sprint start where the ticket was not subsequently removed."
- agreement: 3/3
- layer: domain-summary
- criticality: golden
- verdict: verified

### SR-32: disruption-rate-formula
- statement: Disruption rate = added SP / committed SP x 100.
- boundary: none (zero denominator covered by SR-57)
- citation: "Disruption rate = added SP / committed SP x 100."
- agreement: 3/3
- layer: domain-summary
- criticality: golden
- verdict: verified

### SR-33: completion-rate-formula
- statement: Completion % = SP completed / SP committed x 100; the denominator is committed SP, not assigned SP.
- boundary: none (zero denominator covered by SR-57)
- citation: "**Completion rate measures commitment delivery.** Completion % = SP completed / SP committed x 100. The denominator is committed SP (not assigned SP) because the summary card measures how well the team delivered against its sprint commitment."
- agreement: 3/3
- layer: domain-summary
- criticality: golden
- verdict: verified

### SR-34: carryover-sp-definition
- statement: Carry-over SP = sum of story points on non-removed memberships whose final status is not in the done statuses list.
- boundary: removed memberships excluded; done-status tickets excluded
- citation: "**\"Carry-over\" is everything not done.** Carry-over SP = sum of story points on non-removed memberships whose final status is not in the done statuses list."
- agreement: 3/3
- layer: domain-summary
- criticality: golden
- verdict: verified

### SR-35: carryover-rate-formula
- statement: Carry-over rate = carry-over SP / (committed SP + added SP) x 100.
- boundary: none (zero denominator covered by SR-58)
- citation: "**Carry-over rate denominator includes both committed and added.** Carry-over rate = carry-over SP / (committed SP + added SP) x 100. This reflects the total work the team was responsible for, not just what they started with."
- agreement: 3/3
- layer: domain-summary
- criticality: golden
- verdict: verified

### SR-36: delta-prior-sprint-definition
- statement: The "prior" sprint for delta comparison is the closed sprint with the next-earlier start date relative to the selected sprint.
- boundary: none
- citation: "**Delta compares to the prior closed sprint.** \"Prior\" is the closed sprint with the next-earlier start date relative to the selected sprint."
- agreement: 3/3
- layer: domain-summary
- criticality: core
- verdict: verified

### SR-37: delta-null-when-no-prior-sprint
- statement: When no prior closed sprint exists, the delta is null, not zero.
- boundary: absence of prior sprint → null (explicitly not 0)
- citation: "When no prior sprint exists, delta is null (not zero)."
- agreement: 3/3
- layer: domain-summary
- criticality: core
- verdict: verified

### SR-38: delta-polarity-completion-pct
- statement: For Completion %, higher is better: an upward delta is green (positive polarity), a downward delta is red (negative polarity).
- boundary: none
- citation: "Completion % and SP completed/committed: higher is better (green up, red down)." / "Delta polarity: completion % higher=green; disruption/carry-over lower=green; SP completed/committed neutral"
- agreement: 3/3
- layer: domain-summary
- criticality: core
- verdict: verified

### SR-39: delta-polarity-disruption-carryover
- statement: For Disruption rate and Carry-over rate, lower is better: a downward delta is green (positive polarity), an upward delta is red (negative polarity).
- boundary: none
- citation: "Disruption rate and carry-over rate: lower is better (green down, red up)."
- agreement: 3/3
- layer: domain-summary
- criticality: core
- verdict: verified

### SR-40: delta-polarity-sp-completed-committed-neutral
- statement: The SP Completed/Committed card's delta is neutral (no color), because more committed SP is neither inherently good nor bad.
- boundary: none
- citation: "Completion % and SP completed/committed: higher is better (green up, red down)." / "SP completed/committed is neutral (no color) because more committed SP is neither inherently good nor bad." / "Delta polarity: completion % higher=green; disruption/carry-over lower=green; SP completed/committed neutral"
- agreement: 3/3
- layer: domain-summary
- criticality: core
- verdict: ambiguous
- reason: BR-12 contradicts itself — its first sentence assigns SP completed/committed higher-is-better (green up, red down) while its final clause and the Acceptance Criteria assign neutral; neutral is the likelier intent (2 of 3 statements plus a rationale) but the spec is internally inconsistent.

### SR-41: sparkline-4-sprint-trailing-window
- statement: The sparkline always shows the 4 most recent closed sprints ending at and including the selected sprint; the window shifts backward when an older sprint is selected, always ending at the selected sprint.
- boundary: window capped at 4; anchored with the selected sprint as the last point
- citation: "**Sparkline is a fixed 4-sprint trailing window.** Always shows the 4 most recent closed sprints ending at and including the selected sprint — the window shifts backward when an older sprint is selected, always ending at the selected sprint."
- agreement: 3/3
- layer: domain-summary
- criticality: core
- verdict: verified

### SR-42: sparkline-fewer-when-fewer-exist
- statement: If fewer than 4 closed sprints exist, the sparkline shows however many are available rather than padding to 4.
- boundary: total closed sprints < 4 → show what's available
- citation: "If fewer than 4 closed sprints exist, show what's available." / "Sparklines show fewer points when fewer than 4 sprints exist"
- agreement: 3/3
- layer: domain-summary
- criticality: edge
- verdict: verified

### SR-43: top-epics-eligibility-and-ranking
- statement: Top epics are ranked by SP completed in the selected sprint; only epics with at least 1 SP completed in the sprint appear; the list is capped at the top 3.
- boundary: eligibility >= 1 SP completed in the selected sprint (spec's "at least 1"); max 3 entries
- citation: "**Top epics are ranked by SP completed in the selected sprint.** Only epics with at least 1 SP completed in the sprint appear. Take the top 3."
- agreement: 3/3
- layer: domain-summary
- criticality: core
- verdict: verified

### SR-44: top-epics-overall-progress-scope
- statement: Each epic's overall progress (done SP / total SP) is computed across all sprints, not just the selected one.
- boundary: none
- citation: "Overall progress (done/total SP) is computed across all sprints, not just the selected one."
- agreement: 3/3
- layer: domain-summary
- criticality: core
- verdict: verified

### SR-45: leaderboard-active-developers-only
- statement: The developer leaderboard shows all active developers; inactive developers are excluded.
- boundary: inactive → excluded regardless of SP
- citation: "**Developer leaderboard shows all active developers.** ... Inactive developers are excluded."
- agreement: 3/3
- layer: domain-summary
- criticality: core
- verdict: verified

### SR-46: leaderboard-sorted-sp-desc
- statement: The leaderboard is sorted by SP completed descending.
- boundary: none
- citation: "Sorted by SP completed descending."
- agreement: 3/3
- layer: domain-summary
- criticality: core
- verdict: verified

### SR-47: leaderboard-zero-sp-still-appear
- statement: Active developers with 0 SP completed still appear on the leaderboard, sorted to the bottom.
- boundary: SP completed = 0 → still listed, ranked last
- citation: "Developers with 0 SP completed still appear (sorted to the bottom)."
- agreement: 3/3
- layer: domain-summary
- criticality: edge
- verdict: verified

### SR-48: leaderboard-no-truncation
- statement: The leaderboard has no truncation or cap on the number of entries.
- boundary: none (unbounded list)
- citation: "No truncation or cap."
- agreement: 3/3
- layer: domain-summary
- criticality: core
- verdict: verified

### SR-49: zombie-ticket-threshold
- statement: A ticket that appears in sprint memberships for 3 or more distinct sprints is flagged as a zombie.
- boundary: >= 3 distinct sprints (inclusive at exactly 3)
- citation: "**Zombie ticket threshold is 3 sprints.** A ticket that appears in sprint memberships for 3 or more distinct sprints is flagged as a zombie."
- agreement: 3/3
- layer: domain-summary
- criticality: golden
- verdict: verified

### SR-50: zombie-count-scope
- statement: The zombie sprint count includes all sprints the ticket appears in (including the selected one), and the sprints need not be consecutive.
- boundary: none
- citation: "The count includes all sprints (including the selected one), not just consecutive sprints."
- agreement: 3/3
- layer: domain-summary
- criticality: core
- verdict: verified

### SR-51: mid-sprint-disruption-grace-period
- statement: Tickets added to the sprint more than 2 days after the sprint start date, and not removed, are flagged as mid-sprint disruption; day 1-2 additions are planning overflow, not disruption. The flag reports total SP and ticket count. The 2-day grace period is fixed in v1.
- boundary: strictly more than 2 days after start (day 3+); day 1-2 additions excluded
- citation: "**Mid-sprint disruption uses a 2-day grace period.** Tickets added to the sprint more than 2 days after the sprint start date (and not removed) are flagged as mid-sprint disruption. Day 1-2 additions are considered planning overflow, not disruption. The flag reports total SP and ticket count." / "**Configurable disruption grace period** -- fixed at 2 days."
- agreement: 3/3
- layer: domain-summary
- criticality: golden
- verdict: verified

### SR-52: zero-sp-developers-flag
- statement: Any active developer assigned to the sprint with 0 SP completed is flagged; developers not assigned to the sprint at all are not flagged, even though they appear in the leaderboard at 0.
- boundary: flag requires active AND assigned to the sprint (had tickets) AND exactly 0 SP completed
- citation: "**Zero-SP developers are active developers only.** Any active developer assigned to the sprint with 0 SP completed is flagged. Developers not assigned to the sprint at all are not flagged — they would appear in the leaderboard at 0 but are not a \"flag\" unless they had tickets."
- agreement: 3/3
- layer: domain-summary
- criticality: core
- verdict: verified

### SR-53: sub-team-filter-scopes-everything
- statement: When a sub-team is selected, all computations (metrics, health score, leaderboard, epics, flags) are restricted to tickets assigned to developers in that sub-team.
- boundary: none
- citation: "**Sub-team filter scopes everything.** When a sub-team is selected, all computations (metrics, health score, leaderboard, epics, flags) are restricted to tickets assigned to developers in that sub-team."
- agreement: 3/3
- layer: domain-summary
- criticality: golden
- verdict: verified

### SR-54: dashboard-single-sprint-only
- statement: The Dashboard sprint selector shows individual closed sprints only; the aggregate options (Last 3, Last 5, All) stubbed by F7's toolbar are suppressed on Dashboard.
- boundary: none
- citation: "**Dashboard is single-sprint only.** The sprint selector on Dashboard shows individual closed sprints. The sprint selector toolbar built by F7 includes visual stubs for aggregate options (Last 3, Last 5, All) — on Dashboard, these are suppressed."
- agreement: 3/3
- layer: ui
- criticality: core
- verdict: verified

### SR-55: tickets-no-sp-excluded-from-sp-metrics
- statement: Tickets with no story points do not count as 0 SP — they are absent from committed SP, completed SP, added SP, carry-over SP, and all rate calculations.
- boundary: story points absent → total exclusion from SP sums and rates (distinct from a 0 value)
- citation: "**Tickets with no story points are excluded from SP metrics.** They do not count as 0 SP. They are simply absent from committed SP, completed SP, added SP, carry-over SP, and all rate calculations."
- agreement: 3/3
- layer: domain-summary
- criticality: golden
- verdict: verified

### SR-56: tickets-no-sp-count-for-ticket-flags
- statement: Tickets with no story points still count toward ticket-based flags, e.g. zombie tickets, which count by membership rather than SP.
- boundary: none
- citation: "They still count toward ticket-based flags (zombie tickets count by membership, not SP)."
- agreement: 3/3
- layer: domain-summary
- criticality: core
- verdict: verified

### SR-57: division-by-zero-committed
- statement: When committed SP is 0, completion % is 0 and disruption rate is 0 (not undefined/NaN).
- boundary: committed SP == 0 triggers the rule for both rates
- citation: "**Division by zero produces zero.** When committed SP is 0, completion % is 0 and disruption rate is 0."
- agreement: 3/3
- layer: domain-summary
- criticality: golden
- verdict: verified

### SR-58: division-by-zero-carryover
- statement: When committed SP + added SP is 0, carry-over rate is 0 (not undefined/NaN).
- boundary: (committed SP + added SP) == 0 triggers the rule
- citation: "When committed + added SP is 0, carry-over rate is 0."
- agreement: 3/3
- layer: domain-summary
- criticality: edge
- verdict: verified

## Spec findings

- **BR-12 self-contradiction (delta polarity):** the first sentence groups "Completion % and SP completed/committed" as "higher is better (green up, red down)"; the final clause and the Acceptance Criteria say SP completed/committed is neutral (no color). Neutral is the likelier intent (2 of 3 statements plus rationale), but the spec must pick one.
- **Composite RAG bands don't partition decimal scores:** composite score is "(0-100, decimal)" but bands are "Green >= 75, Amber 40-74, Red < 40" — scores in (74, 75) are formally unassigned; amber = [40, 75) must be inferred.
- **Exactly-amber sub-score unassigned:** for both interpolation directions, "between amber and green"/"between green and amber" vs "below amber"/"above amber" never assigns the value at exactly the amber threshold; amber → 50 is inferable only from the interpolation endpoint. Design also implies 1-point discontinuities at green (99 vs 100) and amber (49 vs 50) that the spec never acknowledges.
- **Completion below-amber lower anchor unstated:** "below amber interpolates linearly from 0 to 49" never says score 0 occurs at completion 0% — the lower anchor is implied, not stated.
- **Per-metric RAG mapping undefined:** BR-4 says metric cards use "the per-metric thresholds in app settings" but never defines the classification (band direction per metric, boundary inclusivity) — only the sub-score interpolation is specified, and it produces scores, not RAG colors.
- **"Flat" delta direction undefined:** the response shape enumerates (up, down, flat, or null) but no rule states when direction is flat (exactly 0? tolerance?).
- **SP Completed/Committed scalar value undefined:** the metric card object has one "Value (decimal)", but for the compound SP Completed/Committed card ("24 / 30 SP") the spec never says what the decimal value — and hence its delta and sparkline points — is (completed? committed? ratio?).
- **Top-epics threshold vs phrasing divergence:** BR-14 requires "at least 1 SP completed" but Flow 1/AC/API say "epics with SP completed"/"no epics had progress" — these diverge for fractional SP (e.g. 0.5), and the spec never pins SP granularity.
- **Top-epics tie-break unstated:** ranking/cutoff behavior when epics tie on SP completed at the top-3 boundary is undefined.
- **Leaderboard tie ordering unstated:** ordering among developers with equal SP completed (including the 0-SP group) is undefined.
- **Sparkline fewer-points clause scoped to total scarcity:** "If fewer than 4 closed sprints exist" doesn't literally cover selecting an old sprint with fewer than 3 predecessors; the window definition implies fewer points there, but the spec never says so.
- **Zombie count temporal scope:** "the count includes all sprints (including the selected one)" — whether sprints *later* than a historically selected sprint count is unstated.
- **Mid-sprint day arithmetic undefined:** "more than 2 days after the sprint start date" / "Day 1-2" never defines calendar days vs 24-hour periods, timezone, or whether an addition at exactly 48 hours counts.
- **Defined but surprising division-by-zero outcome:** committed SP = 0 with added SP > 0 yields disruption rate 0 — a sprint that is 100% mid-sprint additions reports zero disruption. Committed behavior, questionable intent.
- **Assignment semantics undefined:** both sub-team scoping ("tickets assigned to developers in that sub-team") and the zero-SP flag ("assigned to the sprint"/"had tickets") rely on an assignment notion the spec never defines (assignee snapshot timing, unassigned tickets, developers with null sub-team reachable only via "All").

## Provenance

- Mode: `mine-from-spec` (Mine→Verify depth only — no Cover, no mutation gate)
- Manifest: `spec.md` only (`help.tooltips.md` / `help.page.md` deliberately excluded)
- Stages: 3 parallel clean-room miners (Sonnet) → consolidate + skeptic (fresh context)
- Miner yields: 44 / 59 / 45 rules → 58 consolidated
- Run date: 2026-07-03
