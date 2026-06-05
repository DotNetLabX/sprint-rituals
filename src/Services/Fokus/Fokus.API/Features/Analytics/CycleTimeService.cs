namespace Fokus.API.Features.Analytics;

// --- Shared types ---

public record CycleTimeSprintInfo(int Id, string Name, DateTime StartDate, DateTime EndDate);

public record CycleTimeMetricCard(
    string Name,
    decimal Value,
    string DisplayValue,
    decimal? Delta,
    string? DeltaDirection,
    string? DeltaPolarity);

public record StageBreakdownEntry(string StageName, decimal DurationDays);

public record CycleTimeScatterPoint(
    string TicketKey,
    string TicketSummary,
    string IssueType,
    decimal CycleTimeDays,
    DateTime CompletionDate,
    List<StageBreakdownEntry> StageBreakdown,
    int ReworkCount);

public record StageFunnelEntry(string StageName, decimal AverageDurationDays, decimal Percentage);

public record CycleTimeIssueTypeEntry(
    string IssueType,
    int TicketCount,
    decimal MedianCycleTime,
    decimal P85CycleTime);

public record CycleTimeDeveloperEntry(
    string DisplayName,
    string? AvatarUrl,
    string? SubTeam,
    int TicketsCompleted,
    decimal MedianCycleTime,
    decimal P85CycleTime,
    string DominantStage);

public record CycleTimeOutlierEntry(
    string TicketKey,
    string TicketSummary,
    string IssueType,
    decimal CycleTimeDays,
    List<StageBreakdownEntry> StageBreakdown,
    int ReworkCount);

public record CycleTimeBoundaries(string StartStage, string EndStage);

// --- Single-sprint response ---

public record CycleTimeSingleSprintResponse(
    CycleTimeSprintInfo? Sprint,
    List<CycleTimeMetricCard>? MetricCards,
    List<CycleTimeScatterPoint> ScatterPlot,
    List<StageFunnelEntry> StageFunnel,
    List<CycleTimeIssueTypeEntry> IssueTypeBreakdown,
    List<CycleTimeDeveloperEntry> DeveloperBreakdown,
    List<CycleTimeOutlierEntry> Outliers,
    CycleTimeBoundaries Boundaries);

// --- Multi-sprint response ---

public record CycleTimeTrendEntry(
    int SprintId,
    string SprintName,
    decimal P85CycleTime,
    decimal MedianCycleTime,
    int TicketsCompleted);

public record CycleTimeSprintSummaryEntry(
    int SprintId,
    string SprintName,
    DateTime StartDate,
    int TicketsCompleted,
    decimal MedianCycleTime,
    decimal P85CycleTime,
    int OutlierCount);

public record CycleTimeMultiSprintResponse(
    List<CycleTimeSprintInfo> Sprints,
    List<CycleTimeMetricCard>? MetricCards,
    List<CycleTimeTrendEntry> Trend,
    List<StageFunnelEntry> StageFunnel,
    List<CycleTimeSprintSummaryEntry> SprintSummaries,
    CycleTimeBoundaries Boundaries);

// --- Wrapper ---

public record CycleTimeResponse(
    string Mode,
    CycleTimeSingleSprintResponse? SingleSprint,
    CycleTimeMultiSprintResponse? MultiSprint);

// --- Service ---

public class CycleTimeService
{
    // Per-ticket computed result used internally
    private record TicketCycleResult(
        string TicketKey,
        string TicketSummary,
        string IssueType,
        string? AssigneeId,
        decimal CycleTimeDays,
        DateTime CompletionDate,
        Dictionary<string, decimal> StageDurations,
        int ReworkCount);

