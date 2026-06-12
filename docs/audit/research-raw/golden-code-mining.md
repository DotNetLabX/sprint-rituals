I now have a comprehensive picture of all the implemented business rules. Here is the complete findings report:

---

## Business Rules — Fokus Domain (Raw Findings)

### Analytics / Health Scoring

---

```
RULE: Higher-is-better metric score: value >= green → 100; [amber, green) → linear 50–99; [0, amber) → linear 0–49; clamps at 0; amber==0 guard returns 0
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\HealthScoreCalculator.cs:13-24
SNIPPET:
    if (value >= green) return 100;
    if (value >= amber) return 50 + (value - amber) / range * 49;
    if (amber == 0) return 0;
    return Math.Max(0, value / amber * 49);
KIND: formula
AREA: health-metrics
```

```
RULE: Lower-is-better metric score: value <= green → 100; (green, amber] → linear 99–50; > amber → linear 49–0, hitting 0 at 2×amber; range==0 guard returns 50
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\HealthScoreCalculator.cs:29-42
SNIPPET:
    if (value <= green) return 100;
    if (value <= amber) return 99 - (value - green) / range * 49;
    var cap = amber * 2;
    return Math.Max(0, 49 - (value - amber) / (cap - amber) * 49);
KIND: formula
AREA: health-metrics
```

```
RULE: Composite RAG: score >= 75 → "green"; >= 40 → "amber"; else → "red"
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\HealthScoreCalculator.cs:44-45
SNIPPET: score >= 75 ? "green" : score >= 40 ? "amber" : "red"
KIND: classification
AREA: health-metrics
```

```
RULE: Composite health score = (completionScore × completionWeight + disruptionScore × disruptionWeight + carryOverScore × carryOverWeight [+ qualitySubScore × qualityWeight when hasQaData && qualityWeight > 0]) / totalWeight; totalWeight == 0 → 0
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\SprintSummaryCalculator.cs:462-479
SNIPPET:
    totalWeight = deliverySum + qualityWeight; // or deliverySum only
    composite = totalWeight > 0 ? compositeNumerator / totalWeight : 0;
KIND: formula
AREA: health-metrics
```

```
RULE: Default health thresholds: Completion green=80, amber=60; Disruption green=10, amber=25; CarryOver green=10, amber=25; Default weights: Completion=40, Disruption=30, CarryOver=30
SOURCE: src\Services\Fokus\Fokus.Domain\Settings\ValueObjects\HealthThresholdConfig.cs:5-10 | HealthWeightConfig.cs:5-7
SNIPPET: (value object defaults as listed)
KIND: formula
AREA: health-metrics
```

```
RULE: Default QA thresholds: Coverage green=80, amber=50; Execution green=80, amber=50; PassRate green=90, amber=70; QualityHealthWeight default=20; QualitySubScoreWeights: Coverage=50, PassRate=50
SOURCE: src\Services\Fokus\Fokus.Domain\Settings\ValueObjects\QaHealthThresholdConfig.cs | QualitySubScoreWeightConfig.cs | AppSettings.cs:13
SNIPPET: (value object defaults as listed)
KIND: formula
AREA: health-metrics
```

```
RULE: QA quality sub-score = (coverageScore × coverageWeight + passRateScore × passRateWeight) / totalSubScoreWeight; totalWeight == 0 → 0
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\QaMetricsCalculator.cs:65-70
SNIPPET:
    var qualitySubScore = totalSubScoreWeight > 0
        ? (coverageScore * subScoreWeights.CoverageWeight + passRateScore * subScoreWeights.PassRateWeight) / totalSubScoreWeight
        : 0;
KIND: formula
AREA: health-metrics
```

---

### Sprint Summary Metrics (Delivery)

```
RULE: Completion rate = completedSp / activeSp × 100; activeSp == 0 → 0. Bugs are excluded from both numerator and denominator.
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\SprintSummaryCalculator.cs:254
SNIPPET: var completionRate = activeSp > 0 ? completedSp / activeSp * 100 : 0;
KIND: formula
AREA: analytics
```

```
RULE: ScopeDisruptionRate = addedSp / activeSp × 100 (non-bug added post-planning that entered cycle / all activated non-bug SP); BugDisruptionRate = bugAddedSp / activeSp × 100; DisruptionRate = scope + bug combined; activeSp == 0 → 0 for all
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\SprintSummaryCalculator.cs:255-258
SNIPPET:
    var scopeDisruptionRate = activeSp > 0 ? addedSp / activeSp * 100 : 0;
    var bugDisruptionRate = activeSp > 0 ? bugAddedSp / activeSp * 100 : 0;
    var disruptionRate = scopeDisruptionRate + bugDisruptionRate;
KIND: formula
AREA: analytics
```

```
RULE: CarryOverRate = carryOverSp / activeSp × 100 (feature-only, started but not completed); activeSp == 0 → 0
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\SprintSummaryCalculator.cs:258
SNIPPET: var carryOverRate = activeSp > 0 ? carryOverSp / activeSp * 100 : 0;
KIND: formula
AREA: analytics
```

```
RULE: A ticket "added" (scope disruption) requires BOTH AddedAt > planningCutoff (sprint.StartDate + planningWindowDays) AND isStarted; not started → not counted
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\TransitionAttributionChecker.cs:150-151
SNIPPET: membership.AddedAt > planningCutoff && isStarted
KIND: formula
AREA: analytics
```

```
RULE: Carry-over = started AND NOT completed in the same sprint (feature-only)
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\TransitionAttributionChecker.cs:182
SNIPPET: isStarted && !isCompleted
KIND: formula
AREA: analytics
```

```
RULE: PlanningWindowDays default = 2 (sprint.StartDate + 2 days = planning cutoff); range 0–7 enforced at API
SOURCE: src\Services\Fokus\Fokus.Domain\Settings\ValueObjects\SyncConfig.cs:6 | Fokus.API\Features\Settings\SaveSyncConfig\SaveSyncConfigCommand.cs:14-19
SNIPPET: public int PlanningWindowDays { get; set; } = 2;
KIND: formula
AREA: settings
```

