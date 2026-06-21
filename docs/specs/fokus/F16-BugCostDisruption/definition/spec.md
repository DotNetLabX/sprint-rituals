# Bug Cost & Disruption Split

**Traces to:** `docs/product/v1.md` §5.2 (Disruption Analysis), §5.5 (Bug Ratio), §5.7 (Sprint Summary Card)
**Source:** Scratch
**Dependencies:** F3 (Settings System), F8 (Sprint Summary Card), F10 (Scope Change & Disruption), F13 (Bug Ratio)
**Status:** Done
**Plan:** `docs/plans/BugCostDisruption/plan.md`

---

## Purpose

Bug tickets in Jira are rarely estimated because they appear mid-sprint without a team estimation session. This makes SP-based metrics (disruption rate, bug ratio) misleadingly low — a sprint can have 23 completed bugs but only 5.0 SP total because most went unestimated. This feature introduces a configurable default SP per bug as a fallback for unestimated bugs, and splits the Dashboard's single disruption rate into two cards (scope disruption vs bug disruption) so Scrum Masters can see at a glance whether a sprint was disrupted by scope creep or by firefighting.

## Entities

**Extended: App settings**
- Default SP per bug: an integer (0-13, default 3). When a bug ticket has no story point estimate, this value is used as a fallback in all SP calculations. Setting to 0 disables the fallback (existing behavior: unestimated bugs excluded from SP metrics).

## User Flows

```
Flow 1: Configure Default SP Per Bug
1. User navigates to Settings
2. Under the metrics/thresholds area, a "Default SP per bug" field appears with the current value (default 3)
3. User changes the value (0-13 range, where 0 = disabled)
4. User saves settings
5. All analytics recalculate on next page load — bug tickets without estimates now carry the configured SP value in all metrics
```

```
Flow 2: View Split Disruption on Dashboard
1. User navigates to Dashboard
2. Five metric cards appear in a row: SP Completed, Completion %, Scope Disruption Rate, Bug Disruption Rate, Carry-Over Rate
3. Scope Disruption Rate shows the percentage of committed capacity consumed by non-bug mid-sprint additions
4. Bug Disruption Rate shows the percentage of committed capacity consumed by bug mid-sprint additions
5. Each card includes delta vs prior sprint and a 4-sprint sparkline
6. The health score badge still shows a single "Disruption" sub-score computed from the combined total disruption rate
7. User can see at a glance whether scope changes or bug load is the dominant disruption source
```

```
Flow 3: View Dashboard with Default SP Applied
1. User has configured default SP per bug = 3
2. A sprint has 10 bug tickets added mid-sprint, 8 with no SP estimate
3. Those 8 bugs now contribute 8 x 3 = 24 SP to the bug disruption numerator
4. Bug Disruption Rate reflects the actual bug cost, not just the 2 estimated bugs
5. The combined total disruption (scope + bug) feeds the health score
```

```
Flow 4: View Bug Ratio with Default SP Applied
1. User navigates to Developers > Bug Ratio tab
2. A developer completed 5 unestimated bugs and 3 stories (total 21 SP)
3. With default SP = 3, those 5 bugs count as 15 bug SP
4. The developer's bug ratio shows 15 / (15 + 21) = 41.7% instead of 0%
5. Team-level bug ratio also reflects the default SP fallback
```

## API Surface

No new endpoints. Existing endpoints return modified responses.

| Method | Route | Auth | Changes | Status codes |
|--------|-------|------|---------|--------------|
| GET | /api/analytics/sprint-summary | None | MetricsResult gains a fifth named property; existing DisruptionRate renamed | 200, 400 |
| GET | /api/settings | None | Response includes `defaultSpPerBug` field | 200 |
| PUT | /api/settings | None | Accepts `defaultSpPerBug` field | 200, 400 |

**Sprint summary response changes:**

The `MetricsResult` record currently has four named properties (`SpCompleted`, `CompletionRate`, `DisruptionRate`, `CarryOverRate`). This feature renames and adds:

- `SpCompleted` — unchanged
- `CompletionRate` — unchanged
- `ScopeDisruptionRate` — renamed from `DisruptionRate`. Formula: (non-bug added SP / committed SP) x 100
- `BugDisruptionRate` — new property. Formula: (bug added SP / committed SP) x 100
- `CarryOverRate` — unchanged

Each disruption card includes: value, display value (formatted percentage), delta vs prior sprint, delta direction, delta polarity, sparkline data (4-sprint trailing window).

The `healthScore` object is unchanged in structure. The `disruptionSubScore` uses the combined total disruption rate (scope + bug).