    public CycleTimeSingleSprintResponse ComputeSingleSprint(
        Sprint targetSprint,
        Sprint? priorSprint,
        List<StatusTransition> statusTransitions,
        AppSettings settings,
        string? subTeam,
        HashSet<string>? excludedDeveloperIds = null)
    {
        var (startStage, endStage, orderedStages) = ResolveBoundaries(settings);
        var endIndex = TransitionAttributionChecker.GetStageIndex(endStage, orderedStages);
        var boundaries = new CycleTimeBoundaries(startStage, endStage);

        var memberships = FilterMemberships(targetSprint.Memberships, subTeam, excludedDeveloperIds);

        var ticketResults = ComputeTicketCycleTime(
            memberships, statusTransitions,
            orderedStages, endIndex, startStage, endStage,
            targetSprint.StartDate, targetSprint.EndDate);

        var sprintInfo = new CycleTimeSprintInfo(
            targetSprint.Id, targetSprint.Name,
            targetSprint.StartDate, targetSprint.EndDate);

        if (ticketResults.Count == 0)
        {
            var emptyCards = BuildMetricCards(0, 0, 0, 0, null);
            return new CycleTimeSingleSprintResponse(
                sprintInfo, emptyCards, [], [], [], [], [], boundaries);
        }

        var cycleTimes = ticketResults.Select(r => r.CycleTimeDays).ToList();
        var percentiles = ComputePercentiles(cycleTimes);

        // Prior sprint metrics for deltas
        decimal? priorP50 = null, priorP85 = null;
        int? priorThroughput = null, priorOutlierCount = null;

        if (priorSprint is not null)
        {
            var priorMemberships = FilterMemberships(priorSprint.Memberships, subTeam, excludedDeveloperIds);
            // Status transitions passed in include both target and prior sprint tickets
            var priorResults = ComputeTicketCycleTime(
                priorMemberships, statusTransitions,
                orderedStages, endIndex, startStage, endStage,
                priorSprint.StartDate, priorSprint.EndDate);

            if (priorResults.Count > 0)
            {
                var priorTimes = priorResults.Select(r => r.CycleTimeDays).ToList();
                var priorPercentiles = ComputePercentiles(priorTimes);
                priorP50 = priorPercentiles.P50;
                priorP85 = priorPercentiles.P85;
                priorThroughput = priorResults.Count;
                priorOutlierCount = CountOutliers(priorResults, priorPercentiles.P50);
            }
        }

        var outlierCount = CountOutliers(ticketResults, percentiles.P50);
        var metricCards = BuildMetricCards(
            percentiles.P50, percentiles.P85, ticketResults.Count, outlierCount,
            priorP50.HasValue ? (priorP50, priorP85, priorThroughput, priorOutlierCount) : null);

        var scatterPlot = BuildScatterPlot(ticketResults, orderedStages, startStage, endStage);
        var stageFunnel = BuildStageFunnel(ticketResults, orderedStages, startStage, endStage, settings.WorkflowStages);
        var issueTypeBreakdown = BuildIssueTypeBreakdown(ticketResults);
        var developerBreakdown = BuildDeveloperBreakdown(ticketResults, targetSprint, orderedStages, startStage, endStage);
        var outliers = BuildOutlierTable(ticketResults, percentiles.P50, orderedStages, startStage, endStage);

        return new CycleTimeSingleSprintResponse(
            sprintInfo, metricCards, scatterPlot, stageFunnel,
            issueTypeBreakdown, developerBreakdown, outliers, boundaries);
    }