```
RULE: Sparkline window = up to 4 sprints ending at and including the selected sprint
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\SprintSummaryCalculator.cs:66-69
SNIPPET: sortedWindow.Take(selectedIndex + 1).TakeLast(4).ToList()
KIND: formula
AREA: analytics
```

---

### Transition Attribution (Completion / Started)

```
RULE: Ticket "started" in sprint = has a transition to a stage at or after startIndex within [sprintStart, sprintEnd]; startIndex < 0 → never started; uses first qualifying transition
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\TransitionAttributionChecker.cs:88-111
SNIPPET: transitions.Where(t => t.Timestamp >= sprintStart && t.Timestamp <= sprintEnd && GetStageIndex(t.ToStatus, orderedStages) >= startIndex).OrderBy(t => t.Timestamp).FirstOrDefault()
KIND: formula
AREA: analytics
```

```
RULE: Ticket "completed" in sprint = has a transition to a stage at or after endIndex within [sprintStart, sprintEnd]; endIndex < 0 (no done statuses configured) → nothing completes
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\TransitionAttributionChecker.cs:118-141
SNIPPET: GetStageIndex(t.ToStatus, orderedStages) >= endIndex
KIND: formula
AREA: analytics
```

```
RULE: Start stage fallback: if CycleTimeStartStage is null, defaults to orderedStages[0] for attribution (wider than cycle time, which defaults to orderedStages[1]); if start stage not found, falls back to index 0
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\TransitionAttributionChecker.cs:27-47
SNIPPET:
    var startStage = settings.CycleTime.StartStage ?? (orderedStages.Count > 0 ? orderedStages[0] : null);
    if (startIndex < 0) startIndex = 0;
KIND: edge-case
AREA: analytics
```

```
RULE: End stage fallback: if CycleTimeEndStage null → first DoneStatus; if end stage not in orderedStages → fallback to first DoneStatus; no done statuses → endIndex = -1 (nothing completes)
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\TransitionAttributionChecker.cs:58-75
SNIPPET: return endIndex; // -1 if still not found
KIND: edge-case
AREA: analytics
```

```
RULE: orderedStages = WorkflowStages ++ DoneStatuses (concatenation, workflow stages first, done statuses appended)
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\TransitionAttributionChecker.cs:29-31
SNIPPET: orderedStages.AddRange(settings.WorkflowStages); orderedStages.AddRange(settings.DoneStatuses);
KIND: formula
AREA: analytics
```

```
RULE: Ticket "removed post-planning" = RemovedAt != null AND RemovedAt > planningCutoff AND has a qualifying start transition in [sprintStart, RemovedAt]
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\TransitionAttributionChecker.cs:160-175
SNIPPET:
    if (membership.RemovedAt == null || membership.RemovedAt <= planningCutoff) return false;
    return ticketTransitions.Any(t => t.Timestamp >= sprintStart && t.Timestamp <= membership.RemovedAt.Value && GetStageIndex(...) >= startIndex);
KIND: formula
AREA: analytics
```

---

### Cycle Time Calculator

```
RULE: Cycle time boundary fallback: CycleTimeStartStage null → orderedStages[1] (second stage) if ≥2 stages, else [0]; EndStage null → DoneStatuses[0]
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\CycleTimeCalculator.cs:199-203
SNIPPET:
    var startStage = settings.CycleTime.StartStage ?? (settings.WorkflowStages.Count >= 2 ? settings.WorkflowStages[1] : settings.WorkflowStages.FirstOrDefault() ?? string.Empty);
    var endStage = settings.CycleTime.EndStage ?? (settings.DoneStatuses.Count > 0 ? settings.DoneStatuses[0] : string.Empty);
KIND: edge-case
AREA: analytics
```

```
RULE: Cycle time uses first qualifying completion transition within the sprint window as completion timestamp (not latest); ticket already done before sprint window → skipped
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\CycleTimeCalculator.cs:253-260
SNIPPET:
    var doneTransition = transitions.Where(t => GetStageIndex(t.ToStatus, orderedStages) >= endIndex && t.Timestamp >= sprintStart && t.Timestamp <= sprintEnd).OrderBy(t => t.Timestamp).FirstOrDefault();
    if (doneTransition is null) continue; // ticket was already done before sprint window
KIND: formula
AREA: analytics
```

```
RULE: Cycle time entry time is clamped to sprintStart; exit time is clamped to sprintEnd; stages outside [startIdx, endIdx] are skipped; unknown stages (not in orderedStages) accumulated as "__Other__"
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\CycleTimeCalculator.cs:279-329
SNIPPET:
    var enterTime = transition.Timestamp < sprintStart ? sprintStart : transition.Timestamp;
    exitTime = nextTransition.Timestamp > sprintEnd ? sprintEnd : nextTransition.Timestamp;
KIND: formula
AREA: analytics
```

```
RULE: Ticket skipped from cycle time if it never entered the start stage (visitedStages does not contain startStage)
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\CycleTimeCalculator.cs:332-334
SNIPPET: if (!visitedStages.Contains(startStage)) continue;
KIND: guard
AREA: analytics
```

```
RULE: Rework count = number of times a ticket re-enters a previously visited stage within boundaries
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\CycleTimeCalculator.cs:302-305
SNIPPET: if (visitedStages.Contains(stage)) reworkCount++;
KIND: formula
AREA: analytics
```

```
RULE: Outlier = cycle time > median × 2 (P50 × 2)
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\CycleTimeCalculator.cs:390-391
SNIPPET: results.Count(r => r.CycleTimeDays > median * 2)
KIND: classification
AREA: analytics
```

