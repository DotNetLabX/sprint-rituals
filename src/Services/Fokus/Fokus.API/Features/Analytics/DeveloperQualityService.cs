namespace Fokus.API.Features.Analytics;

// --- Result records ---

public record DeveloperQualitySprintInfo(int Id, string Name, DateTime StartDate, DateTime EndDate);

public record DeveloperQualitySprintBreakdown(
    int SprintId,
    int Stories,
    int Covered,
    decimal CoveragePercent,
    decimal PassRatePercent,
    int Untested,
    int BugsFound,
    string? CoverageRag,
    string? PassRateRag,
    decimal? CoveragePercentDelta,
    string? CoveragePercentDeltaDirection,
    string? CoveragePercentDeltaPolarity,
    decimal? PassRatePercentDelta,
    string? PassRatePercentDeltaDirection,
    string? PassRatePercentDeltaPolarity,
    int? BugsFoundDelta,
    string? BugsFoundDeltaDirection,
    string? BugsFoundDeltaPolarity,
    List<SparklinePoint>? CoverageSparkline,
    List<SparklinePoint>? PassRateSparkline);

public record DeveloperQualityEntry(
    string AccountId,
    string DisplayName,
    string? SubTeam,
    string? AvatarUrl,
    List<DeveloperQualitySprintBreakdown> SprintBreakdowns,
    int? BelowMedianStreak);

public record DeveloperQualityResult(
    bool HasQaData,
    List<DeveloperQualitySprintInfo> Sprints,
    List<DeveloperQualityEntry> Developers);

// --- Service ---