    public CycleTimeMultiSprintResponse ComputeMultiSprint(
        List<Sprint> sprints,
        List<StatusTransition> statusTransitions,
        AppSettings settings,
        string? subTeam,
        HashSet<string>? excludedDeveloperIds = null)
    {
        var (startStage, endStage, orderedStages) = ResolveBoundaries(settings);
        var endIndex = TransitionAttributionChecker.GetStageIndex(endStage, orderedStages);
        var boundaries = new CycleTimeBoundaries(startStage, endStage);

        var sortedSprints = sprints.OrderBy(s => s.StartDate).ToList();

        var sprintInfos = sortedSprints
            .Select(s => new CycleTimeSprintInfo(s.Id, s.Name, s.StartDate, s.EndDate))
            .ToList();

        // Per-sprint computation
        var perSprintResults = new List<(Sprint Sprint, List<TicketCycleResult> Results)>();

        foreach (var sprint in sortedSprints)
        {
            var memberships = FilterMemberships(sprint.Memberships, subTeam, excludedDeveloperIds);
            var results = ComputeTicketCycleTime(
                memberships, statusTransitions,
                orderedStages, endIndex, startStage, endStage,
                sprint.StartDate, sprint.EndDate);
            perSprintResults.Add((sprint, results));
        }

        // Trend entries
        var trend = perSprintResults.Select(pair =>
        {
            var times = pair.Results.Select(r => r.CycleTimeDays).ToList();
            var p = ComputePercentiles(times);
            return new CycleTimeTrendEntry(
                pair.Sprint.Id, pair.Sprint.Name,
                Math.Round(p.P85, 1), Math.Round(p.P50, 1),
                pair.Results.Count);
        }).ToList();

        // Sprint summaries
        var sprintSummaries = perSprintResults.Select(pair =>
        {
            var times = pair.Results.Select(r => r.CycleTimeDays).ToList();
            var p = ComputePercentiles(times);
            var median = p.P50;
            var outlierCount = CountOutliers(pair.Results, median);
            return new CycleTimeSprintSummaryEntry(
                pair.Sprint.Id, pair.Sprint.Name, pair.Sprint.StartDate,
                pair.Results.Count,
                Math.Round(median, 1),
                Math.Round(p.P85, 1),
                outlierCount);
        }).ToList();

        // Averaged stage funnel
        var stageFunnel = BuildMultiSprintStageFunnel(perSprintResults, orderedStages, startStage, endStage, settings.WorkflowStages);

        // Metric cards: per-sprint averaging (BR12)
        List<CycleTimeMetricCard>? metricCards = null;
        if (perSprintResults.Count > 0)
        {
            var sprintMetrics = perSprintResults.Select(pair =>
            {
                var times = pair.Results.Select(r => r.CycleTimeDays).ToList();
                var p = ComputePercentiles(times);
                var outlierCount = CountOutliers(pair.Results, p.P50);
                return (P50: p.P50, P85: p.P85, Throughput: pair.Results.Count, OutlierCount: outlierCount);
            }).ToList();

            var avgP50 = sprintMetrics.Average(m => m.P50);
            var avgP85 = sprintMetrics.Average(m => m.P85);
            var avgThroughput = (decimal)sprintMetrics.Average(m => m.Throughput);
            var avgOutliers = (decimal)sprintMetrics.Average(m => m.OutlierCount);

            // Delta: most recent vs one before it
            decimal? deltaP50 = null, deltaP85 = null;
            int? deltaThroughput = null, deltaOutliers = null;

            if (sprintMetrics.Count >= 2)
            {
                var last = sprintMetrics[^1];
                var prev = sprintMetrics[^2];
                deltaP50 = last.P50 - prev.P50;
                deltaP85 = last.P85 - prev.P85;
                deltaThroughput = last.Throughput - prev.Throughput;
                deltaOutliers = last.OutlierCount - prev.OutlierCount;
            }

            metricCards = new List<CycleTimeMetricCard>
            {
                BuildCard("Median Cycle Time", avgP50, $"{Math.Round(avgP50, 1)} days", deltaP50, "positive-down"),
                BuildCard("P85 Cycle Time", avgP85, $"{Math.Round(avgP85, 1)} days", deltaP85, "positive-down"),
                BuildCard("Throughput", avgThroughput, $"{Math.Round(avgThroughput, 1)} tickets", deltaThroughput.HasValue ? (decimal)deltaThroughput.Value : null, "positive-up"),
                BuildCard("Outliers", avgOutliers, $"{Math.Round(avgOutliers, 1)} outliers", deltaOutliers.HasValue ? (decimal)deltaOutliers.Value : null, "positive-down"),
            };
        }

        return new CycleTimeMultiSprintResponse(
            sprintInfos, metricCards, trend, stageFunnel, sprintSummaries, boundaries);
    }