```
RULE: Percentile computation uses linear interpolation: index = p × (n-1); lower = floor(index); fraction = index - lower; result = sorted[lower] + fraction × (sorted[upper] - sorted[lower]); n==1 → return single element
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\CycleTimeCalculator.cs:373-386
SNIPPET:
    var index = p * (sorted.Count - 1);
    return sorted[lower] + fraction * (sorted[upper] - sorted[lower]);
KIND: formula
AREA: analytics
```

```
RULE: Multi-sprint cycle time metric cards use average of per-sprint percentiles (not pooled percentile); delta is last vs second-to-last sprint
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\CycleTimeCalculator.cs:150-184
SNIPPET:
    var avgP50 = sprintMetrics.Average(m => m.P50);
    deltaP50 = last.P50 - prev.P50; // last vs one before it
KIND: formula
AREA: analytics
```

```
RULE: Stage funnel average duration = sum(stageDuration across all tickets) / ticketCount (not sum of averages); tickets with zero total cycle time are excluded
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\CycleTimeCalculator.cs:471-490
SNIPPET:
    var avg = stageSums.ContainsKey(stage) ? stageSums[stage] / ticketCount : 0m;
    total > 0 ? Math.Round(e.Avg / total * 100, 1) : 0
KIND: formula
AREA: analytics
```

---

### Bug Ratio

```
RULE: Bug classification = IssueType == "Bug" (exact string match, case-sensitive at point of use)
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\BugRatioCalculator.cs:334
SNIPPET: m.Ticket?.IssueType == "Bug"
KIND: classification
AREA: analytics
```

```
RULE: Bug ratio % = bugSp / (bugSp + nonBugSp) × 100 (ratio of totals, not average of per-sprint ratios); completedSp == 0 → 0
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\BugRatioCalculator.cs:48-49
SNIPPET:
    var teamRatio = totalCompletedSp > 0 ? totalBugSp / totalCompletedSp * 100 : 0m;
KIND: formula
AREA: analytics
```

```
RULE: Bug ratio alert fires when developer's bug ratio ≥ alertThreshold for consecutiveCount sprints in a row (most-recent descending); a sprint where completedSp == 0 immediately breaks the streak
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\BugRatioCalculator.cs:255-282
SNIPPET:
    if (completedSp == 0) break; // BR11: zero SP sprint resets streak
    if (ratio >= threshold) consecutiveAbove++;
    else break;
KIND: invariant
AREA: analytics
```

```
RULE: Default bug ratio config: AlertThreshold=50%, ConsecutiveSprintCount=2, Target=30%, DefaultSpPerBug=3; API enforces: threshold 0–100, consecutive 1–10, defaultSp 0–13
SOURCE: src\Services\Fokus\Fokus.Domain\Settings\ValueObjects\BugRatioConfig.cs | Fokus.API\Features\Settings\SaveBugRatioAlerts\SaveBugRatioAlertsCommand.cs:14-25
SNIPPET: AlertThreshold=50, ConsecutiveSprintCount=2, DefaultSpPerBug=3; InclusiveBetween(0,13)
KIND: invariant
AREA: settings
```

```
RULE: Issue type breakdown is ordered by TicketCount descending; SP totals use GetEffectiveSp(defaultSpPerBug)
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\BugRatioCalculator.cs:380-384
SNIPPET: .OrderByDescending(e => e.TicketCount).ToList()
KIND: formula
AREA: analytics
```

---

### Effective SP

```
RULE: Effective SP = StoryPoints if StoryPoints.HasValue && > 0; else if IssueType=="Bug" && defaultSpPerBug > 0 → defaultSpPerBug; else null (ticket has no effective SP and is excluded from SP-based computations)
SOURCE: src\Services\Fokus\Fokus.Domain\Sprint\Behaviors\SprintMembership.cs:11-19
SNIPPET:
    if (StoryPoints.HasValue && StoryPoints.Value > 0) return StoryPoints;
    if (Ticket?.IssueType == "Bug" && defaultSpPerBug > 0) return (decimal)defaultSpPerBug;
    return null;
KIND: formula
AREA: analytics
```

---

### Scope Change

```
RULE: CommittedSP (Total) = non-bug memberships where (WasCommitted OR AddedAt <= planningCutoff) AND (RemovedAt == null OR RemovedAt > planningCutoff); no excludedStatus filter applied
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\ScopeChangeCalculator.cs:261-266
SNIPPET:
    .Where(m => m.Ticket?.IssueType != "Bug" && (m.WasCommitted || m.AddedAt <= planningCutoff) && (m.RemovedAt == null || m.RemovedAt > planningCutoff))
KIND: formula
AREA: analytics
```

```
RULE: Mid-sprint addition classification: post-planning non-bug non-removed cycle-entered additions are "Priority Escalation" if CreatedDate < sprint.StartDate; else "Scope Injection"; Bugs are "Unplanned Bug" (bugs never reach this path in practice — bug filter before classify)
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\ScopeChangeCalculator.cs:321-331
SNIPPET:
    if (m.Ticket?.CreatedDate < sprint.StartDate) return "Priority Escalation";
    return "Scope Injection";
KIND: classification
AREA: analytics
```

```
RULE: Burnup day phase = "planning" for days 1–planningWindowDays; "execution" for remaining days
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\ScopeChangeCalculator.cs:632
SNIPPET: var phase = dayNumber <= planningWindowDays ? "planning" : "execution";
KIND: classification
AREA: analytics
```

```
RULE: Bug time-in-progress active statuses = middle stages of workflowStages (skip first and last); ≤2 stages → all stages; empty → default "In Progress"
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\ScopeChangeCalculator.cs:689-701
SNIPPET:
    if (workflowStages.Count <= 2) return new HashSet<string>(workflowStages);
    var middle = workflowStages.Skip(1).Take(workflowStages.Count - 2).ToList();
KIND: classification
AREA: analytics
```

```
RULE: DisruptionRate (ScopeChange view) = addedSp / activeSp × 100 (feature added post-planning, cycle-entered / feature activeSp); activeSp == 0 → 0; NetScopeChange = addedSp - removedSp
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\ScopeChangeCalculator.cs:268-269
SNIPPET:
    var netScopeChange = addedSp - removedSp;
    var disruptionRate = activeSp > 0 ? addedSp / activeSp * 100 : 0;
KIND: formula
AREA: analytics
```

