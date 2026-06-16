# Bug Ratio

Bug-vs-feature story-point ratios, multi-sprint aggregation, per-developer breakdowns, and the
consecutive-sprint bug-ratio alert. Computed by `BugRatioAnalyzer`.

Rules below are **verified** via the Mine→Verify pass (3 independent clean-room miners +
independent Codex refutation, 2026-06-14). Confidence is high: the three miners converged on the
same ~22 behaviors with zero contradictions.

## Rules

- BR1: `IsBug` — a membership is a bug iff `Ticket.IssueType == "Bug"` (case-SENSITIVE ordinal `==`); a null ticket → not a bug.
- BR2: Bug/Non-Bug SP = sum of `GetEffectiveSp(defaultSpPerBug) ?? 0m` over completed memberships, partitioned by `IsBug`; `completedSp = bugSp + nonBugSp`.
- BR3: Bug ratio % = `bugSp / completedSp * 100` when `completedSp > 0`, else `0m` (divide-by-zero guard) — for the per-sprint and per-developer outputs.
- BR4: Team and per-developer aggregate ratio is **ratio-of-totals** (`ΣbugSp / ΣcompletedSp × 100`), never the average of per-sprint ratios.
- BR5: Alert — over the developer's closed sprints most-recent-first, count the consecutive streak where `ratio >= threshold` (inclusive); the alert fires when the streak `>= consecutiveCount`. Both come from `settings.BugRatio`.
- BR6: A sprint with `completedSp == 0` **breaks** the streak (not skipped, not counted) — iteration stops at the first zero-SP sprint encountered most-recent-first.
- BR7: Completed set excludes `RemovedAt != null` and any `FinalStatus` in `excludedStatuses` (matched case-INSENSITIVELY, `OrdinalIgnoreCase`); final inclusion is `TransitionAttributionChecker.IsCompletedInSprint(...) == true`.
- BR8: Transition grouping by `TicketId` uses `OrdinalIgnoreCase`; a ticket with no transitions → empty list.
- BR9: Sub-team / assignee filters use case-SENSITIVE ordinal `==`; a blank `subTeam` disables the filter. `excludedDeveloperIds` drops matching assignees but keeps null-assignee memberships — and applies on the **team path only**, not per-developer rows or alert evaluation.
- BR10: Bug / Non-Bug ticket counts = `Count(IsBug)` / `Count(!IsBug)` over the completed set (complementary).
- BR11: Issue-type breakdown groups completed memberships by `IssueType` (null → `"Unknown"`, case-sensitive grouping), ordered by `TicketCount` descending (stable ties, no secondary key); the multi-sprint view pools all target sprints via `SelectMany`.
- BR12: Prior-sprint deltas are emitted only when a prior sprint exists; each delta is `current − prior`. Team-card polarity: Bug Ratio % and Bug SP = `positive-down`, Non-Bug SP = `positive-up`.
- BR13: Sprints are processed ascending `StartDate`; emitted SP/ratio figures are rounded to 1 dp via `Math.Round(x, 1)` (banker's rounding) — **except** single-sprint team cards, which carry raw decimals and round only on display (`0.#`).

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
<!-- status: verified — per-rule mutation gating pending Step 3 (Cover) -->
<!-- mutation-gated: false -->
<!-- last-stryker-run: none — Cover not yet run -->