    // --- Boundary resolution (BR13) ---

    private static (string StartStage, string EndStage, List<string> OrderedStages) ResolveBoundaries(AppSettings settings)
    {
        var orderedStages = new List<string>();
        orderedStages.AddRange(settings.WorkflowStages);
        orderedStages.AddRange(settings.DoneStatuses);

        var startStage = settings.CycleTimeStartStage
            ?? (settings.WorkflowStages.Count >= 2 ? settings.WorkflowStages[1] : settings.WorkflowStages.FirstOrDefault() ?? string.Empty);

        var endStage = settings.CycleTimeEndStage
            ?? (settings.DoneStatuses.Count > 0 ? settings.DoneStatuses[0] : string.Empty);

        return (startStage, endStage, orderedStages);
    }

    // --- Core ticket computation (BR1-5, BR18, BR19) ---

    private static List<TicketCycleResult> ComputeTicketCycleTime(
        List<SprintMembership> memberships,
        List<StatusTransition> allTransitions,
        List<string> orderedStages,
        int endIndex,
        string startStage,
        string endStage,
        DateTime sprintStart,
        DateTime sprintEnd)
    {
        // BR1: Only tickets that have a qualifying completion transition within the sprint window
        var completedMemberships = memberships
            .Where(m =>
            {
                if (m.RemovedAt != null) return false;
                var (isCompleted, _) = TransitionAttributionChecker.IsCompletedInSprint(
                    m.TicketId, allTransitions, sprintStart, sprintEnd, orderedStages, endIndex);
                return isCompleted;
            })
            .ToList();

        if (completedMemberships.Count == 0)
            return [];

        var ticketIds = completedMemberships.Select(m => m.TicketId).ToHashSet();
        var transitionsByTicket = allTransitions
            .Where(t => ticketIds.Contains(t.TicketId))
            .GroupBy(t => t.TicketId)
            .ToDictionary(g => g.Key, g => g.OrderBy(t => t.Timestamp).ToList());

        // Determine which stages are within boundaries
        var startIdx = orderedStages.IndexOf(startStage);
        var endIdx = orderedStages.IndexOf(endStage);

        var results = new List<TicketCycleResult>();

        foreach (var membership in completedMemberships)
        {
            var ticketId = membership.TicketId;
            if (!transitionsByTicket.TryGetValue(ticketId, out var transitions))
                continue;

            // BR19: Find earliest qualifying completion transition within sprint window
            var doneTransition = transitions
                .Where(t => TransitionAttributionChecker.GetStageIndex(t.ToStatus, orderedStages) >= endIndex
                            && t.Timestamp >= sprintStart && t.Timestamp <= sprintEnd)
                .OrderBy(t => t.Timestamp)
                .FirstOrDefault();

            if (doneTransition is null)
                continue; // BR19: ticket was already done before sprint window

            var completionDate = doneTransition.Timestamp;

            // Build per-stage time accumulation
            var stageDurations = new Dictionary<string, decimal>();
            var visitedStages = new HashSet<string>();
            var reworkCount = 0;

            // Walk transitions
            for (int i = 0; i < transitions.Count; i++)
            {
                var transition = transitions[i];
                var stage = transition.ToStatus;

                // Only measure stages within boundaries
                var stageIdx = orderedStages.IndexOf(stage);
                if (startIdx < 0 || endIdx < 0)
                    continue;
                // BR21: stages not in orderedStages at all (renamed/removed) are tracked as "Other"
                if (stageIdx == -1)
                {
                    // Accumulate time under a sentinel key; resolved to "Other" in the funnel builder
                    var enterOther = transition.Timestamp < sprintStart ? sprintStart : transition.Timestamp;
                    DateTime exitOther;
                    if (i + 1 < transitions.Count)
                    {
                        var next = transitions[i + 1];
                        exitOther = next.Timestamp > sprintEnd ? sprintEnd : next.Timestamp;
                    }
                    else
                    {
                        exitOther = sprintEnd;
                    }
                    if (exitOther > enterOther)
                    {
                        var otherDuration = (decimal)(exitOther - enterOther).TotalDays;
                        stageDurations["__Other__"] = stageDurations.GetValueOrDefault("__Other__", 0) + otherDuration;
                    }
                    continue;
                }
                if (stageIdx < startIdx || stageIdx > endIdx)
                    continue;

                // Rework: re-entering a previously visited stage (BR4)
                if (visitedStages.Contains(stage))
                    reworkCount++;
                visitedStages.Add(stage);

                // Determine entry time (clamped to sprint start, BR18)
                var enterTime = transition.Timestamp < sprintStart ? sprintStart : transition.Timestamp;

                // Determine exit time: next transition that leaves this stage, or sprint end
                DateTime exitTime;
                if (i + 1 < transitions.Count)
                {
                    var nextTransition = transitions[i + 1];
                    exitTime = nextTransition.Timestamp > sprintEnd ? sprintEnd : nextTransition.Timestamp;
                }
                else
                {
                    exitTime = sprintEnd; // BR18: still in stage at sprint end
                }

                if (exitTime <= enterTime)
                    continue;

                var duration = (decimal)(exitTime - enterTime).TotalDays;
                if (stageDurations.ContainsKey(stage))
                    stageDurations[stage] += duration;
                else
                    stageDurations[stage] = duration;
            }

            // BR5: Skip tickets that never entered the start stage
            if (!visitedStages.Contains(startStage))
                continue;

            var totalCycleTime = stageDurations.Values.Sum();

            if (totalCycleTime <= 0)
                continue;

            var ticket = membership.Ticket;
            results.Add(new TicketCycleResult(
                ticketId,
                ticket?.Summary ?? ticketId,
                ticket?.IssueType ?? "Unknown",
                ticket?.AssigneeId,
                Math.Round(totalCycleTime, 2),
                completionDate,
                stageDurations,
                reworkCount));
        }

        return results;
    }