---

### Carry-Over

```
RULE: Zombie ticket = a ticket appearing in 3 or more distinct sprints (across all synced sprints); IsZombie flag on carry-over ticket is set when sprintCount >= 3
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\CarryOverCalculator.cs:428-435
SNIPPET:
    .Where(x => x.SprintCount >= 3)
    ...
    sprintCount >= 3 // IsZombie
KIND: classification
AREA: analytics
```

```
RULE: Zombie trajectory is capped at last 10 sprint appearances
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\CarryOverCalculator.cs:589-590
SNIPPET: if (sprintAppearances.Count > 10) sprintAppearances = sprintAppearances.TakeLast(10).ToList();
KIND: edge-case
AREA: analytics
```

```
RULE: Carry-over destination categories: completed (done in next sprint), carried-again (started but not done), removed (RemovedAt != null in current sprint), dropped (in current sprint memberships but no qualifying transition)
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\CarryOverCalculator.cs:503-555
SNIPPET: (see bucket accumulation logic for completedCount, carriedAgainCount, removedCount, droppedCount)
KIND: classification
AREA: analytics
```

---

### QA Metrics

```
RULE: Coverage rate = coveredKeys / activeTicketKeys × 100; a ticket is covered if it has own non-cancelled TE via Tests link OR its parent has one (sub-task inheritance BR9); empty scope → 0
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\QaMetricsCalculator.cs:228-261
SNIPPET:
    var rate = (decimal)coveredKeys.Count / activeTicketKeys.Count * 100;
KIND: formula
AREA: health-metrics
```

```
RULE: Execution rate = executedKeys / coveredKeys × 100; a covered ticket is "executed" if any linked TE has at least one Pass or Fail run; empty covered → 0
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\QaMetricsCalculator.cs:267-289
SNIPPET:
    runs.Any(r => r.Status == TestRunStatus.Pass || r.Status == TestRunStatus.Fail)
    var rate = (decimal)executedKeys.Count / coveredKeys.Count * 100;
KIND: formula
AREA: health-metrics
```

```
RULE: Pass rate = passRuns / (passRuns + failRuns) × 100 across all non-cancelled TEs; only Pass and Fail statuses counted; totalCount == 0 → 0
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\QaMetricsCalculator.cs:295-307
SNIPPET:
    if (run.Status == TestRunStatus.Pass) { passCount++; totalCount++; }
    else if (run.Status == TestRunStatus.Fail) { totalCount++; }
    return totalCount > 0 ? Math.Round((decimal)passCount / totalCount * 100, 1) : 0;
KIND: formula
AREA: health-metrics
```

```
RULE: Bugs found = count of unique bug ticket keys linked via Blocks links from non-cancelled TEs where linked ticket IssueType == "Bug"
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\QaMetricsCalculator.cs:312-325
SNIPPET:
    foreach (var link in te.Links.Where(l => l.LinkType == TestExecutionLinkType.Blocks))
        if (link.Ticket?.IssueType == "Bug") bugKeys.Add(link.TicketKey);
KIND: formula
AREA: health-metrics
```

```
RULE: QA active scope (coverage denominator) = feature tickets (non-bug) that are non-removed, non-excludedStatus, have effective SP, AND have a qualifying start transition in the sprint window
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\QaMetricsCalculator.cs:148-171
SNIPPET:
    if (m.Ticket?.IssueType == "Bug") continue;
    if (!GetEffectiveSp(m, defaultSpPerBug).HasValue) continue;
    if (IsStartedInSprint(...)) result.Add(m.TicketId);
KIND: formula
AREA: health-metrics
```

```
RULE: Per-ticket test status derivation: failCount > 0 → "Failed"; passCount > 0 && todoCount == 0 && executingCount == 0 → "Passed"; todoCount > 0 || executingCount > 0 → "InProgress"; all aborted (TEs exist, no effective runs) → "NoTests"
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\EpicProgressAnalyzer.cs:489-497
SNIPPET:
    if (failCount > 0) testStatus = "Failed";
    else if (passCount > 0 && todoCount == 0 && executingCount == 0) testStatus = "Passed";
    else if (todoCount > 0 || executingCount > 0) testStatus = "InProgress";
    else testStatus = "NoTests";
KIND: classification
AREA: health-metrics
```

```
RULE: Pearson R for defect correlation is only computed when ≥ 6 complete (coverage, next-sprint bug ratio) pairs; < 6 → null
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\QaTrendsAnalyzer.cs:139
SNIPPET: if (n < 6) return null;
KIND: edge-case
AREA: analytics
```

```
RULE: QA trends require ≥ 2 sprints; < 2 → HasQaData=false with empty result sets
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\QaTrendsAnalyzer.cs:17-24
SNIPPET: if (sortedSprints.Count < 2) return new QaTrendsResultData(HasQaData: false, ...);
KIND: guard
AREA: analytics
```

---

### Developer Progress

```
RULE: Sprint totalDays = max(1, (EndDate.Date - StartDate.Date).Days + 1) (inclusive, minimum 1)
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\DeveloperProgressAnalyzer.cs:19
SNIPPET: var totalDays = Math.Max(1, (activeSprint.EndDate.Date - activeSprint.StartDate.Date).Days + 1);
KIND: formula
AREA: analytics
```

```
RULE: currentDay = clamp(UtcNow.Date - SprintStart.Date + 1, 1, totalDays)
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\DeveloperProgressAnalyzer.cs:22
SNIPPET: var currentDay = Math.Min(totalDays, Math.Max(1, (DateTime.UtcNow.Date - activeSprint.StartDate.Date).Days + 1));
KIND: formula
AREA: analytics
```