public class DeveloperQualityService
{
    public DeveloperQualityResult ComputeDeveloperQuality(
        List<Sprint> allLoadedSprints,
        List<int> targetSprintIds,
        List<Developer> activeDevelopers,
        Dictionary<int, List<TestExecution>> tesBySprintId,
        List<StatusTransition> statusTransitions,
        AppSettings settings,
        string? subTeam,
        bool isSingleSprint,
        Sprint? priorSprint,
        List<TestExecution>? priorSprintTEs,
        List<Sprint> sparklineWindow,
        Dictionary<int, List<TestExecution>> sparklineTEsBySprintId,
        List<Sprint> streakWindow,
        Dictionary<int, List<TestExecution>> streakTEsBySprintId)
    {
        var (orderedStages, startIndex) = ResolveStartIndex(settings);

        // Sub-team filtering on developers
        var filteredDevelopers = FilterDevelopers(activeDevelopers, subTeam);

        // Sort all loaded sprints ascending
        var sortedAllSprints = allLoadedSprints.OrderBy(s => s.StartDate).ToList();

        // Target sprints in ascending order
        var targetSprints = sortedAllSprints
            .Where(s => targetSprintIds.Contains(s.Id))
            .OrderBy(s => s.StartDate)
            .ToList();

        // Pre-group transitions by ticketId once
        var transitionsByTicket = statusTransitions
            .GroupBy(t => t.TicketId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        // Sprint info list
        var sprintInfos = targetSprints
            .Select(s => new DeveloperQualitySprintInfo(s.Id, s.Name, s.StartDate, s.EndDate))
            .ToList();

        // Check if there's any QA data at all
        var hasQaData = tesBySprintId.Values.Any(tes => tes.Count > 0);

        // Build per-developer entries
        var developerEntries = filteredDevelopers.Select(developer =>
        {
            var breakdowns = targetSprints.Select(sprint =>
            {
                var sprintTEs = tesBySprintId.GetValueOrDefault(sprint.Id, []);

                var (stories, covered, coveragePercent, passRatePercent, untested, bugsFound) =
                    ComputeDevMetrics(developer.Id, sprint, sprintTEs, transitionsByTicket, settings, orderedStages, startIndex, subTeam);

                var thresholds = settings.QaHealthThresholds;
                var coverageRag = HealthScoreCalculator.MetricRag(coveragePercent, thresholds.CoverageGreen, thresholds.CoverageAmber, higherIsBetter: true);
                var passRateRag = HealthScoreCalculator.MetricRag(passRatePercent, thresholds.PassRateGreen, thresholds.PassRateAmber, higherIsBetter: true);

                // Deltas (single-sprint only, with prior sprint data)
                decimal? coverageDelta = null;
                string? coverageDeltaDir = null;
                string? coverageDeltaPol = null;
                decimal? passRateDelta = null;
                string? passRateDeltaDir = null;
                string? passRateDeltaPol = null;
                int? bugsDelta = null;
                string? bugsDeltaDir = null;
                string? bugsDeltaPol = null;
                List<SparklinePoint>? coverageSparkline = null;
                List<SparklinePoint>? passRateSparkline = null;

                if (isSingleSprint && priorSprint is not null && priorSprintTEs is not null && priorSprintTEs.Count > 0)
                {
                    var (_, _, priorCoverage, priorPassRate, _, priorBugs) =
                        ComputeDevMetrics(developer.Id, priorSprint, priorSprintTEs, transitionsByTicket, settings, orderedStages, startIndex, subTeam);

                    coverageDelta = Math.Round(coveragePercent - priorCoverage, 1);
                    coverageDeltaDir = DeltaDirection(coverageDelta.Value);
                    coverageDeltaPol = DeltaPolarity(coverageDelta.Value, positiveUp: true);

                    passRateDelta = Math.Round(passRatePercent - priorPassRate, 1);
                    passRateDeltaDir = DeltaDirection(passRateDelta.Value);
                    passRateDeltaPol = DeltaPolarity(passRateDelta.Value, positiveUp: true);

                    bugsDelta = bugsFound - priorBugs;
                    bugsDeltaDir = DeltaDirection(bugsDelta.Value);
                    bugsDeltaPol = "neutral";
                }

                if (isSingleSprint && sparklineWindow.Count > 0)
                {
                    coverageSparkline = BuildDevSparkline(developer.Id, sparklineWindow, sparklineTEsBySprintId, transitionsByTicket, settings, orderedStages, startIndex, subTeam, usePassRate: false);
                    passRateSparkline = BuildDevSparkline(developer.Id, sparklineWindow, sparklineTEsBySprintId, transitionsByTicket, settings, orderedStages, startIndex, subTeam, usePassRate: true);
                }

                return new DeveloperQualitySprintBreakdown(
                    sprint.Id,
                    stories,
                    covered,
                    Math.Round(coveragePercent, 1),
                    Math.Round(passRatePercent, 1),
                    untested,
                    bugsFound,
                    coverageRag,
                    passRateRag,
                    coverageDelta,
                    coverageDeltaDir,
                    coverageDeltaPol,
                    passRateDelta,
                    passRateDeltaDir,
                    passRateDeltaPol,
                    bugsDelta,
                    bugsDeltaDir,
                    bugsDeltaPol,
                    coverageSparkline,
                    passRateSparkline);
            }).ToList();

            // Below-median streak (single-sprint only, computed after all breakdowns)
            int? belowMedianStreak = null;
            if (isSingleSprint && targetSprints.Count == 1)
            {
                belowMedianStreak = ComputeBelowMedianStreak(
                    developer.Id, targetSprints[0],
                    streakWindow, streakTEsBySprintId,
                    transitionsByTicket,
                    filteredDevelopers, settings, orderedStages, startIndex, subTeam);
            }

            return new DeveloperQualityEntry(
                developer.Id,
                developer.DisplayName,
                developer.SubTeam,
                developer.AvatarUrl,
                breakdowns,
                belowMedianStreak);
        }).ToList();

        return new DeveloperQualityResult(hasQaData, sprintInfos, developerEntries);
    }

    // --- Per-developer metrics for a single sprint ---

    private static (int Stories, int Covered, decimal CoveragePercent, decimal PassRatePercent, int Untested, int BugsFound)
        ComputeDevMetrics(
            string developerId,
            Sprint sprint,
            List<TestExecution> sprintTEs,
            Dictionary<string, List<StatusTransition>> transitionsByTicket,
            AppSettings settings,
            List<string> orderedStages,
            int startIndex,
            string? subTeam)
    {
        // Active scope for this developer: feature tickets that IsStartedInSprint
        var devMemberships = sprint.Memberships
            .Where(m => m.Ticket?.AssigneeId == developerId &&
                        (string.IsNullOrWhiteSpace(subTeam) || m.Ticket?.Assignee?.SubTeam == subTeam))
            .ToList();

        var activeTicketKeys = GetActiveFeatureTicketKeysForDev(
            devMemberships, transitionsByTicket,
            sprint.StartDate, sprint.EndDate,
            orderedStages, startIndex,
            settings.ExcludedFromScopeStatuses, settings.DefaultSpPerBug);

        if (activeTicketKeys.Count == 0)
            return (0, 0, 0m, 0m, 0, 0);

        // Build parent lookup from all sprint memberships (for sub-task inheritance)
        var parentByTicket = sprint.Memberships
            .Where(m => m.Ticket?.ParentTicketKey != null)
            .ToDictionary(m => m.TicketId, m => m.Ticket!.ParentTicketKey!, StringComparer.OrdinalIgnoreCase);

        // Build tests-link map from non-cancelled TEs
        var testsTesByTicket = BuildTestsTesByTicket(sprintTEs);

        // Coverage per developer (BR2-BR3) with sub-task inheritance (BR10)
        var coveredKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in activeTicketKeys)
        {
            if (testsTesByTicket.ContainsKey(key) && testsTesByTicket[key].Count > 0)
            {
                coveredKeys.Add(key);
                continue;
            }
            // Sub-task inheritance: if no own TEs, check parent
            if (parentByTicket.TryGetValue(key, out var parentKey) &&
                !string.IsNullOrEmpty(parentKey) &&
                testsTesByTicket.TryGetValue(parentKey, out var parentTeIds) &&
                parentTeIds.Count > 0)
            {
                coveredKeys.Add(key);
            }
        }

        var stories = activeTicketKeys.Count;
        var covered = coveredKeys.Count;
        var coveragePercent = stories > 0 ? (decimal)covered / stories * 100 : 0m;
        var untested = stories - covered;

        // Pass rate (BR4) and bugs found (BR6): only TEs directly linked to this developer's
        // active scope tickets. Sub-task inheritance (BR10) is coverage-only — it marks a story
        // as covered but does not pull the parent's test runs into the developer's pass rate or
        // bug count.
        var directTeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in activeTicketKeys)
        {
            if (testsTesByTicket.TryGetValue(key, out var teIds))
                foreach (var id in teIds) directTeIds.Add(id);
        }

