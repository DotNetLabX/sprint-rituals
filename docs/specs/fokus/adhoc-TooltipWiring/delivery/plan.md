# TooltipWiring

**Feature Spec:** None

## Context
Ten `help.tooltips.md` files define tooltip text for UI elements across the app. Some features already have tooltips wired; many do not. This plan covers wiring the missing tooltips only.

The established pattern uses an info-icon (`<svg>` circle-i) with a `title` attribute on a `<span>` next to the label, with `cursor-help` styling. See `client/src/components/epics/EpicSummaryCards.vue` and `client/src/components/cycle-time/CycleTimeMetricCards.vue` for reference implementations. Some tooltips go on the section heading itself (with `cursor-help` and `title`), and some go on table column `<th>` elements.

## Scope
**In scope:** Wiring missing tooltip text from `help.tooltips.md` files to corresponding Vue component elements using `title` attributes and info icons matching existing patterns.

**Out of scope:** Creating new tooltip components, changing tooltip behavior (hover delays, positioning), adding tooltips for conceptual/structural items that have no specific UI element (e.g., "Design Tokens", "Card-Based Layout").

## Already Complete (no work needed)

### CycleTime
All 16 tooltips are wired across `CycleTimeMetricCards.vue`, `CycleTimeScatterPlot.vue`, `PercentileToggle.vue`, `StageFunnel.vue`, `CycleTimeIssueTypeTable.vue`, `CycleTimeDeveloperTable.vue`, `CycleTimeOutlierTable.vue`, `CycleTimeTrendChart.vue`, `CycleTimeSprintSummaryTable.vue`, and `SettingsView.vue` (Cycle Starts At / Cycle Ends At).

### EpicProgress
All 12 tooltips are wired across `EpicSummaryCards.vue` (Active Epics, Average Completion, Unlinked Work), `EpicTable.vue` (Progress Bar, SP Done/Total, Tickets, Velocity, Projected, Sprints column headers), `EpicActiveCompletedToggle.vue` (Active/Completed Toggle), `EpicTicketTable.vue` (Ticket Row), and `PageToolbar.vue` (Sub-Team Filter).

## Implementation Steps

### Step 1: SprintSummaryCard tooltips (DashboardView + dashboard components)
**Skill:** None (pure template attribute additions)
**Pattern:** Follow `client/src/components/epics/EpicSummaryCards.vue` info-icon + title pattern

Add tooltips to these elements:

**File: `client/src/components/dashboard/HealthScoreBadge.vue`**
- "Health Score" label: add `cursor-help` and `title="Weighted composite of completion, disruption, and carry-over rates. 0-100 scale with green/amber/red status."`
- Each sub-score label (Completion, Disruption, Carry-Over): add `cursor-help` and `title="Individual 0-100 scores for completion, disruption, and carry-over that feed the composite."`

**File: `client/src/components/dashboard/MetricCard.vue`**
- The metric name `<div>`: add `cursor-help` and a computed tooltip based on `metric.name`:
  - "SP Completed" -> `title="Story points completed versus story points committed at sprint start."`
  - "Completion %" -> `title="Percentage of committed story points completed. Higher is better."`
  - "Disruption Rate" -> `title="Percentage of committed SP that was added mid-sprint. Lower is better."`
  - "Carry-Over Rate" -> `title="Percentage of total sprint scope (committed + added) not completed. Lower is better."`
- The delta display: add `cursor-help` and `title="Change versus the prior closed sprint. Arrow direction and color show whether the metric improved."`
- The sparkline container: add `title="Trend of the last 4 sprints ending at the selected sprint."`

**File: `client/src/views/DashboardView.vue`**
- "Top Epics" heading: add info icon with `title="Up to 3 epics with the most story points completed in this sprint."`
- "Developer Leaderboard" heading: add info icon with `title="All active developers ranked by story points completed in this sprint."`

**File: `client/src/components/dashboard/SprintFlags.vue`**
- "Zombie Tickets" heading: add info icon with `title="Tickets that have appeared in 3 or more sprints without being completed."`
- "Mid-Sprint Disruption" heading: add info icon with `title="Story points and ticket count added to the sprint after the first 2 days."`
- "Zero-SP Developers" heading: add info icon with `title="Active developers who had assigned tickets but completed zero story points."`
- "No flags this sprint" text: add `title="All sprint health indicators passed with no issues detected."`