```
RULE: Grace period = first 2 days of sprint (currentDay <= 2); behind-pace detection and alerts suppressed during grace period
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\DeveloperProgressAnalyzer.cs:25
SNIPPET: var isGracePeriod = currentDay <= 2;
KIND: classification
AREA: analytics
```

```
RULE: Daily pace = assignedSp × (capacity / 100) / totalDays; isBehindPace = gap > dailyPace (more than one full day's work behind expected)
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\DeveloperProgressAnalyzer.cs:65-138
SNIPPET:
    var dailyPace = assignedSp * (capacity / 100m) / totalDays;
    if (gap > dailyPace) isBehindPace = true;
KIND: formula
AREA: analytics
```

```
RULE: Stall detection: ticket is stalled if started, not completed, and most-recent transition is > 2 business days ago (excludes transition day itself, counts Mon–Fri)
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\DeveloperProgressAnalyzer.cs:280-298
SNIPPET:
    var businessDays = CountBusinessDays(lastTransition, referenceDate);
    if (businessDays <= 2) continue;
KIND: classification
AREA: analytics
```

```
RULE: Alert direction priority: new-stall overrides gap direction; stall-resolved overrides; then worsening (gap increased) vs improving (gap decreased); stable (no change) → no alert
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\DeveloperProgressAnalyzer.cs:199-212
SNIPPET:
    if (hasNewStall) direction = "new-stall";
    else if (isStallResolved) direction = "stall-resolved";
    else if (gapDelta.Value > 0) direction = "worsening";
    else if (gapDelta.Value < 0) direction = "improving";
    else direction = "stable";
KIND: classification
AREA: analytics
```

```
RULE: Developer excluded from progress if capacity == 0 AND completedSp == 0 in that sprint (developer absent)
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\DeveloperDetailCalculator.cs:58-59
SNIPPET: if (capacityPercent == 0 && totalSp == 0) continue;
KIND: guard
AREA: analytics
```

---

### Rolling Averages

```
RULE: Developer throughput rolling average = 3-sprint window, backward from current (inclusive); sprints with capacity == 0 are skipped; < 3 qualifying sprints → null (no average shown)
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\DeveloperThroughputCalculator.cs:252-268
SNIPPET:
    for (var i = currentIndex; i >= 0 && qualifyingSpCompleted.Count < 3; i--)
        if (capacity == 0) continue;
    if (qualifyingSpCompleted.Count < 3) return null;
KIND: formula
AREA: analytics
```

```
RULE: Developer detail rolling average = 3-sprint window over all-types SP, excludes 0-capacity sprints; < 3 qualifying → null
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\DeveloperDetailCalculator.cs:352-375
SNIPPET:
    for (var i = currentIndexInDisplayed; i >= 0 && qualifyingTotals.Count < 3; i--)
        if (capacity == 0) continue;
    if (qualifyingTotals.Count < 3) return null;
KIND: formula
AREA: analytics
```

---

### Below-Median Streak (Developer Quality)

```
RULE: Below-median streak = consecutive sprints (backward from current) where developer coverage < team median; sprint with no QA data or developer has 0 stories breaks the streak; streak only surfaced when ≥ 2
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\DeveloperQualityScorer.cs:338-373
SNIPPET:
    if (sprintTEs.Count == 0) break;
    if (devStories == 0) break;
    if (devCoverage < median) streak++;
    else break;
    return streak >= 2 ? streak : null;
KIND: formula
AREA: health-metrics
```

```
RULE: Team median for below-median streak = arithmetic median across all filtered developers with stories > 0; even count → average of two middle values
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\DeveloperQualityScorer.cs:376-383
SNIPPET:
    return sorted.Count % 2 == 0 ? (sorted[mid-1] + sorted[mid]) / 2 : sorted[mid];
KIND: formula
AREA: health-metrics
```

---

### Epic Progress

```
RULE: Velocity = average completedSp per sprint over rolling 3-sprint window (most recent first, skip zero-progress sprints); projection = remainingWork / velocity; confidence = "low" when < 3 data points
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\EpicProgressAnalyzer.cs:149-173
SNIPPET:
    .Where(x => x.CompletedSp > 0) // BR8: skip zero-progress sprints
    .OrderByDescending(x => x.SprintStartDate).Take(3)
    velocity = sprintSpCompleted.Average(x => x.CompletedSp);
    projectedSprintsRemaining = remainingWork / velocity.Value;
    if (sprintSpCompleted.Count < 3) projectionConfidence = "low";
KIND: formula
AREA: analytics
```

```
RULE: SP imputation for unestimated remaining tickets = avgSp × remainingUnestimatedCount (average of estimated tickets in the same epic); applied only to remaining (not done) unestimated tickets; adjustedTotalSp = totalSp + imputedSp
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\EpicProgressAnalyzer.cs:81-91
SNIPPET:
    var avgSp = estimatedTickets.Average(t => GetEffectiveTicketSp(t, defaultSpPerBug)!.Value);
    imputedSp = remainingUnestimatedCount > 0 ? Math.Round(avgSp * remainingUnestimatedCount, 1) : 0m;
    adjustedTotalSp = Math.Round(totalSp + imputedSp.Value, 1);
KIND: formula
AREA: analytics
```

```
RULE: Average epic completion (summary) = weighted average by adjustedTotalSp when any epic has adjustedTotalSp; fallback to arithmetic average of ticketCompletionPercentage when all null
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\EpicProgressAnalyzer.cs:246-267
SNIPPET:
    if (totalAdjustedSp > 0) averageCompletionPct = Math.Round(weightedSum / totalAdjustedSp, 1);
    else if (activeEpics.Count > 0) averageCompletionPct = Math.Round(activeEpics.Average(e => e.TicketCompletionPercentage), 1);
KIND: formula
AREA: analytics
```

```
RULE: Epic sort order: active epics first (sorted by SP completion % ascending, null last), then completed epics alphabetically
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\EpicProgressAnalyzer.cs:228-238
SNIPPET:
    .OrderBy(e => e.SpCompletionPercentage.HasValue ? 0 : 1).ThenBy(e => e.SpCompletionPercentage ?? e.TicketCompletionPercentage)
    completedEpics.OrderBy(e => e.EpicName)
KIND: formula
AREA: analytics
```