**Dashboard tooltip text for the new cards:**
- Scope Disruption Rate: "Non-bug work added mid-sprint as % of committed SP. Lower is better."
- Bug Disruption Rate: "Bug work added mid-sprint as % of committed SP. Lower is better."

**Dashboard layout:** The metric card grid changes from 4 columns to 5 columns on large screens (1440px+). On laptop screens (1366px), cards may wrap to a second row (3+2) if 5-across is too narrow.

**Settings response changes:**

The `defaultSpPerBug` field is included alongside existing settings fields. Stored as an integer (0-13), participates in decimal SP arithmetic at query time.

**Error conditions (settings):**
- 400: `defaultSpPerBug` outside 0-13 range

## Business Rules

1. **Default SP is a fallback, not an override.** When a bug ticket (IssueType = "Bug") has no story point estimate (null or zero), the configured default SP per bug is used in all SP calculations. When a bug has an actual estimate, the actual value is used.

2. **Default SP applies system-wide.** The fallback affects every SP calculation across all features: dashboard metrics (F8), developer throughput (F9), scope change metrics (F10), carry-over (F11), bug ratio (F13). This ensures consistent numbers across all pages. F12 (Cycle Time) is not affected — it measures calendar duration, not SP aggregates. F14 (Epic Progress) is not affected — it has its own imputation mechanism for unestimated tickets; the default SP per bug takes precedence for bug-type tickets, so Epic Progress treats bugs with the default as "estimated" and skips its own imputation for those tickets.

3. **Default SP of 0 disables the fallback.** When set to 0, unestimated bug tickets remain excluded from SP metrics, preserving the existing behavior. This is the opt-out mechanism.

4. **Only bug tickets receive the default.** Non-bug tickets (Story, Task, Sub-task, Improvement, etc.) with no story points remain excluded from SP metrics as before. The fallback is specific to bug-type tickets.

5. **Scope disruption rate = non-bug added SP / committed SP x 100.** The numerator includes SP from all mid-sprint additions where IssueType is not "Bug": scope injection, priority escalation, and planning overflow items that are not bugs.

6. **Bug disruption rate = bug added SP / committed SP x 100.** The numerator includes SP from bug-type tickets added mid-sprint (not committed, not removed, not excluded-from-scope). With the default SP fallback, previously-invisible unestimated bugs now contribute to this number.

7. **Scope disruption + bug disruption = total disruption rate.** The two rates are an additive decomposition of the existing disruption rate formula. Their sum equals the total disruption rate. This is guaranteed by the partitioning: every mid-sprint addition is either a bug or not a bug.

8. **Health score uses combined total disruption.** The disruption sub-score in the health score composite uses the total disruption rate (scope + bug combined). No change to health score weights, thresholds, or computation. The split is a display concern only.

9. **Sparklines track respective rates independently.** The scope disruption card's sparkline shows the 4-sprint trailing window of scope disruption rate values. The bug disruption card's sparkline shows the 4-sprint trailing window of bug disruption rate values.

10. **Delta polarity: lower is better for both.** Both scope disruption and bug disruption rates follow the same polarity as the original disruption rate: green down, red up.

11. **Default SP applies to completed bug metrics too.** In the bug ratio feature (F13), unestimated completed bugs now carry the default SP. This changes bug ratio percentages for developers who fix unestimated bugs.

12. **Default SP applies to committed bugs.** If a committed bug (present at sprint start) has no estimate, it also receives the default SP. The fallback applies to all bug SP calculations uniformly, not just mid-sprint additions.

13. **Excluded-from-scope filtering still applies.** The default SP substitution occurs before excluded-from-scope filtering. A bug that receives the default SP but has a final status matching an excluded-from-scope status is still excluded from all metric calculations. The default SP does not override the exclusion.

14. **Carry-over rate is affected in both numerator and denominator.** The carry-over rate denominator (committed SP + added SP) and numerator (carry-over SP) both change when unestimated bugs receive the default. A committed bug that doesn't complete increases both the denominator and the numerator. The net effect on the carry-over rate depends on the existing ratio.

15. **Developer throughput (F9) reflects the default.** SP Assigned, SP Completed, and Completion % on the Developers > Throughput tab include the default SP for unestimated bugs. A developer assigned 5 unestimated bugs sees those reflected in their throughput numbers.

16. **Default SP takes precedence over Epic Progress imputation.** Epic Progress (F14) imputes SP for unestimated tickets using the epic's average. For bug-type tickets, the default SP per bug is applied first — Epic Progress treats those bugs as estimated and does not apply its own imputation.

