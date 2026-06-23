# Cycle Time

Cycle-time analytics — how long work items take from start to completion, with per-sprint and
aggregate breakdowns. Computed by `CycleTimeAnalyzer`.

## Rules

Rules below are **verified** via the Mine→Verify pass (automated harness, 2026 — 59 rules confirmed).

- BR-1: ResolveBoundaries constructs orderedStages by appending all settings.WorkflowStages first, then all settings.DoneStatuses in that order. Done statuses always appear after workflow stages in the resulting list.
- BR-2: ResolveBoundaries selects startStage as settings.CycleTime.StartStage when non-null; otherwise it falls back to settings.WorkflowStages[1] when WorkflowStages.Count >= 2, then to settings.WorkflowStages.FirstOrDefault() when the count is 1, and finally to string.Empty when WorkflowStages is empty.
- BR-3: ResolveBoundaries selects endStage as settings.CycleTime.EndStage when non-null; otherwise it falls back to settings.DoneStatuses[0] when DoneStatuses.Count > 0, and to string.Empty when DoneStatuses is empty.
- BR-4: A SprintMembership is excluded from cycle-time computation when its RemovedAt property is non-null, regardless of any completion transitions.
- BR-5: After the RemovedAt guard, a membership is included only if TransitionAttributionChecker.IsCompletedInSprint returns true for that ticket within the sprint window [sprintStart, sprintEnd] using orderedStages and endIndex. The logic of that check is delegated to TransitionAttributionChecker and is not encoded in ComputeTicketCycleTime.
- BR-6: When completedMemberships is empty after filtering, ComputeTicketCycleTime returns an empty list immediately without proceeding to transition analysis.
- BR-7: All transitions for qualifying tickets are sorted ascending by Timestamp before per-ticket stage-duration accumulation begins. The sort is a stable LINQ OrderBy with no secondary key; ties preserve original enumeration order.
- BR-8: The completion transition (doneTransition) for a ticket is the earliest transition — by ascending Timestamp — whose ToStatus maps to a stage index >= endIndex in orderedStages AND whose Timestamp falls within [sprintStart, sprintEnd] inclusive on both ends. If no such transition exists the ticket is skipped.
- BR-9: The ticket's completionDate is exactly the Timestamp of its doneTransition.
- BR-10: When either startIdx or endIdx is negative (i.e., startStage or endStage is not found in orderedStages), all transitions for that ticket are skipped via continue inside the per-transition loop — no stage durations are accumulated for any transition.
- BR-11: A transition whose ToStatus is not present in orderedStages (stageIdx == -1) is accumulated under the key "__Other__" rather than being dropped. Entry time is clamped to sprintStart if the transition timestamp precedes it; exit time is the next transition's Timestamp clamped to sprintEnd, or sprintEnd if there is no next transition. Duration is added to "__Other__" only when exitOther > enterOther.
- BR-12: Transitions whose stageIdx in orderedStages is strictly less than startIdx or strictly greater than endIdx are skipped; only stages within the inclusive boundary [startIdx, endIdx] are measured.
- BR-13: Rework is counted each time a ticket transitions to a stage already present in visitedStages: reworkCount increments by 1 per re-entry. The stage is unconditionally added to visitedStages after the check, so the first visit adds it without incrementing.
- BR-14: The effective entry time for an in-boundary stage is transition.Timestamp clamped to sprintStart: if transition.Timestamp < sprintStart the entry time is sprintStart, otherwise it is transition.Timestamp.
- BR-15: The exit time for a stage is the immediately following transition's Timestamp clamped to sprintEnd (using sprintEnd when that Timestamp exceeds it). When no subsequent transition exists — the ticket is still in the stage at sprint end — the exit time is sprintEnd.
- BR-16: A stage interval is discarded when exitTime <= enterTime; only strictly positive durations are accumulated into stageDurations.
- BR-17: Stage duration is computed as (decimal)(exitTime - enterTime).TotalDays and added to the existing value for that stage key, accumulating additively across multiple visits to the same stage within one ticket.
- BR-18: A ticket is excluded from results if visitedStages does not contain startStage after the full transition loop — that is, if the ticket never entered the configured start stage.
- BR-19: A ticket is excluded from results when the sum of all stageDurations values is <= 0 after processing all transitions.
- BR-20: The CycleTimeDays value stored on a TicketCycleResult is Math.Round(totalCycleTime, 2) — the total stage duration rounded to 2 decimal places.
- BR-21: When membership.Ticket is null, TicketSummary falls back to the raw ticketId string and IssueType falls back to the literal string "Unknown". AssigneeId is null in that case.
- BR-22: ComputePercentiles returns PercentileSet(0, 0, 0, 0) — all four percentiles zero — when the input list is empty.
- BR-23: ComputePercentiles sorts the input ascending and computes four percentile ranks: P50, P75, P85, and P90.
- BR-24: The Percentile function returns the single element directly — without any interpolation — when the sorted list contains exactly one element.
- BR-25: Percentile uses a zero-based linear-interpolation formula: index = p * (count - 1). When floor(index) == ceiling(index) (index is an integer), the element at that index is returned directly. Otherwise, interpolation is applied: sorted[lower] + fraction * (sorted[upper] - sorted[lower]) where fraction = (decimal)(index - lower).
- BR-26: A ticket is classified as an outlier when its CycleTimeDays is strictly greater than median * 2 (i.e., more than double the median). A value exactly equal to double the median is not an outlier.
- BR-27: In BuildMetricCards, each delta is computed as current minus prior (e.g., p50 - prior.PriorP50.Value). A delta is null when the prior tuple is null or when the specific prior field within it is null.
- BR-28: BuildMetricCards produces four cards in this order: "Median Cycle Time" (polarity "positive-down"), "P85 Cycle Time" (polarity "positive-down"), "Throughput" (polarity "positive-up"), "Outliers" (polarity "positive-down"). In the single-sprint path, P50 and P85 display values are formatted as "{Math.Round(value, 1)} days"; Throughput and Outliers display as raw integers without rounding in their label strings.
- BR-29: When ticketResults.Count == 0 for the target sprint, ComputeSingleSprint returns immediately with metric cards built from all-zero arguments and no prior, and empty collections for scatter plot, stage funnel, issue type breakdown, developer breakdown, and outlier table.
- BR-30: Prior-sprint delta values are computed only when priorSprint is non-null AND priorResults.Count > 0. When priorResults is empty, all four prior fields (priorP50, priorP85, priorThroughput, priorOutlierCount) remain null and no deltas are passed to BuildMetricCards.
- BR-31: In ComputeMultiSprint, sprints are sorted ascending by StartDate before any per-sprint computation, trend construction, or delta calculation. This ordering governs the indices used for last-vs-prev delta comparisons.
- BR-32: Multi-sprint trend entries carry P85 rounded to 1 decimal place, P50 rounded to 1 decimal place, and the raw ticket count (not rounded).
- BR-33: Multi-sprint sprint summary entries carry median rounded to 1 decimal place, P85 rounded to 1 decimal place, raw ticket count, and a raw outlierCount integer. The outlier count is computed using CountOutliers against that sprint's own P50 (median).
- BR-34: Multi-sprint metric cards are null (not an empty list) when perSprintResults.Count == 0.
- BR-35: Multi-sprint metric card values are computed as arithmetic averages across all sprints (average-of-per-sprint-values, not a pooled ratio of totals): avgP50 = Average of per-sprint P50, avgP85 = Average of per-sprint P85, avgThroughput = (decimal)Average of per-sprint ticket counts, avgOutliers = (decimal)Average of per-sprint outlier counts. Sprints with zero results contribute P50=0, P85=0, Throughput=0, OutlierCount=0.
- BR-36: Multi-sprint metric card deltas are computed only when sprintMetrics.Count >= 2. Delta = sprintMetrics[^1] value minus sprintMetrics[^2] value (most recent sprint minus the one immediately before it). When fewer than 2 sprints exist, all deltas remain null.
- BR-37: Multi-sprint metric card display labels for avgP50, avgP85, avgThroughput, and avgOutliers are all formatted with Math.Round(value, 1). Throughput and Outlier deltas are cast from nullable int to decimal before being passed to MetricCardBuilder.Build.
- BR-38: BuildStageFunnel returns an empty list when results is empty, or when startStage or endStage is not found in orderedStages (either index < 0), or when startIdx > endIdx.
- BR-39: BuildStageFunnel defines boundaryStages as the slice of orderedStages from startIdx to endIdx inclusive: orderedStages.Skip(startIdx).Take(endIdx - startIdx + 1).
- BR-40: In BuildStageFunnel, a StageDurations key is accumulated into stageSums only when the key is present in configuredWorkflowStages OR in orderedStages, AND the key is also in boundaryStages. Keys that fail both containment checks (neither in configuredWorkflowStages nor in orderedStages) are accumulated into otherSum instead.
- BR-41: The per-stage average in BuildStageFunnel is stageSums[stage] / ticketCount where ticketCount = results.Count (ratio of total accumulated time to total ticket count, not an average of per-ticket averages). Stages absent from stageSums contribute an average of 0.
- BR-42: An "Other" funnel entry is appended after all boundary stages only when otherSum > 0. Its average is otherSum / ticketCount.
- BR-43: Each funnel entry's percentage is Math.Round(avg / total * 100, 1) where total is the sum of all funnel entry averages including "Other". When total == 0, the percentage is 0. AverageDurationDays is rounded to 2 decimal places.
- BR-44: BuildMultiSprintStageFunnel computes per-sprint funnels via BuildStageFunnel and discards sprints whose funnel is empty (Count == 0). If no sprint funnel survives, it returns an empty list.
- BR-45: In BuildMultiSprintStageFunnel, stage output order follows boundaryStages (the orderedStages slice from startIdx to endIdx inclusive). Stages present in allStageNames but absent from boundaryStages are omitted. "Other" is appended last when present in any sprint funnel.
- BR-46: In BuildMultiSprintStageFunnel, the average for each stage is an average-of-averages: only sprints whose per-sprint funnel contains an entry for that stage participate (sprintsWithStage). Sprints missing the stage are excluded from both numerator and denominator. When no sprint contains the stage, the average is 0.
- BR-47: BuildIssueTypeBreakdown groups tickets by IssueType using default C# string equality (ordinal case-sensitive), computes P50 and P85 per group each rounded to 1 decimal place, and orders output ascending by IssueType string.
- BR-48: BuildDeveloperBreakdown excludes tickets with a null AssigneeId; only assigned tickets contribute to any developer group.
- BR-49: Developer lookup for display name and avatar URL uses sprint.Memberships.FirstOrDefault(m => m.Ticket?.AssigneeId == group.Key). Only the first matching membership's Ticket.Assignee is used; if none matches, developer is null.
- BR-50: In BuildDeveloperBreakdown, the dominant stage is the boundary-stage key with the highest accumulated StageDurations value across all of that developer's tickets (using MaxBy). When no boundary-stage durations exist, dominantStage defaults to startStage.
- BR-51: Developer DisplayName falls back to group.Key (the AssigneeId string) when the developer entity cannot be resolved from sprint memberships.
- BR-52: Developer P50 and P85 in BuildDeveloperBreakdown are each rounded to 1 decimal place. The final list is ordered ascending by DisplayName (which may be the AssigneeId string when the developer entity is unavailable).
- BR-53: BuildOutlierTable filters to tickets with CycleTimeDays > median * 2 (same strict-greater-than threshold as CountOutliers) and orders them descending by CycleTimeDays.
- BR-54: BuildStageBreakdown returns all stageDurations entries rounded to 2 decimal places (unfiltered, in dictionary enumeration order) when startStage or endStage is not found in orderedStages (startIdx < 0 OR endIdx < 0). When both are found, only entries whose key is in the inclusive boundary range [startIdx, endIdx] are returned, ordered ascending by their index in orderedStages, each rounded to 2 decimal places.
- BR-55: FilterMemberships applies the sub-team filter only when subTeam is not null, empty, or whitespace. The comparison is m.Ticket?.Assignee?.SubTeam == subTeam — exact ordinal case-sensitive equality.
- BR-56: FilterMemberships applies the excluded-developer filter only when excludedDeveloperIds is non-null and has at least one entry. A membership passes when m.Ticket?.AssigneeId is null (unassigned tickets are never excluded) OR the AssigneeId is not in excludedDeveloperIds.
- BR-57: BuildScatterPlot includes one point per TicketCycleResult with no additional filtering beyond what ComputeTicketCycleTime already applied; each point carries a per-ticket BuildStageBreakdown result.
- BR-58: In ComputeMultiSprint, the per-sprint outlier count used for sprint summaries and for metric-card sprintMetrics is computed via CountOutliers(pair.Results, p.P50) — using that specific sprint's own P50, not a global median across all sprints.
- BR-59: In BuildMultiSprintStageFunnel, multi-sprint funnel percentage and AverageDurationDays rounding follow the same rules as the single-sprint funnel: AverageDurationDays is rounded to 2 decimal places and percentage is Math.Round(avg / total * 100, 1) with 0 when total == 0.
## Key Files

- `src/Services/Fokus/Fokus.Domain/Analytics/CycleTimeAnalyzer.cs` — production implementation
- `src/Services/Fokus/Fokus.Domain/Analytics/CycleTimeResult.cs` — result DTO
- `src/Services/Fokus/Fokus.Domain.Tests/Analytics/CycleTimeAnalyzerTests.cs` — example-based tests (pending Cover)
- `src/Services/Fokus/Fokus.Domain.Tests/Analytics/CycleTimeAnalyzerPropertyTests.cs` — property-based tests (pending Cover)

## Edge Cases

_Populated by the harness Discover stage (deferred). None recorded yet._

## Relationships

- `CycleTimeAnalyzer` → `CycleTimeResult`: shapes the computed output.

## Source

Cycle-time analytics in `CycleTimeAnalyzer`. Rule verification: Mine→Verify pass (automated harness)
— see the status footer for the latest run.

---
<!-- status: mutation-gated — 100% reachable kill, floor 75 cleared, 0 candidate bugs (2026-06-21) -->
<!-- mutation-gated: true -->
<!-- last-stryker-run: 2026-06-21 — 100% reachable kill, floor 75 cleared, 0 candidate bugs -->