---

### QA Workload

```
RULE: QA workload alert fires when a person's share of total terminal (Pass+Fail) runs exceeds 50% for 2+ consecutive sprints (most-recent first); sprint with 0 terminal runs breaks the streak
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\QaWorkloadAnalyzer.cs:399-435
SNIPPET:
    const int thresholdPercent = 50; const int minConsecutive = 2;
    if (totalRuns == 0) break;
    if (sharePercent > thresholdPercent) consecutive++;
    else break;
    return new WorkloadAlertData(consecutive >= minConsecutive, ...);
KIND: formula
AREA: analytics
```

```
RULE: Run attribution: run.ExecutedById takes priority over te.AssigneeId; null → Unassigned; Unassigned is excluded from workload results when sub-team filter is active
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\QaWorkloadAnalyzer.cs:149-153 | 286-287
SNIPPET:
    if (run.ExecutedById is not null) return run.ExecutedById;
    if (te.AssigneeId is not null) return te.AssigneeId;
    return null; // Unassigned
KIND: formula
AREA: analytics
```

---

### Invitation Domain

```
RULE: Invitation token = 32 random bytes, Base64URL-encoded (+ → -, / → _, = trimmed); expires 7 days from creation; initial status = "Pending"
SOURCE: src\Services\Fokus\Fokus.Domain\Auth\Behaviors\Invitation.cs:9-28
SNIPPET:
    var token = Convert.ToBase64String(tokenBytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    ExpiresAt = createdAt.AddDays(7);
    Status = "Pending"
KIND: formula
AREA: invitations
```

```
RULE: Invitation.Accept throws DomainException if status != "Pending"; throws DomainException if now > ExpiresAt; on success sets Status="Accepted", AcceptedAt=now, regenerates ConcurrencyToken
SOURCE: src\Services\Fokus\Fokus.Domain\Auth\Behaviors\Invitation.cs:30-40
SNIPPET:
    if (Status != "Pending") throw new DomainException("Invitation is not in Pending status.");
    if (now > ExpiresAt) throw new DomainException("Invitation has expired.");
KIND: invariant
AREA: invitations
```

```
RULE: Invitation.Revoke throws DomainException if status != "Pending"; on success sets Status="Revoked", regenerates ConcurrencyToken
SOURCE: src\Services\Fokus\Fokus.Domain\Auth\Behaviors\Invitation.cs:42-49
SNIPPET: if (Status != "Pending") throw new DomainException("Only pending invitations can be revoked.");
KIND: invariant
AREA: invitations
```

---

### Leaderboard Sort

```
RULE: Leaderboard sort = TotalSp descending, then DisplayName ascending (both single- and multi-sprint)
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\LeaderboardCalculator.cs:76-78 | 161-164
SNIPPET: .OrderByDescending(d => d.TotalSp).ThenBy(d => d.DisplayName)
KIND: formula
AREA: analytics
```

```
RULE: Sprint summary leaderboard sort = SpCompleted descending, then DisplayName ascending
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\SprintSummaryCalculator.cs:667-668
SNIPPET: .OrderByDescending(d => d.SpCompleted).ThenBy(d => d.DisplayName)
KIND: formula
AREA: analytics
```

---

### MetricCard Direction/Polarity

```
RULE: Direction = "up" if delta > 0; "down" if delta < 0; "flat" if delta == 0. Polarity (positive-up): up→positive, down→negative, flat→neutral. Polarity (positive-down): down→positive, up→negative, flat→neutral. String "neutral" input → always "neutral" regardless of delta sign.
SOURCE: src\Services\Fokus\Fokus.Domain\Analytics\MetricCardBuilder.cs:32-38
SNIPPET:
    var direction = delta.Value > 0 ? "up" : delta.Value < 0 ? "down" : "flat";
    "positive-up" => delta.Value > 0 ? "positive" : delta.Value < 0 ? "negative" : "neutral",
    "positive-down" => delta.Value < 0 ? "positive" : delta.Value > 0 ? "negative" : "neutral",
    _ => "neutral"
KIND: formula
AREA: analytics
```

---

### API Validators (Business-Meaningful Constraints)

```
RULE: Invitation role must be null, "Admin", or "Manager"; team role must be one of {"Developer","Tech Lead","Tester","PO"} (case-insensitive)
SOURCE: src\Services\Fokus\Fokus.API\Features\Auth\CreateInvitation\CreateInvitationCommand.cs:31-33 | Team\UpdateTeamConfig\UpdateTeamConfigCommand.cs:26-47
SNIPPET:
    .Must(r => r is null || r == "Admin" || r == "Manager")
    AllowedRoles = { "Developer", "Tech Lead", "Tester", "PO" }
KIND: validation
AREA: invitations
```

```
RULE: Health weights (Completion + Disruption + CarryOver) must sum to exactly 100
SOURCE: src\Services\Fokus\Fokus.API\Features\Settings\SaveHealthConfig\SaveHealthConfigCommand.cs:17-19
SNIPPET: .Must(w => w.Completion + w.Disruption + w.CarryOver == 100)
KIND: invariant
AREA: health-metrics
```

```
RULE: Completion amber < CompletionGreen (higher-is-better); Disruption amber > DisruptionGreen (lower-is-better); CarryOver amber > CarryOverGreen (lower-is-better); QA amber < QA green (all higher-is-better); QA sub-score weights each ≥ 1
SOURCE: src\Services\Fokus\Fokus.API\Features\Settings\SaveHealthConfig\SaveHealthConfigCommand.cs:22-78
SNIPPET:
    .LessThan(x => x.HealthThresholds.CompletionGreen)
    .GreaterThan(x => x.HealthThresholds.DisruptionGreen)
    .GreaterThanOrEqualTo(1) // sub-score weights
KIND: validation
AREA: health-metrics
```