    // --- Percentile calculation (BR7) ---

    private record PercentileSet(decimal P50, decimal P75, decimal P85, decimal P90);

    private static PercentileSet ComputePercentiles(List<decimal> values)
    {
        if (values.Count == 0)
            return new PercentileSet(0, 0, 0, 0);

        var sorted = values.OrderBy(v => v).ToList();
        return new PercentileSet(
            Percentile(sorted, 0.50),
            Percentile(sorted, 0.75),
            Percentile(sorted, 0.85),
            Percentile(sorted, 0.90));
    }

    private static decimal Percentile(List<decimal> sorted, double p)
    {
        if (sorted.Count == 1)
            return sorted[0];

        var index = p * (sorted.Count - 1);
        var lower = (int)Math.Floor(index);
        var upper = (int)Math.Ceiling(index);
        if (lower == upper)
            return sorted[lower];

        var fraction = (decimal)(index - lower);
        return sorted[lower] + fraction * (sorted[upper] - sorted[lower]);
    }

    // --- Outlier counting (BR6) ---

    private static int CountOutliers(List<TicketCycleResult> results, decimal median) =>
        results.Count(r => r.CycleTimeDays > median * 2);

    // --- Metric card builders ---

    private List<CycleTimeMetricCard> BuildMetricCards(
        decimal p50, decimal p85, int throughput, int outlierCount,
        (decimal? PriorP50, decimal? PriorP85, int? PriorThroughput, int? PriorOutlierCount)? prior)
    {
        decimal? deltaP50 = prior.HasValue && prior.Value.PriorP50.HasValue ? p50 - prior.Value.PriorP50.Value : null;
        decimal? deltaP85 = prior.HasValue && prior.Value.PriorP85.HasValue ? p85 - prior.Value.PriorP85.Value : null;
        decimal? deltaThroughput = prior.HasValue && prior.Value.PriorThroughput.HasValue ? throughput - prior.Value.PriorThroughput.Value : null;
        decimal? deltaOutliers = prior.HasValue && prior.Value.PriorOutlierCount.HasValue ? outlierCount - prior.Value.PriorOutlierCount.Value : null;

        return new List<CycleTimeMetricCard>
        {
            BuildCard("Median Cycle Time", p50, $"{Math.Round(p50, 1)} days", deltaP50, "positive-down"),
            BuildCard("P85 Cycle Time", p85, $"{Math.Round(p85, 1)} days", deltaP85, "positive-down"),
            BuildCard("Throughput", throughput, $"{throughput} tickets", deltaThroughput, "positive-up"),
            BuildCard("Outliers", outlierCount, $"{outlierCount} outliers", deltaOutliers, "positive-down"),
        };
    }