### Step 2: DeveloperThroughput tooltips (DevelopersView)
**Skill:** None
**Pattern:** Follow `client/src/components/epics/EpicTable.vue` column header `title` pattern

**File: `client/src/views/DevelopersView.vue`**
- Single-sprint table column headers: add `title` attributes to `<th>` elements:
  - "SP Assigned" -> `title="Total story points on non-removed tickets assigned to the developer in this sprint."`
  - "SP Completed" -> `title="Story points on tickets the developer finished — those whose final status is in the done statuses list."`
  - "Completion %" -> `title="Percentage of assigned story points the developer completed (SP Completed / SP Assigned)."`
  - "Tickets Done" -> `title="Number of tickets the developer completed — those with a final status in the done statuses list."`
  - "Carried Over" -> `title="Non-removed tickets assigned to the developer that were not completed by sprint end."`
  - "Capacity %" -> `title="Developer's availability for a sprint as a percentage (0-100%). Default is 100% (fully available)."`
- Multi-sprint table: same tooltips on the equivalent "Avg" column headers
- Delta indicators: already have polarity-based coloring; add `title="Change from the prior sprint — green for improvement, red for regression, gray for neutral."` to the delta `<span>` elements
- "SP Completed Trend" chart heading: add info icon with `title="Multi-line chart of SP completed per developer across sprints, using 3-sprint rolling averages."`

### Step 3: ScopeChangeDisruption tooltips (Sprints page - scope change section)
**Skill:** None
**Pattern:** Follow info-icon + title pattern from EpicSummaryCards

**File: `client/src/components/sprints/ScopeMetricCards.vue`**
- Multi-sprint summary card labels:
  - "Avg Disruption Rate" -> `title="Mean percentage of unplanned work added mid-sprint relative to committed scope, across selected sprints."`
  - "Avg Net Scope Change" -> `title="Mean difference between added and removed SP across selected sprints. Positive means sprints grew."`
  - "Total Bugs Added" -> `title="Count of bug-type tickets added mid-sprint across all selected sprints."`
- Single-sprint metric card labels (dynamic from card.name):
  - "Committed SP (Active)" -> `title="Story points committed at sprint start. Active excludes tickets with excluded final statuses."`
  - "Committed SP (Total)" -> (same text variant for total)
  - "Added SP" -> `title="Story points on tickets added after the sprint started (not removed, not excluded)."`
  - "Removed SP" -> `title="Story points on tickets explicitly pulled out of the sprint."`
  - "Net Scope Change" -> `title="Added SP minus removed SP. Positive means the sprint grew; negative means it shrank."`
  - "Disruption Rate" -> `title="Added SP as a percentage of active committed SP. Lower is better."`
  - "Bug Count" -> `title="Number of bug-type tickets added mid-sprint, regardless of story points."`

**File: `client/src/components/sprints/ScopeChangeChart.vue`**
- "Scope Change by Sprint" heading: add `cursor-help` + `title="Per-sprint bars showing committed, added, removed, and completed SP side by side."`
- "Disruption Rate Trend" heading: add `cursor-help` + `title="Disruption rate percentage per sprint over time. Spot sustained increases early."`

**File: `client/src/components/sprints/ClassificationTable.vue`**
- "Classification Breakdown" heading: add info icon
- Add `title` attributes to category name cells or add a tooltip map for known categories:
  - "Planning Overflow" -> `title="Items added within the first 2 days — work missed during sprint planning, not true disruption."`
  - "Unplanned Bug" -> `title="Bug-type tickets added after day 2 of the sprint — reactive quality work consuming planned capacity."`
  - "Scope Injection" -> `title="New work added after day 2 that didn't exist before the sprint — truly unplanned scope."`
  - "Priority Escalation" -> `title="Pre-existing tickets pulled into the sprint after day 2 due to changed priorities."`

**File: `client/src/components/sprints/BurnupChart.vue`**
- "Scope Burnup" heading: add `cursor-help` + `title="Daily scope and completion lines showing when and how the sprint's total work changed."`