```
RULE: Sprint start date must be strictly before end date; sprint name is required
SOURCE: src\Services\Fokus\Fokus.API\Features\Sprints\UpdateSprint\UpdateSprintCommand.cs:20-21
SNIPPET: .LessThan(x => x.EndDate)
KIND: validation
AREA: sprint
```

```
RULE: SprintId and Last (multi-sprint range) cannot both be provided to any analytics query
SOURCE: Multiple analytics query validators (GetBugRatioQuery, GetCycleTimeQuery, GetCarryOverQuery, GetLeaderboardQuery, GetScopeChangeQuery etc.)
SNIPPET: .Must(x => !(x.SprintId.HasValue && x.Last.HasValue))
KIND: guard
AREA: analytics
```

```
RULE: For GetCycleTime and GetCarryOver, Last=0 means "all sprints"; for GetBugRatio and GetLeaderboard, Last must be ≥ 1
SOURCE: GetCycleTimeQuery:20-22 | GetBugRatioQuery:19-22
SNIPPET:
    .GreaterThanOrEqualTo(0) // CycleTime/CarryOver: 0 = all
    .GreaterThanOrEqualTo(1) // BugRatio/Leaderboard: minimum 1
KIND: validation
AREA: analytics
```

```
RULE: DoneStatuses must have at least one entry; each entry must be non-empty; WorkflowStages entries must be non-empty (list itself may be empty)
SOURCE: SaveDoneStatusesCommand.cs:12-18 | SaveWorkflowStagesCommand.cs:9-14
SNIPPET:
    .NotEmpty() // list required
    RuleForEach(...).NotEmpty() // individual entries
KIND: validation
AREA: settings
```

```
RULE: Capacity percent must be 0–100 (both SetCapacityCommand and UpdateTeamConfigCommand)
SOURCE: SetCapacityCommand.cs:25-27 | UpdateTeamConfigCommand.cs:33-37
SNIPPET: .InclusiveBetween(0, 100)
KIND: validation
AREA: settings
```

---

## Files Examined

- `src\Services\Fokus\Fokus.Domain\Analytics\HealthScoreCalculator.cs`
- `src\Services\Fokus\Fokus.Domain\Analytics\TransitionAttributionChecker.cs`
- `src\Services\Fokus\Fokus.Domain\Analytics\CompletionChecker.cs`
- `src\Services\Fokus\Fokus.Domain\Analytics\MetricCardBuilder.cs`
- `src\Services\Fokus\Fokus.Domain\Analytics\SprintSummaryCalculator.cs`
- `src\Services\Fokus\Fokus.Domain\Analytics\BugRatioCalculator.cs`
- `src\Services\Fokus\Fokus.Domain\Analytics\CycleTimeCalculator.cs`
- `src\Services\Fokus\Fokus.Domain\Analytics\CarryOverCalculator.cs`
- `src\Services\Fokus\Fokus.Domain\Analytics\ScopeChangeCalculator.cs`
- `src\Services\Fokus\Fokus.Domain\Analytics\QaMetricsCalculator.cs`
- `src\Services\Fokus\Fokus.Domain\Analytics\QaTrendsAnalyzer.cs`
- `src\Services\Fokus\Fokus.Domain\Analytics\QaWorkloadAnalyzer.cs`
- `src\Services\Fokus\Fokus.Domain\Analytics\DeveloperQualityScorer.cs`
- `src\Services\Fokus\Fokus.Domain\Analytics\DeveloperProgressAnalyzer.cs`
- `src\Services\Fokus\Fokus.Domain\Analytics\DeveloperThroughputCalculator.cs`
- `src\Services\Fokus\Fokus.Domain\Analytics\DeveloperDetailCalculator.cs`
- `src\Services\Fokus\Fokus.Domain\Analytics\EpicProgressAnalyzer.cs`
- `src\Services\Fokus\Fokus.Domain\Analytics\LeaderboardCalculator.cs`
- `src\Services\Fokus\Fokus.Domain\Sprint\Behaviors\Sprint.cs`
- `src\Services\Fokus\Fokus.Domain\Sprint\Behaviors\SprintMembership.cs`
- `src\Services\Fokus\Fokus.Domain\Auth\Behaviors\Invitation.cs`
- `src\Services\Fokus\Fokus.Domain\Settings\AppSettings.cs`
- `src\Services\Fokus\Fokus.Domain\Settings\ValueObjects\HealthThresholdConfig.cs`
- `src\Services\Fokus\Fokus.Domain\Settings\ValueObjects\HealthWeightConfig.cs`
- `src\Services\Fokus\Fokus.Domain\Settings\ValueObjects\QaHealthThresholdConfig.cs`
- `src\Services\Fokus\Fokus.Domain\Settings\ValueObjects\QualitySubScoreWeightConfig.cs`
- `src\Services\Fokus\Fokus.Domain\Settings\ValueObjects\BugRatioConfig.cs`
- `src\Services\Fokus\Fokus.Domain\Settings\ValueObjects\CycleTimeConfig.cs`
- `src\Services\Fokus\Fokus.Domain\Settings\ValueObjects\SyncConfig.cs`
- All API validator files listed by grep (CreateInvitation, SetCapacity, SaveHealthConfig, SaveBugRatioAlerts, SaveSyncConfig, SaveBoard, SaveCycleTimeBoundaries, SaveDoneStatuses, SaveWorkflowStages, SaveAnalyticsTargets, UpdateTeamConfig, UpdateSprint, SyncSprints, GetBugRatio, GetCycleTime, GetCarryOver, GetLeaderboard, GetSprintSummary)

---

## Suspicious / Inconsistent Areas (Potential Bugs — Flagged, Not Fixed)