17. **Sprints page layout unchanged.** The Sprints page (F10) retains its single disruption rate card and 4-category classification breakdown. The default SP fallback changes the Sprints page numbers (disruption rate increases when unestimated bugs carry SP), but the page structure and card count remain as-is.

18. **Division by zero: committed SP = 0 produces 0 for both rates.** When committed SP is zero, both scope disruption rate and bug disruption rate are 0%.

19. **The setting is applied at query time, not stored.** The default SP is not written back to the ticket's story points. It is used as a substitution during metric computation. Changing the setting retroactively changes all historical metrics. The substitution should be centralized — a single shared method that all analytics services call to get the effective SP for a membership, so all features apply it consistently.

## Acceptance Criteria

- [ ] Settings page shows "Default SP per bug" field with current value (default 3)
- [ ] Default SP per bug accepts integer values 0-13
- [ ] Setting to 0 disables the fallback (unestimated bugs excluded from SP metrics, matching existing behavior)
- [ ] PUT /api/settings with `defaultSpPerBug` outside 0-13 returns 400
- [ ] Bug tickets with no estimate use the default SP value in all SP calculations when default > 0
- [ ] Bug tickets with an actual estimate use the actual value (default is not applied)
- [ ] Non-bug tickets with no estimate remain excluded from SP metrics regardless of default
- [ ] Default SP applies consistently across dashboard (F8), developer throughput (F9), scope change (F10), carry-over (F11), and bug ratio (F13)
- [ ] Dashboard shows 5 metric cards: SP Completed, Completion %, Scope Disruption Rate, Bug Disruption Rate, Carry-Over Rate
- [ ] Dashboard metric card grid accommodates 5 cards (5-across on 1440px+, wraps on smaller screens)
- [ ] MetricsResult record has 5 named properties: SpCompleted, CompletionRate, ScopeDisruptionRate, BugDisruptionRate, CarryOverRate
- [ ] Scope Disruption Rate = (non-bug added SP / committed SP) x 100
- [ ] Bug Disruption Rate = (bug added SP / committed SP) x 100
- [ ] Scope Disruption Rate + Bug Disruption Rate = total disruption rate
- [ ] Each disruption card shows delta vs prior sprint with polarity: lower is better (green down, red up)
- [ ] Each disruption card shows a 4-sprint trailing sparkline for its respective rate
- [ ] Scope Disruption Rate tooltip: "Non-bug work added mid-sprint as % of committed SP. Lower is better."
- [ ] Bug Disruption Rate tooltip: "Bug work added mid-sprint as % of committed SP. Lower is better."
- [ ] Health score uses combined total disruption rate (scope + bug) with no change to scoring formula
- [ ] Health score badge shows a single "Disruption" sub-score using the combined rate
- [ ] GET /api/analytics/sprint-summary returns MetricsResult with 5 named properties
- [ ] GET /api/settings includes `defaultSpPerBug` in the response
- [ ] Developer throughput (F9) SP Assigned, SP Completed, and Completion % reflect the default SP fallback
- [ ] Sprints page disruption rate and classification breakdown numbers reflect the default SP fallback
- [ ] Sprints page layout and card count remain unchanged
- [ ] Bug ratio (F13) per-developer and team metrics reflect the default SP fallback
- [ ] Committed bugs with no estimate also receive the default SP
- [ ] Bugs with excluded-from-scope final statuses are still excluded from metrics even when they receive default SP
- [ ] Epic Progress (F14) does not apply its own imputation to bugs that received the default SP
- [ ] Default SP substitution is centralized (single shared method used by all analytics services)

## Out of Scope

- **Per-issue-type default SP** — only bugs get a default. A generalized "default SP by type" mapping adds configuration complexity without matching the core problem (bugs are uniquely under-estimated because they appear mid-sprint without team estimation).
- **Sprints page disruption split** — the Sprints page retains a single disruption rate. Its classification breakdown (planning overflow, unplanned bug, scope injection, priority escalation) already provides the granularity at the detail level.
- **Historical default SP tracking** — the default is applied at query time. Changing it retroactively changes all historical metrics. Tracking "what the default was per sprint" adds complexity for a rough estimation tool.
- **Variable default by sprint** — no per-sprint override. One global setting applies across all sprints.
- **Bug severity weighting** — all unestimated bugs get the same default SP regardless of priority. Modeling P1 bugs as more costly than P3 is deferred.
- **Cycle Time (F12) SP changes** — Cycle Time measures calendar duration, not SP aggregates. Its scatter plot uses SP for bubble sizing but does not aggregate SP. No changes needed.