**File: `client/src/components/sprints/EventTable.vue`**
- "Scope Change Events" heading: add `cursor-help` + `title="Chronological log of every ticket added to or removed from the sprint after it started."`

**File: `client/src/components/sprints/BugTimeTable.vue`**
- "Bug Time in Progress" heading: add `cursor-help` + `title="Calendar days each mid-sprint bug spent in active work statuses — measures capacity consumed."`

### Step 4: CarryOverTracker tooltips (Sprints page - carry-over section)
**Skill:** None
**Pattern:** Same info-icon + title pattern

**File: `client/src/components/sprints/CarryOverMetricCards.vue`**
- Multi-sprint summary labels:
  - "Avg Carry-Over Rate" -> `title="The mean carry-over rate across the selected sprints."`
  - "Avg Carry-Over SP" -> (no specific tooltip text in help file, use the single-sprint equivalent)
  - "Total Zombie Tickets" -> `title="Count of tickets appearing in 3 or more sprints within the selected range."`
- Single-sprint card labels (dynamic):
  - "Carry-Over Rate" -> `title="Percentage of total sprint work (committed + added) not completed by sprint end."`
  - "Carry-Over SP" -> `title="Total story points on tickets not completed by sprint end."`
  - "Carry-Over Ticket Count" -> `title="Number of tickets not completed by sprint end."`

**File: `client/src/components/sprints/CarryOverRateChart.vue`**
- "Carry-Over Rate Trend" heading: add `cursor-help` + `title="How carry-over rate changes across sprints — a rising line signals growing delivery problems."`

**File: `client/src/components/sprints/CarryOverStackedChart.vue`**
- "Carry-Over SP by Workflow Stage" heading: add `cursor-help` + `title="Where unfinished work is stuck — which workflow phase accumulates the most carry-over."`

**File: `client/src/components/sprints/StatusDistributionChart.vue`**
- "Status Distribution" heading: add `cursor-help` + `title="Proportional breakdown of where carry-over tickets are stuck by workflow phase."`

**File: `client/src/components/sprints/IssueTypeBreakdown.vue`**
- "Issue Type Breakdown" heading: add `cursor-help` + `title="Carry-over ticket counts by work type — stories, bugs, tasks, improvements, etc."`

**File: `client/src/components/sprints/CarryOverDestination.vue`**
- "Prior Sprint Carry-Over" heading: add `cursor-help` + `title="What happened to the prior sprint's unfinished tickets — completed, carried again, removed, or dropped."`

**File: `client/src/components/sprints/ZombieSummaryTable.vue`**
- "Zombie Tickets" heading: add `cursor-help` + `title="Tickets that have lived through 3 or more sprints without being completed."`

**File: `client/src/components/sprints/ZombieTrajectorySection.vue`**
- "Zombie Trajectories" heading: add `cursor-help` + `title="Sprint-by-sprint status history showing how a zombie ticket moved (or didn't) through workflow stages."`

### Step 5: BugRatio tooltips (Developers page - Bug Ratio tab)
**Skill:** None
**Pattern:** Same info-icon + title pattern

**File: `client/src/components/developers/BugRatioMetricCards.vue`**
- Multi-sprint card labels:
  - "Team Bug Ratio" -> `title="Percentage of completed story points spent on bug fixes vs. all completed work across the selected sprints."`
  - "Total Bug SP" -> `title="Story points completed on bug-type tickets vs. all other ticket types (stories, tasks, sub-tasks)."` (use Bug SP / Non-Bug SP text)
  - "Total Non-Bug SP" -> same tooltip
- Single-sprint card labels: same tooltips using `card.name` for matching

**File: `client/src/components/developers/BugRatioTrendChart.vue`**
- "Bug Ratio Trend" heading: add `cursor-help` + `title="Bug ratio percentage per sprint over time. Spot sustained increases before they become the norm."`

**File: `client/src/components/developers/BugRatioStackedChart.vue`**
- "Bug SP vs Non-Bug SP per Sprint" heading: add `cursor-help` + `title="Bug SP (red) vs. Non-Bug SP (blue) per sprint for each developer. Compare allocation patterns."`

**File: `client/src/components/developers/BugRatioDevTable.vue`**
- "Developer Bug Ratio" heading: add info icon + `title="Bug and non-bug SP, ticket counts, bug ratio %, and alert status for each active developer."`
- Alert badge: update existing `title` to match help text: `title="Appears when a developer's bug ratio exceeds the threshold for consecutive sprints."`

