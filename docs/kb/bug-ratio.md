# Bug Ratio

Bug-vs-feature story-point ratios, multi-sprint aggregation, per-developer breakdowns, and the
consecutive-sprint bug-ratio alert. Computed by `BugRatioAnalyzer`.

Rules below are **verified** via the Mine→Verify pass (3 independent clean-room miners +
independent Codex refutation, 2026-06-14). Confidence is high: the three miners converged on the
same ~22 behaviors with zero contradictions.

## Rules

Rules below are **verified** via the Mine→Verify pass (automated harness, 2026 — 37 rules confirmed).

- BR-1: A SprintMembership is a 'bug' iff its Ticket.IssueType equals the exact string "Bug" via default C# == (case-sensitive, ordinal). "bug"/"BUG"/"Bug " do not match. A null Ticket yields null != "Bug", so null-Ticket memberships are non-bug.
- BR-2: Per-sprint bug ratio percent = bugSp / completedSp * 100 where completedSp = bugSp + nonBugSp. It is a story-point-weighted ratio, not a count ratio.
- BR-3: The ratio computation is guarded by a strictly-positive check: only when completedSp > 0 is the division performed; otherwise (completedSp <= 0, i.e. 0) the ratio degenerates to exactly 0m with no division. completedSp == 0 yields 0; the guard's strict > means a negative completedSp would also yield 0m. This same guard appears at every non-alert ratio site (per-sprint trend L35, per-dev sprint trend L76, team totals L48, single-sprint team L145, single-sprint dev L186, prior team L161, prior dev L201, multi-sprint dev aggregate L95).
- BR-4: Each per-sprint team trend datum rounds ratioPercent, bugSp, nonBugSp, and completedSp to 1 decimal via Math.Round (default banker's / round-half-to-even).
- BR-5: Team-level totals are a ratio-of-totals, not an average-of-ratios: totalBugSp/totalNonBugSp are summed across the per-sprint trend rows and teamRatio = totalBugSp / totalCompletedSp * 100 (same >0 guard). Critically the summed values are the ALREADY-ROUNDED per-sprint trend fields (t.BugSp, t.NonBugSp = the Math.Round(...,1) values), so the team total is a sum of rounded components, not of raw sprint sums.
- BR-6: Team metrics round teamRatio, totalBugSp, totalNonBugSp, and totalCompletedSp to 1 decimal and carry the per-sprint trend list.
- BR-7: Target sprints are sorted ascending by StartDate once (sortedTarget) and that order drives sprintInfos, the per-sprint trend, the issue-type aggregation, and per-developer breakdowns. OrderBy is stable, so StartDate ties preserve original input order.
- BR-8: Sprint info records (BugRatioSprintData) carry Id, Name, StartDate, EndDate copied verbatim from each sprint, built from the StartDate-ascending sorted target list.
- BR-9: Per-developer multi-sprint aggregate SP/ratio are a ratio-of-totals over the ALREADY-ROUNDED per-sprint breakdown fields: devBugSp/devNonBugSp = sum of b.BugSp/b.NonBugSp (each Math.Round(...,1)), devCompletedSp = devBugSp + devNonBugSp, devRatio = devBugSp/devCompletedSp*100 with the >0 guard.
- BR-10: Per-developer multi-sprint bug/non-bug ticket counts are sums of the per-sprint breakdown counts (devBugCount = sum b.BugTicketCount, devNonBugCount = sum b.NonBugTicketCount).
- BR-11: Per-developer per-sprint counts are raw integer counts of completed memberships: bugCount = Count(IsBug), nonBugCount = Count(!IsBug). They are emitted unrounded, unlike the SP/ratio fields which are Math.Round(...,1).
- BR-12: A membership counts as completed-in-sprint only if ALL of: (a) RemovedAt is null, (b) FinalStatus is NOT in excludedStatuses, and (c) TransitionAttributionChecker.IsCompletedInSprint returns true for its ticket transitions within [sprintStart, sprintEnd]. Each failing condition short-circuits to exclusion (return false).
- BR-13: The FinalStatus exclusion test is CASE-INSENSITIVE: excludedStatuses.Contains uses StringComparer.OrdinalIgnoreCase, so a FinalStatus matching an excluded status in any letter case is excluded.
- BR-14: Status transitions are grouped per ticket with a CASE-INSENSITIVE key (OrdinalIgnoreCase) in both the GroupBy and the resulting dictionary's comparer, so TicketIds differing only by case group together and lookups are case-insensitive. A ticket with no transitions resolves to an empty list via GetValueOrDefault(..., []) rather than throwing.
- BR-15: Bug SP = sum of GetEffectiveSp(defaultSpPerBug) over completed bug memberships; Non-Bug SP = same over completed non-bug memberships. A null effective SP is coalesced to 0m (?? 0m). The bug/non-bug split (Where(IsBug) vs Where(!IsBug)) is exhaustive and mutually exclusive over completed memberships; null-Ticket completed memberships land in nonBugSp. defaultSpPerBug originates from settings.BugRatio.DefaultSpPerBug.
- BR-16: Developer sub-team filtering: if subTeam is null/empty/whitespace, ALL developers are returned unfiltered; otherwise only developers whose SubTeam == subTeam (default case-sensitive ordinal equality) are kept.
- BR-17: FilterMemberships applies up to two successive filters: (1) if subTeam is non-blank, keep only memberships whose Ticket.Assignee.SubTeam == subTeam (case-sensitive; null Ticket/Assignee yields null != subTeam, dropped); (2) if excludedDeveloperIds is non-null AND Count > 0, drop memberships whose Ticket.AssigneeId is in the set, but KEEP memberships whose Ticket.AssigneeId is null (a null assignee is never excluded). excludedDeveloperIds.Contains uses the HashSet's own comparer.
- BR-18: Per-developer membership selection for a sprint (GetDeveloperMemberships): keep memberships whose Ticket.AssigneeId == developerId (case-sensitive), AND (subTeam blank OR Ticket.Assignee.SubTeam == subTeam, case-sensitive). A null Ticket fails the AssigneeId equality and is dropped. Note this path keys the sub-team check on the assignee's SubTeam and does NOT apply excludedDeveloperIds filtering.
- BR-19: Alert evaluation walks a developer's allClosedSprints from most recent to oldest (OrderByDescending StartDate). The descending sort is stable, so StartDate ties preserve original input order; the walk starts at the latest-start sprint.
- BR-20: Zero-SP streak reset: in the alert loop a sprint with completedSp == 0 BREAKS immediately — it is neither counted nor skipped-over; the streak ends at the first zero-SP sprint walking newest-to-oldest. The check is exact equality to 0, so a negative completedSp would not be caught here and would proceed to the ratio test.
- BR-21: Inside the alert loop the ratio = bugSp / completedSp * 100 is computed WITHOUT the >0 guard used elsewhere, but is only reached after the completedSp == 0 break, so completedSp is guaranteed non-zero and no divide-by-zero occurs. No rounding is applied before the threshold comparison. A sprint counts toward the streak (consecutiveAbove++) iff ratio >= threshold (inclusive boundary); a ratio strictly below threshold BREAKS the loop (streak ends) rather than skipping.
- BR-22: An alert is active when consecutiveAbove >= consecutiveCount (inclusive). The returned BugRatioAlertData carries (isAlert, consecutiveAbove, threshold). threshold = settings.BugRatio.AlertThreshold, consecutiveCount = settings.BugRatio.ConsecutiveSprintCount.
- BR-23: Issue-type breakdown groups completed memberships by Ticket.IssueType, substituting the literal "Unknown" when Ticket is null or IssueType is null. GroupBy uses default string equality (case-SENSITIVE/ordinal), so issue types differing only in case form separate groups. Per group: SP total = sum of GetEffectiveSp(defaultSpPerBug) (null -> 0m), rounded to 1 decimal; ticket count = raw g.Count(); the group key is the issue type.
- BR-24: Issue-type breakdown rows are ordered by TicketCount descending. OrderByDescending is stable, so groups tied on TicketCount retain their GroupBy emission (first-seen) order. SP total is NOT a tiebreaker — only count drives ordering, with no secondary sort key.
- BR-25: The multi-sprint issue-type breakdown is built from completed memberships flattened across ALL target sprints (SelectMany over sortedTarget, each sprint's memberships filtered by sub-team and excluded developers then reduced to transition-completed using that sprint's own window), then a single BuildIssueTypeBreakdown over the combined list. Each completed membership contributes once.
- BR-26: Both ComputeMultiSprint and ComputeSingleSprint resolve the stage pipeline identically at entry: (orderedStages, startIndex) from TransitionAttributionChecker.ResolveStartIndex(settings) and endIndex from ResolveEndIndex(settings, orderedStages). startIndex is computed but NOT used anywhere in this file; only orderedStages and endIndex flow into completion checks. Attribution logic is delegated to TransitionAttributionChecker.
- BR-27: Excluded-from-scope statuses and the per-bug default story points are sourced from settings (excludedStatuses = settings.ExcludedFromScopeStatuses, defaultSpPerBug = settings.BugRatio.DefaultSpPerBug) and threaded unchanged into all completion and SP computations.
- BR-28: The completion window is the sprint's own [StartDate, EndDate] passed positionally to GetTransitionCompletedMemberships / IsCompletedInSprint; this analyzer applies no boundary-inclusivity comparison itself — inclusivity is delegated to TransitionAttributionChecker.IsCompletedInSprint.
- BR-29: Single-sprint team metrics: completedSp = bugSp + nonBugSp; ratio = bugSp/completedSp*100 with the >0 guard (else 0m). Same ratio-of-totals semantics as the multi-sprint per-sprint computation.
- BR-30: Prior-sprint comparison runs only when priorSprint is non-null; otherwise priorBugSp/priorNonBugSp/priorCompletedSp/priorRatio stay null and no deltas are produced. When present, prior values use the prior sprint's own window and the same sub-team/excluded filters, and priorRatio = priorBugSp/priorCompletedSp*100 with the >0 guard (else 0m).
- BR-31: Single-sprint team metric cards: each card's delta is current-minus-prior, computed only when the prior value HasValue (else null). Bug Ratio % delta = ratio - priorRatio; Bug SP delta = bugSp - priorBugSp; Non-Bug SP delta = nonBugSp - priorNonBugSp. Polarity: Bug Ratio % and Bug SP are "positive-down" (lower is better); Non-Bug SP is "positive-up".
- BR-32: Single-sprint metric-card display strings use the "0.#" format (at most one decimal, trailing-zero decimals dropped): Bug Ratio % as $"{ratio:0.#}%" (with percent sign), Bug SP and Non-Bug SP as $"{x:0.#}" (no sign). This display format is distinct from the Math.Round(x,1) applied to numeric fields elsewhere.
- BR-33: Single-sprint per-developer metrics are computed directly from the developer's own memberships (GetDeveloperMemberships), not from rounded sprint breakdowns: devCompletedSp = devBugSp + devNonBugSp; devRatio = devBugSp/devCompletedSp*100 with >0 guard; devBugCount/devNonBugCount = Count(IsBug)/Count(!IsBug) over a second GetTransitionCompletedMemberships call (devCompleted).
- BR-34: Single-sprint per-developer delta block runs only when priorSprint is non-null (else delta is null). Deltas are computed from the UNROUNDED current and prior values (current - prior): bugSpDelta, nonBugSpDelta, ratioDelta, bugCountDelta, nonBugCountDelta; Math.Round(...,1) is then applied once to the SP/ratio deltas (rounded once, not derived from two pre-rounded operands), while count deltas stay integer. Each delta carries a Direction and a Polarity, with positiveUp false for bugSp/ratio/bugCount (lower-is-better) and true for nonBugSp/nonBugCount (higher-is-better).
- BR-35: Prior per-developer values for the delta are recomputed from the prior sprint's own developer memberships through the identical pipeline (GetDeveloperMemberships, ComputeCompletedBugNonBugSp, GetTransitionCompletedMemberships, IsBug counts) using the prior sprint's window; priorDevRatio uses the >0 guard. So deltas compare like-for-like recomputed metrics, not stored values.
- BR-36: Per-developer alerts are evaluated against the FULL allClosedSprints history (passed to EvaluateAlert), not just the target/reporting-window sprints, so a developer's alert reflects their recent closed-sprint streak independent of the report scope. Both the single- and multi-sprint paths pass settings.BugRatio.AlertThreshold and settings.BugRatio.ConsecutiveSprintCount.
- BR-37: Developer output records carry identity fields (dev.Id, DisplayName, SubTeam, AvatarUrl) alongside the rounded metrics and the alert. The multi-sprint record (BugRatioDeveloperData) additionally carries the per-sprint breakdown list; the single-sprint record (BugRatioDeveloperSingleData) carries the (possibly null) delta. Both round devBugSp/devNonBugSp/devCompletedSp/devRatio to 1 decimal and keep counts as integers.
## Key Files

- `src/Services/Fokus/Fokus.Domain/Analytics/BugRatioAnalyzer.cs` — production implementation
- `src/Services/Fokus/Fokus.Domain.Tests/Analytics/BugRatioAnalyzerTests.cs` — example-based tests (pending Step 3 / Cover)
- `src/Services/Fokus/Fokus.Domain.Tests/Analytics/BugRatioAnalyzerPropertyTests.cs` — property-based tests (pending Step 3 / Cover)

## Edge Cases

- **Rounding compounding (candidate bug):** team/dev totals sum the *already-rounded* per-sprint values, so the aggregate ratio is computed from rounded inputs — precision loss vs a raw `ΣbugSp / ΣcompletedSp`. Not gated; backlog.
- **Rounding inconsistency (candidate bug):** multi-sprint paths round the stored SP; single-sprint team cards (`BugRatioTeamSingleData`) store raw decimals and round only on display. Two paths, two behaviors.
- **`excludedDeveloperIds.Contains` comparer unspecified** in this file — case behavior depends on how the caller constructed the `HashSet`. Latent ambiguity.
- **`startIndex` dead variable:** `ResolveStartIndex` returns a `startIndex` destructured at L17 / L133 but never read downstream. Candidate cleanup.
- **Streak guard is `== 0`, not `> 0`:** a structurally-impossible negative `completedSp` would not break the streak.
- **Zero / empty scope:** `completedSp <= 0` → ratio `0m` (never null, never throws).

## Relationships

- `BugRatioAnalyzer` → `TransitionAttributionChecker`: delegates the "completed in sprint" determination and stage/window resolution.
- `BugRatioAnalyzer` → `SprintMembership.GetEffectiveSp`: effective-SP and `defaultSpPerBug` fallback live on the membership, not here.
- `BugRatioAnalyzer` → `MetricCardBuilder`: delta direction/polarity and card display formatting.

## Source

Original Fokus `fokus-spec.md` §Bug Ratio; ported implementation in `BugRatioAnalyzer`.
Rule verification: Mine→Verify pilot (Pass 2), 3 clean-room miners + Codex refutation, 2026-06-14.

---
<!-- status: mutation-gated — 100% reachable kill, floor 75 cleared, 0 candidate bugs (2026-06-22) -->
<!-- mutation-gated: true -->
<!-- last-stryker-run: 2026-06-22 — 100% reachable kill, floor 75 cleared, 0 candidate bugs -->
