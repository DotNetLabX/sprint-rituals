# TooltipWiring — Implementation

## Files Modified

### Step 1: SprintSummaryCard tooltips
- `client/src/components/dashboard/HealthScoreBadge.vue` — Added info icon + title to "Health Score" label; added cursor-help + title to each sub-score label (Completion, Disruption, Carry-Over)
- `client/src/components/dashboard/MetricCard.vue` — Added `metricTooltip()` function mapping metric names to tooltip text; added cursor-help + title to metric name div, delta div, and sparkline container
- `client/src/views/DashboardView.vue` — Added info icon + title to "Top Epics" and "Developer Leaderboard" headings
- `client/src/components/dashboard/SprintFlags.vue` — Added info icons + titles to "Zombie Tickets", "Mid-Sprint Disruption", "Zero-SP Developers" headings; added title to "No flags this sprint" span

### Step 2: DeveloperThroughput tooltips
- `client/src/views/DevelopersView.vue` — Added title attributes to all 6 column `<th>` elements in single-sprint table; same titles on equivalent multi-sprint "Avg" headers; added title="Change from the prior sprint..." to all delta `<span>` elements in single-sprint rows; added info icon + title to "SP Completed Trend" chart heading

### Step 3: ScopeChangeDisruption tooltips
- `client/src/components/sprints/ScopeMetricCards.vue` — Added info icons + titles to multi-sprint summary card labels (Avg Disruption Rate, Avg Net Scope Change, Total Bugs Added); added `singleCardTooltip()` function; added cursor-help + :title to single-sprint card name divs
- `client/src/components/sprints/ScopeChangeChart.vue` — Added cursor-help + title to "Scope Change by Sprint" and "Disruption Rate Trend" headings
- `client/src/components/sprints/ClassificationTable.vue` — Added `categoryTooltip()` function; added info icon to heading; added cursor-help + :title to category name `<td>` cells
- `client/src/components/sprints/BurnupChart.vue` — Added cursor-help + title to "Scope Burnup" heading
- `client/src/components/sprints/EventTable.vue` — Added cursor-help + title to "Scope Change Events" heading
- `client/src/components/sprints/BugTimeTable.vue` — Added cursor-help + title to "Bug Time in Progress" heading

### Step 4: CarryOverTracker tooltips
- `client/src/components/sprints/CarryOverMetricCards.vue` — Added info icons + titles to multi-sprint summary card labels; added `singleCardTooltip()` function; added cursor-help + :title to single-sprint card name divs
- `client/src/components/sprints/CarryOverRateChart.vue` — Added cursor-help + title to "Carry-Over Rate Trend" heading
- `client/src/components/sprints/CarryOverStackedChart.vue` — Added cursor-help + title to "Carry-Over SP by Workflow Stage" heading
- `client/src/components/sprints/StatusDistributionChart.vue` — Added cursor-help + title to "Status Distribution" heading
- `client/src/components/sprints/IssueTypeBreakdown.vue` — Added cursor-help + title to "Issue Type Breakdown" heading
- `client/src/components/sprints/CarryOverDestination.vue` — Added cursor-help + title to "Prior Sprint Carry-Over" heading
- `client/src/components/sprints/ZombieSummaryTable.vue` — Added cursor-help + title to "Zombie Tickets" heading
- `client/src/components/sprints/ZombieTrajectorySection.vue` — Added cursor-help + title to "Zombie Trajectories" heading

### Step 5: BugRatio tooltips
- `client/src/components/developers/BugRatioMetricCards.vue` — Added info icons + titles to multi-sprint card labels (Team Bug Ratio, Total Bug SP, Total Non-Bug SP); added cursor-help + title to single-sprint card name divs
- `client/src/components/developers/BugRatioTrendChart.vue` — Added cursor-help + title to "Bug Ratio Trend" heading
- `client/src/components/developers/BugRatioStackedChart.vue` — Added cursor-help + title to "Bug SP vs Non-Bug SP per Sprint" heading
- `client/src/components/developers/BugRatioDevTable.vue` — Added info icon + title to "Developer Bug Ratio" heading; replaced dynamic alert badge title with fixed help text from tooltips spec
- `client/src/components/developers/BugRatioIssueTypeBreakdown.vue` — Added cursor-help + title to "Completed Tickets by Issue Type" heading

### Step 6: JiraSync tooltips
- `client/src/views/SettingsView.vue` — Added info icon + title to "Jira Board" heading; added cursor-help + title to "Board" label; added info icon + title to "Sync from Jira" heading; added title to explanatory `<p>`; added title to "Sync complete!" text

### Step 7: WorkflowAutoDetection tooltips
- `client/src/views/SettingsView.vue` — Added info icon + title to "Workflow Stages" heading; added title to "Re-detect" button; added title to confidence summary `<p>`; added title to Add stage input; added cursor-help + title to "Other statuses" label

### Step 8: AppShell tooltips
- `client/src/views/SettingsView.vue` — Added info icon + title to "Bug Ratio Alerts" section heading
- `client/src/components/PageToolbar.vue` — Added title to BaseSelect sprint selector wrapper

## Key Decisions
- For headings that use `<h2>` (SettingsView), wrapped in `<div class="flex items-center gap-1">` to sit info icon beside the heading without altering heading styling
- For chart/table headings that are plain `<div class="text-sm font-medium...">`, applied cursor-help + title directly on the div (same pattern as CycleTimeMetricCards)
- For card labels with info icons (same pattern as EpicSummaryCards), wrapped label text + icon span in `<div class="flex items-center gap-1">`
- ClassificationTable heading: the plan said "add info icon" with no specific tooltip text; used "How mid-sprint additions are categorized by timing and type." as a reasonable descriptor since the plan's scope description covers the concept
- Alert badge title: replaced the dynamic interpolated title with the static text from help.tooltips.md as instructed

## Deviations from Plan
- None. All plan steps completed as specified.