**File: `client/src/components/developers/BugRatioIssueTypeBreakdown.vue`**
- "Completed Tickets by Issue Type" heading: add `cursor-help` + `title="Completed ticket counts grouped by raw Jira issue type. See composition beyond the bug/non-bug split."`

**File: `client/src/views/SettingsView.vue`** (Bug Ratio Alerts section)
- "Bug Ratio Alerts" section heading: add info icon + `title="Configure when the bug ratio alert triggers: threshold percentage and consecutive sprint count."`

### Step 6: JiraSync tooltips (SettingsView)
**Skill:** None
**Pattern:** Add info icon + title next to section headings and labels

**File: `client/src/views/SettingsView.vue`**
- "Jira Board" section heading: add info icon + `title="Link Fokus to your Jira instance using your email and an API token."`
- "Board" label: add `cursor-help` + `title="Choose which Jira board to sync sprints from. Only one board is supported."`
- "Done Statuses" section heading: add info icon (no specific tooltip — this is a settings concept, skip)
- "Sync from Jira" section heading: add info icon + `title="Pull sprint, ticket, developer, and transition data from Jira for the selected range."`
- "Sync All" button or nearby text: the sync operation text already explains it. Add `title="Re-syncing a sprint overwrites its data with fresh values from Jira. No duplicates created."` to the explanatory `<p>`.
- Sync result summary area: add `title="Post-sync report showing sprints synced, tickets upserted, developers found, and any failures."` to the "Sync complete!" text.

Note: Many JiraSync tooltips describe backend behavior concepts (Commitment Status, Removal Tracking, Final Status Capture, Status Transitions, Story Points, Active Sprint Handling, Assignee Attribution, Rate Limiting, Partial Failure) that have no corresponding UI element with an info icon location. These are informational for documentation and cannot be wired to a specific element. Skip these.

### Step 7: WorkflowAutoDetection tooltips (SettingsView)
**Skill:** None
**Pattern:** Same info-icon + title pattern

**File: `client/src/views/SettingsView.vue`**
- "Workflow Stages" section heading: add info icon + `title="The ordered list of status phases your team's tickets move through from start to done."`
- "Re-detect" button: add `title="Runs a fresh detection using current sync data, replacing the displayed proposal."`
- Confidence summary `<p>`: add `title="Shows how much data backs the detection: transitions analyzed, tickets covered, sprints spanned."`
- "Other statuses" label: add `cursor-help` + `title="Statuses found in your data but excluded from the main pipeline — add them manually if needed."`
- "Add stage..." input area: descriptive text says "Ordered workflow stages..."; add `title="Type workflow stages by hand if you already know your pipeline or prefer not to use detection."` to the input placeholder area

Note: Bug Exclusion, Forward-Flow Scoring, Done Status Anchoring, and Unsaved Changes have no specific UI element to attach to. Skip these.

### Step 8: AppShell tooltips (shared components)
**Skill:** None
**Pattern:** `title` attribute on existing elements

**File: `client/src/components/PageToolbar.vue`**
- Sprint selector already has the icon but no tooltip. Add `title="Choose a specific sprint to analyze, or select a range (Last 3, Last 5, All) for trend views."` to the sprint BaseSelect or its wrapper.

**File: `client/src/components/AppSidebar.vue`**
- "Dashboard" nav item: already shows label on hover when collapsed. No additional info-icon tooltip needed (structural navigation, not a metric).

Note: Sidebar Navigation, Dashboard, Empty States, Card-Based Layout, Design Tokens, Responsive Sidebar Collapse are structural/conceptual descriptions of the app, not metric elements that need info-icon tooltips. The Theme Toggle already has a descriptive `title` in AppHeader. Skip the rest.

## Cross-Service Changes
None. This is frontend-only work.

## Migration Notes
None.

## Testing Strategy
- For each modified component, visually verify the tooltip appears on hover of the info icon or cursor-help element.
- Verify tooltip text matches the `help.tooltips.md` content exactly.
- Verify no layout breakage from added info icons (check spacing).
- Spot-check in both dark and light themes.

## Open Questions
None.