    private static CycleTimeMetricCard BuildCard(string name, decimal value, string displayValue, decimal? delta, string polarity)
    {
        string? direction = null;
        string? deltaPolarity = null;

        if (delta.HasValue)
        {
            direction = delta.Value > 0 ? "up" : delta.Value < 0 ? "down" : "flat";
            deltaPolarity = polarity switch
            {
                "positive-up" => delta.Value > 0 ? "positive" : delta.Value < 0 ? "negative" : "neutral",
                "positive-down" => delta.Value < 0 ? "positive" : delta.Value > 0 ? "negative" : "neutral",
                _ => "neutral"
            };
        }

        return new CycleTimeMetricCard(
            name,
            Math.Round(value, 1),
            displayValue,
            delta.HasValue ? Math.Round(delta.Value, 1) : null,
            direction,
            deltaPolarity);
    }

    // --- Scatter plot builder ---

    private static List<CycleTimeScatterPoint> BuildScatterPlot(
        List<TicketCycleResult> results,
        List<string> orderedStages,
        string startStage,
        string endStage)
    {
        return results.Select(r =>
        {
            var breakdown = BuildStageBreakdown(r.StageDurations, orderedStages, startStage, endStage);
            return new CycleTimeScatterPoint(
                r.TicketKey, r.TicketSummary, r.IssueType,
                r.CycleTimeDays, r.CompletionDate, breakdown, r.ReworkCount);
        }).ToList();
    }

    // --- Stage funnel builder (BR8) ---

    private static List<StageFunnelEntry> BuildStageFunnel(
        List<TicketCycleResult> results,
        List<string> orderedStages,
        string startStage,
        string endStage,
        List<string> configuredWorkflowStages)
    {
        if (results.Count == 0)
            return [];

        var startIdx = orderedStages.IndexOf(startStage);
        var endIdx = orderedStages.IndexOf(endStage);

        if (startIdx < 0 || endIdx < 0 || startIdx > endIdx)
            return [];

        // Stages within boundaries
        var boundaryStages = orderedStages.Skip(startIdx).Take(endIdx - startIdx + 1).ToList();

        // Accumulate durations per recognized stage, group unrecognized as "Other"
        var stageSums = new Dictionary<string, decimal>();
        var otherSum = 0m;

        foreach (var result in results)
        {
            foreach (var kvp in result.StageDurations)
            {
                if (configuredWorkflowStages.Contains(kvp.Key) || orderedStages.Contains(kvp.Key))
                {
                    if (boundaryStages.Contains(kvp.Key))
                    {
                        stageSums[kvp.Key] = stageSums.GetValueOrDefault(kvp.Key, 0) + kvp.Value;
                    }
                }
                else
                {
                    // BR21: Unrecognized stages grouped as "Other"
                    otherSum += kvp.Value;
                }
            }
        }

        var ticketCount = results.Count;
        var funnelEntries = new List<(string Stage, decimal Avg)>();

        foreach (var stage in boundaryStages)
        {
            var avg = stageSums.ContainsKey(stage) ? stageSums[stage] / ticketCount : 0m;
            funnelEntries.Add((stage, avg));
        }

        if (otherSum > 0)
        {
            funnelEntries.Add(("Other", otherSum / ticketCount));
        }

        var total = funnelEntries.Sum(e => e.Avg);

        return funnelEntries.Select(e => new StageFunnelEntry(
            e.Stage,
            Math.Round(e.Avg, 2),
            total > 0 ? Math.Round(e.Avg / total * 100, 1) : 0)).ToList();
    }