        var devTEs = sprintTEs.Where(te => directTeIds.Contains(te.Id)).ToList();
        var passRatePercent = ComputePassRate(devTEs);

        // Bugs found (BR6): unique bug tickets from Blocks links on this developer's TEs
        var bugKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var te in devTEs)
        {
            foreach (var link in te.Links.Where(l => l.LinkType == TestExecutionLinkType.Blocks))
            {
                if (link.Ticket?.IssueType == "Bug")
                    bugKeys.Add(link.TicketKey);
            }
        }
        var bugsFound = bugKeys.Count;

        return (stories, covered, coveragePercent, passRatePercent, untested, bugsFound);
    }

    // --- Active scope helper ---

    private static HashSet<string> GetActiveFeatureTicketKeysForDev(
        List<SprintMembership> memberships,
        Dictionary<string, List<StatusTransition>> transitionsByTicket,
        DateTime sprintStart,
        DateTime sprintEnd,
        List<string> orderedStages,
        int startIndex,
        List<string> excludedStatuses,
        int defaultSpPerBug)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in memberships)
        {
            if (m.RemovedAt != null) continue;
            if (m.Ticket?.IssueType == "Bug") continue;
            if (excludedStatuses.Contains(m.FinalStatus, StringComparer.OrdinalIgnoreCase)) continue;
            if (!GetEffectiveSp(m, defaultSpPerBug).HasValue) continue;

            var ticketTransitions = transitionsByTicket.GetValueOrDefault(m.TicketId, []);
            if (IsStartedInSprint(ticketTransitions, sprintStart, sprintEnd, orderedStages, startIndex))
                result.Add(m.TicketId);
        }
        return result;
    }

    // --- Pass rate (BR4) ---

    private static decimal ComputePassRate(List<TestExecution> tes)
    {
        var passCount = 0;
        var totalCount = 0;
        foreach (var te in tes)
        {
            foreach (var run in te.TestRuns)
            {
                if (run.Status == TestRunStatus.Pass) { passCount++; totalCount++; }
                else if (run.Status == TestRunStatus.Fail) { totalCount++; }
            }
        }
        return totalCount > 0 ? Math.Round((decimal)passCount / totalCount * 100, 1) : 0m;
    }

    // --- Tests-link map (non-cancelled TEs) ---

    private static Dictionary<string, List<string>> BuildTestsTesByTicket(List<TestExecution> nonCancelledTEs)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var te in nonCancelledTEs)
        {
            foreach (var link in te.Links.Where(l => l.LinkType == TestExecutionLinkType.Tests))
            {
                if (!result.TryGetValue(link.TicketKey, out var list))
                {
                    list = [];
                    result[link.TicketKey] = list;
                }
                if (!list.Contains(te.Id))
                    list.Add(te.Id);
            }
        }
        return result;
    }

    // --- Sparkline for a developer ---

    private static List<SparklinePoint> BuildDevSparkline(
        string developerId,
        List<Sprint> window,
        Dictionary<int, List<TestExecution>> tesBySprintId,
        Dictionary<string, List<StatusTransition>> transitionsByTicket,
        AppSettings settings,
        List<string> orderedStages,
        int startIndex,
        string? subTeam,
        bool usePassRate)
    {
        var points = new List<SparklinePoint>();
        foreach (var sprint in window)
        {
            var sprintTEs = tesBySprintId.GetValueOrDefault(sprint.Id, []);
            var (_, _, coveragePercent, passRatePercent, _, _) =
                ComputeDevMetrics(developerId, sprint, sprintTEs, transitionsByTicket, settings, orderedStages, startIndex, subTeam);
            var value = usePassRate ? passRatePercent : coveragePercent;
            points.Add(new SparklinePoint(sprint.Name, Math.Round(value, 1)));
        }
        return points;
    }

    // --- Below-median streak (BR14-17, single-sprint only) ---

    private static int? ComputeBelowMedianStreak(
        string developerId,
        Sprint targetSprint,
        List<Sprint> streakSprints,
        Dictionary<int, List<TestExecution>> streakTEsBySprintId,
        Dictionary<string, List<StatusTransition>> transitionsByTicket,
        List<Developer> filteredDevelopers,
        AppSettings settings,
        List<string> orderedStages,
        int startIndex,
        string? subTeam)
    {
        // Walk backward from target sprint through consecutive closed sprints with QA data.
        // streakSprints is pre-filtered to only sprints with QA data (BR8-attributed),
        // ordered ascending, capped at up to 12 sprints ending at the target sprint.
        var targetIndex = streakSprints.FindIndex(s => s.Id == targetSprint.Id);
        if (targetIndex < 0) return null;

        var streak = 0;
        for (var i = targetIndex; i >= 0; i--)
        {
            var sprint = streakSprints[i];

            var sprintTEs = streakTEsBySprintId.GetValueOrDefault(sprint.Id, []);

            // Sprint with no QA data breaks the streak
            if (sprintTEs.Count == 0) break;

            // Get this developer's metrics for this sprint
            var (devStories, _, devCoverage, _, _, _) =
                ComputeDevMetrics(developerId, sprint, sprintTEs, transitionsByTicket, settings, orderedStages, startIndex, subTeam);

            // Sprint where developer has zero stories breaks the streak (BR15)
            if (devStories == 0) break;

            // Compute team median coverage for this sprint (BR14, BR16)
            var teamCoverageValues = new List<decimal>();
            foreach (var dev in filteredDevelopers)
            {
                var (s, _, cov, _, _, _) =
                    ComputeDevMetrics(dev.Id, sprint, sprintTEs, transitionsByTicket, settings, orderedStages, startIndex, subTeam);
                if (s > 0) // Exclude zero-story developers from median
                    teamCoverageValues.Add(cov);
            }

            if (teamCoverageValues.Count == 0) break;

            var median = ComputeMedian(teamCoverageValues);

            // Check if strictly below median
            if (devCoverage < median)
                streak++;
            else
                break;
        }

        return streak >= 2 ? streak : null;
    }

    private static decimal ComputeMedian(List<decimal> values)
    {
        if (values.Count == 0) return 0m;
        var sorted = values.OrderBy(v => v).ToList();
        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2
            : sorted[mid];
    }

    // --- Delta helpers ---

    private static string DeltaDirection(decimal delta) =>
        delta > 0 ? "up" : delta < 0 ? "down" : "flat";

    private static string DeltaDirection(int delta) =>
        delta > 0 ? "up" : delta < 0 ? "down" : "flat";

    private static string DeltaPolarity(decimal delta, bool positiveUp) =>
        positiveUp
            ? (delta > 0 ? "positive" : delta < 0 ? "negative" : "neutral")
            : (delta < 0 ? "positive" : delta > 0 ? "negative" : "neutral");

    // --- Sub-team filtering ---

    private static List<Developer> FilterDevelopers(List<Developer> developers, string? subTeam)
    {
        if (string.IsNullOrWhiteSpace(subTeam))
            return developers;
        return developers.Where(d => d.SubTeam == subTeam).ToList();
    }

    // --- Transition helpers ---

    private static bool IsStartedInSprint(
        List<StatusTransition> transitions,
        DateTime sprintStart,
        DateTime sprintEnd,
        List<string> orderedStages,
        int startIndex)
    {
        if (startIndex < 0) return false;
        return transitions.Any(t =>
            t.Timestamp >= sprintStart &&
            t.Timestamp <= sprintEnd &&
            GetStageIndex(t.ToStatus, orderedStages) >= startIndex);
    }

    private static int GetStageIndex(string status, List<string> orderedStages) =>
        orderedStages.FindIndex(s => string.Equals(s, status, StringComparison.OrdinalIgnoreCase));

    private static decimal? GetEffectiveSp(SprintMembership m, int defaultSpPerBug)
    {
        if (m.StoryPoints.HasValue && m.StoryPoints.Value > 0) return m.StoryPoints;
        if (m.Ticket?.IssueType == "Bug" && defaultSpPerBug > 0) return (decimal)defaultSpPerBug;
        return null;
    }

    // --- Settings helpers ---

    private static (List<string> OrderedStages, int StartIndex) ResolveStartIndex(AppSettings settings)
    {
        var orderedStages = new List<string>();
        orderedStages.AddRange(settings.WorkflowStages);
        orderedStages.AddRange(settings.DoneStatuses);

        var startStage = settings.CycleTimeStartStage
            ?? (orderedStages.Count > 0 ? orderedStages[0] : null);

        if (startStage is null)
            return (orderedStages, 0);

        var startIndex = orderedStages.FindIndex(s => string.Equals(s, startStage, StringComparison.OrdinalIgnoreCase));
        if (startIndex < 0) startIndex = 0;

        return (orderedStages, startIndex);
    }
}