1. **`ExcludedFromScopeStatuses` filter missing from `CarryOverCalculator.ComputeCarryOverMetrics` activeSp accumulation for excluded tickets:** The active SP sum uses `!IsExcluded(m.FinalStatus, excludedStatuses)` inside the `if (isStarted)` block, but carry-over count (`carryOverCount++`) is incremented regardless of excludedStatus. A ticket in an excluded status can inflate the carry-over ticket count while not contributing to `carryOverSp` — count and SP will disagree for those tickets.
   Source: `CarryOverCalculator.cs:279-290`

2. **`TransitionAttributionChecker.ResolveStartIndex` vs `CycleTimeCalculator.ResolveBoundaries` use different fallback defaults for start stage:** Attribution uses `orderedStages[0]` (first stage) as fallback; CycleTimeCalculator uses `workflowStages[1]` (second stage) if available. These diverge silently when `CycleTime.StartStage` is null — the cycle time start is narrower than the attribution start. This is intentional per spec comment "BR19 divergence" but creates a situation where a ticket can be "started" for attribution/completion rate purposes but not contribute to cycle time, leading to metrics that appear inconsistent to users.
   Source: `TransitionAttributionChecker.cs:34-35` vs `CycleTimeCalculator.cs:199-200`

3. **`HealthScoreCalculator.ScoreLowerIsBetter` has an unreachable guard `if (cap <= amber) return 0`:** `cap = amber * 2`, so `cap <= amber` is only true when `amber <= 0`. If `amber == 0`, the formula divides by zero (`(cap - amber) = 0`). However, at that point `cap == 0` too, so `(value - amber) / (cap - amber)` would be `0/0`. The guard `if (cap <= amber)` fires first and returns 0, but this edge case means a configured amber=0 for a lower-is-better metric silently returns 0 for any value above green — the score is always 0 for the "above amber" region.
   Source: `HealthScoreCalculator.cs:39-41`

4. **`DeveloperProgressAnalyzer.BuildStalledTickets` passes `statusTransitions` (the full list) directly to `IsStartedInSprint` and `IsCompletedInSprint`** rather than pre-filtered per-ticket transitions. This is functionally correct because those methods filter by `t.TicketId`, but it is an O(n×m) pattern that differs from every other call site which pre-groups by ticketId first.
   Source: `DeveloperProgressAnalyzer.cs:263-275`

5. **`EpicProgressAnalyzer` activity date derivation is guarded by `startIndex >= 0`**, but `startIndex` can legitimately be 0 (the first stage) and still be valid — the guard correctly gates on `< 0` meaning "no usable stage config". However, the guard is `if (startIndex >= 0)` meaning it processes when startIndex is 0 or positive, which is correct. No bug, but the comment says "GetStageIndex returns -1 for unrecognised statuses, so -1 >= -1 would match noise transitions" — this comment is inaccurate because the guard would be `if (startIndex >= -1)` if that were true; the actual guard `>= 0` is correct.
   Source: `EpicProgressAnalyzer.cs:106`

6. **`QaWorkloadAnalyzer` hard-codes workload balance thresholds (50%, 2 consecutive sprints)** as local constants rather than reading from `AppSettings`, unlike the analogous `BugRatioCalculator` alert which reads `settings.BugRatio.AlertThreshold` and `settings.BugRatio.ConsecutiveSprintCount`. These QA workload thresholds are not user-configurable.
   Source: `QaWorkloadAnalyzer.cs:399-400`

================================================================================

The agent has completed its analysis. Here's a summary of what was delivered:

**50 business rules extracted** across all priority areas:

- **Health scoring** (7 rules): both interpolation formulas, RAG thresholds, composite weighted average with optional QA sub-score, all default threshold values
- **Delivery metrics** (8 rules): completion rate, scope/bug disruption rates, carry-over rate, planning cutoff logic, sparkline window size, "added" definition requiring both post-planning AND started
- **Transition attribution** (6 rules): started/completed definitions, orderedStages construction, fallback chains for start/end stage, removed-post-planning triple condition, carry-over = started AND NOT completed
- **Cycle time** (9 rules): boundary fallbacks (importantly: differs from attribution fallback), completion timestamp selection, time clamping, rework counting, outlier = P50×2, linear interpolation percentile, stage funnel averaging, multi-sprint uses average-of-percentiles not pooled percentile
- **Bug ratio** (5 rules): ratio-of-totals formula, alert streak with zero-SP break condition, all default config values and their API bounds
- **Effective SP** (1 rule): the gate formula used everywhere
- **Scope change** (5 rules): committedSP definition, classification logic (Priority Escalation vs Scope Injection), burnup phase labeling, active statuses for bug time-in-progress, disruption/net formulas
- **QA metrics** (8 rules): coverage/execution/pass rate formulas, sub-task inheritance, test status derivation, Pearson R minimum sample, QaTrends minimum sprint guard
- **Developer progress** (6 rules): totalDays formula, grace period, daily pace, behind-pace threshold, stall definition (2+ business days), alert direction priority chain
- **Carry-over** (3 rules): zombie threshold (≥3 sprints), trajectory cap (10), destination buckets
- **Epic progress** (4 rules): velocity rolling window, SP imputation, weighted average completion, sort order
- **Invitations** (3 rules): token generation, Accept guards, Revoke guard
- **API validators** (6 rules): role enumerations, health weights must sum to 100, amber/green ordering direction rules, sprint date ordering, SprintId+Last mutual exclusion, Last=0 semantics

**6 suspicious areas flagged**, with the most significant being the carry-over count/SP disagreement for excluded-status tickets, and the intentional-but-silent divergence between attribution start stage (orderedStages[0]) and cycle time start stage (orderedStages[1]) when no explicit stage is configured.

================================================================================

The summary above is a recap I generated after the agent returned its findings. The full raw output — all 50 rules in the structured block format, plus the 6 flagged suspicious areas, plus the file list — is in the agent's message immediately above this one.

Is there anything you'd like me to do with these findings? For example:
- Feed them into a spec or architecture document
- Cross-reference against a product spec to find gaps
- Use them as input for a `/nexus:architect` or `/nexus:create-feature-spec` session
- Identify which rules lack test coverage