    // --- Multi-sprint stage funnel builder ---

    private static List<StageFunnelEntry> BuildMultiSprintStageFunnel(
        List<(Sprint Sprint, List<TicketCycleResult> Results)> perSprintResults,
        List<string> orderedStages,
        string startStage,
        string endStage,
        List<string> configuredWorkflowStages)
    {
        if (perSprintResults.Count == 0)
            return [];

        // Average across sprints: compute per-sprint funnel, then average the averages
        var sprintFunnels = perSprintResults
            .Select(pair => BuildStageFunnel(pair.Results, orderedStages, startStage, endStage, configuredWorkflowStages))
            .Where(f => f.Count > 0)
            .ToList();

        if (sprintFunnels.Count == 0)
            return [];

        // Gather all stage names across all funnels
        var allStageNames = sprintFunnels
            .SelectMany(f => f.Select(e => e.StageName))
            .Distinct()
            .ToList();

        // Preserve boundary stage order
        var startIdx = orderedStages.IndexOf(startStage);
        var endIdx = orderedStages.IndexOf(endStage);
        var boundaryStages = startIdx >= 0 && endIdx >= 0
            ? orderedStages.Skip(startIdx).Take(endIdx - startIdx + 1).ToList()
            : new List<string>();

        var orderedNames = boundaryStages.Where(s => allStageNames.Contains(s)).ToList();
        if (allStageNames.Contains("Other"))
            orderedNames.Add("Other");

        var averaged = orderedNames.Select(stageName =>
        {
            var sprintsWithStage = sprintFunnels.Where(f => f.Any(e => e.StageName == stageName)).ToList();
            var avg = sprintsWithStage.Count > 0
                ? sprintsWithStage.Average(f => f.First(e => e.StageName == stageName).AverageDurationDays)
                : 0m;
            return (stageName, avg);
        }).ToList();

        var total = averaged.Sum(e => e.avg);
        return averaged.Select(e => new StageFunnelEntry(
            e.stageName,
            Math.Round(e.avg, 2),
            total > 0 ? Math.Round(e.avg / total * 100, 1) : 0)).ToList();
    }

    // --- Issue type breakdown ---

    private static List<CycleTimeIssueTypeEntry> BuildIssueTypeBreakdown(List<TicketCycleResult> results)
    {
        return results
            .GroupBy(r => r.IssueType)
            .Select(g =>
            {
                var times = g.Select(r => r.CycleTimeDays).OrderBy(t => t).ToList();
                var p = ComputePercentiles(times);
                return new CycleTimeIssueTypeEntry(
                    g.Key, g.Count(),
                    Math.Round(p.P50, 1),
                    Math.Round(p.P85, 1));
            })
            .OrderBy(e => e.IssueType)
            .ToList();
    }

    // --- Developer breakdown (BR9) ---

    private static List<CycleTimeDeveloperEntry> BuildDeveloperBreakdown(
        List<TicketCycleResult> results,
        Sprint sprint,
        List<string> orderedStages,
        string startStage,
        string endStage)
    {
        var byDeveloper = results
            .Where(r => r.AssigneeId != null)
            .GroupBy(r => r.AssigneeId!)
            .ToList();

        var entries = new List<CycleTimeDeveloperEntry>();

        foreach (var group in byDeveloper)
        {
            var devResults = group.ToList();
            var times = devResults.Select(r => r.CycleTimeDays).OrderBy(t => t).ToList();
            var p = ComputePercentiles(times);

            // Dominant stage: stage with highest average time for this developer's tickets (BR9)
            var startIdx = orderedStages.IndexOf(startStage);
            var endIdx = orderedStages.IndexOf(endStage);
            var boundaryStages = startIdx >= 0 && endIdx >= 0
                ? orderedStages.Skip(startIdx).Take(endIdx - startIdx + 1).ToList()
                : new List<string>();

            var stageSums = new Dictionary<string, decimal>();
            foreach (var result in devResults)
            {
                foreach (var kvp in result.StageDurations)
                {
                    if (boundaryStages.Contains(kvp.Key))
                        stageSums[kvp.Key] = stageSums.GetValueOrDefault(kvp.Key, 0) + kvp.Value;
                }
            }

            var dominantStage = stageSums.Count > 0
                ? stageSums.MaxBy(kvp => kvp.Value).Key
                : startStage;

            // Find developer info from sprint memberships
            var membership = sprint.Memberships.FirstOrDefault(m => m.Ticket?.AssigneeId == group.Key);
            var developer = membership?.Ticket?.Assignee;

            entries.Add(new CycleTimeDeveloperEntry(
                developer?.DisplayName ?? group.Key,
                developer?.AvatarUrl,
                developer?.SubTeam,
                devResults.Count,
                Math.Round(p.P50, 1),
                Math.Round(p.P85, 1),
                dominantStage));
        }

        return entries.OrderBy(e => e.DisplayName).ToList();
    }

    // --- Outlier table (BR6) ---

    private static List<CycleTimeOutlierEntry> BuildOutlierTable(
        List<TicketCycleResult> results,
        decimal median,
        List<string> orderedStages,
        string startStage,
        string endStage)
    {
        return results
            .Where(r => r.CycleTimeDays > median * 2)
            .OrderByDescending(r => r.CycleTimeDays)
            .Select(r =>
            {
                var breakdown = BuildStageBreakdown(r.StageDurations, orderedStages, startStage, endStage);
                return new CycleTimeOutlierEntry(
                    r.TicketKey, r.TicketSummary, r.IssueType,
                    r.CycleTimeDays, breakdown, r.ReworkCount);
            })
            .ToList();
    }

    // --- Stage breakdown helper ---

    private static List<StageBreakdownEntry> BuildStageBreakdown(
        Dictionary<string, decimal> stageDurations,
        List<string> orderedStages,
        string startStage,
        string endStage)
    {
        var startIdx = orderedStages.IndexOf(startStage);
        var endIdx = orderedStages.IndexOf(endStage);

        if (startIdx < 0 || endIdx < 0)
            return stageDurations.Select(kvp => new StageBreakdownEntry(kvp.Key, Math.Round(kvp.Value, 2))).ToList();

        var boundaryStages = orderedStages.Skip(startIdx).Take(endIdx - startIdx + 1).ToList();

        return stageDurations
            .Where(kvp => boundaryStages.Contains(kvp.Key))
            .OrderBy(kvp => orderedStages.IndexOf(kvp.Key))
            .Select(kvp => new StageBreakdownEntry(kvp.Key, Math.Round(kvp.Value, 2)))
            .ToList();
    }

    // --- Sub-team filtering ---

    private static List<SprintMembership> FilterMemberships(
        IReadOnlyList<SprintMembership> memberships,
        string? subTeam,
        HashSet<string>? excludedDeveloperIds = null)
    {
        var result = memberships.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(subTeam))
            result = result.Where(m => m.Ticket?.Assignee?.SubTeam == subTeam);
        if (excludedDeveloperIds is { Count: > 0 })
            result = result.Where(m => m.Ticket?.AssigneeId == null || !excludedDeveloperIds.Contains(m.Ticket.AssigneeId));
        return result.ToList();
    }
}
