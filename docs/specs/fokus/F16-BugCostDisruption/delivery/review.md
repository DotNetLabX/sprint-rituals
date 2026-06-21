# Bug Cost & Disruption Split — Review

## Reviewed By
`reviewer` (Sonnet agent, cycle 1 → cycle 2)

## Verdict: APPROVE

### Cycle 2 re-review (fixes verified)

Both cycle 1 findings were fixed correctly:

**[HIGH] ComputeTopEpics — FIXED.** `GetEffectiveTicketSp(Ticket t, int defaultSpPerBug)` private static added at `SprintSummaryService.cs:421-428`. Logic is identical to `EpicProgressService`'s helper. Second phase now filters with `GetEffectiveTicketSp(t, defaultSpPerBug) != null` (line 463) and sums via `GetEffectiveTicketSp(t, defaultSpPerBug) ?? 0m` (lines 468, 470). No regressions introduced.

**[LOW] MetricCard tooltips — FIXED.** Strings at `MetricCard.vue:12-13` now match spec verbatim: "Non-bug work added mid-sprint as % of committed SP. Lower is better." and "Bug work added mid-sprint as % of committed SP. Lower is better."

**Build (cycle 2):** `dotnet build Fokus.API.csproj --no-incremental -o build-review-tmp` → 0 errors, 3 warnings (all pre-existing, unchanged from cycle 1).

---

## Cycle 1 record (preserved)

## Pre-commitment Predictions

| Predicted risk | Actual finding |
|---|---|
| `GetEffectiveSp` null/zero/bug ordering edge cases | No issue — logic is correct: positive SP first, then bug-type fallback, then null |
| SprintSummaryService health score combined total correctness | No issue — `DisruptionRate = scopeDisruptionRate + bugDisruptionRate` and passed to `ComputeHealthScore` unchanged |
| EpicProgressService `unestimatedCount` may double-count bugs | No issue — `unestimatedCount = tickets.Count(t => GetEffectiveTicketSp(t, defaultSpPerBug) == null)` correctly uses the helper |
| Settings full-replacement pattern missing DefaultSpPerBug | No issue — SaveSettingsEndpoint.cs:25 maps it correctly |
| Migration defaultValue 0 vs 3 | Correctly handled — developer manually corrected to 3 and added UPDATE SQL |

Bonus finding not predicted: `ComputeTopEpics` in SprintSummaryService does not apply effective SP to the epic-level completion calculation from Ticket entities (raw `StoryPoints` still used for `totalSp`/`doneSp`).

## Findings

### [HIGH] ComputeTopEpics uses raw StoryPoints for epic completion — plan required GetEffectiveTicketSp
**File:** `src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs:452-459`
**Issue:** The second phase of `ComputeTopEpics` computes `doneSp` and `totalSp` from `allEpicTickets` (Ticket entities) using raw `t.StoryPoints.HasValue` filtering and `t.StoryPoints!.Value` summation. This means bug tickets with no estimate are excluded from the epic completion percentage on the dashboard. Plan Step 13 explicitly states: "Update `ComputeLeaderboard` and `ComputeTopEpics` — use `GetEffectiveSp` for SP sums so leaderboard and top epics reflect default SP." The `EpicProgressService.GetEffectiveTicketSp` private static was created exactly for this pattern, but it lives in a different class and cannot be accessed from `SprintSummaryService`.

```csharp
// Current — line 452-459: uses raw StoryPoints, excludes unestimated bugs
var epicTickets = filteredEpicTickets
    .Where(t => t.EpicKey == e.EpicKey && t.StoryPoints.HasValue)  // ← excludes bugs with null SP
    .ToList();

var doneSp = epicTickets
    .Where(t => doneStatuses.Contains(t.CurrentStatus))
    .Sum(t => t.StoryPoints!.Value);  // ← raw SP, no default

var totalSp = epicTickets.Sum(t => t.StoryPoints!.Value);  // ← raw SP, no default
```

**Fix:** Add a private static `GetEffectiveTicketSp(Ticket t, int defaultSpPerBug)` helper to `SprintSummaryService` (same logic as the one in `EpicProgressService`), then replace the `Where(t => t.StoryPoints.HasValue)` filter with `Where(t => GetEffectiveTicketSp(t, defaultSpPerBug) != null)` and the `.Sum(t => t.StoryPoints!.Value)` calls with `.Sum(t => GetEffectiveTicketSp(t, defaultSpPerBug) ?? 0m)`. Also pass `defaultSpPerBug` into `ComputeTopEpics` (already done at the call site — the parameter is there).

### [LOW] MetricCard tooltip text differs from spec verbatim (architect noted, confirmed)
**File:** `client/src/components/dashboard/MetricCard.vue:12-13`
**Issue:** The spec (lines 85-86) specifies exact tooltip strings:
- Scope Disruption Rate: "Non-bug work added mid-sprint as % of committed SP. Lower is better."
- Bug Disruption Rate: "Bug work added mid-sprint as % of committed SP. Lower is better."

The implementation uses:
- Scope: "Percentage of committed SP added mid-sprint via scope changes (non-bug tickets). Lower is better."
- Bug: "Percentage of committed SP added mid-sprint via bug tickets. Lower is better."

Meaning is equivalent, wording differs. Architect flagged this as LOW severity in Step 1.
**Fix:** Optionally align with spec verbatim, or leave as-is since meaning is identical.

## Positive Observations

- `GetEffectiveSp` on `SprintMembership` is clean and correctly ordered: positive-value guard first, then bug-type fallback, then null. The `StoryPoints.Value > 0` guard (not just `HasValue`) correctly handles zero-SP edge cases as specified by BR1.
- All six analytics services (SprintSummary, ScopeChange, CarryOver, BugRatio, DeveloperThroughput, EpicProgress) uniformly extract `defaultSpPerBug = settings.DefaultSpPerBug` at the top of each public method — clean and consistent.
- The Step 13 deviation (no endpoint signature changes needed) is legitimate: services accept `AppSettings` directly and extract `DefaultSpPerBug` internally. This is a simpler and better pattern than passing the int through every method boundary.
- `SaveSettingsEndpoint.cs` correctly maps `DefaultSpPerBug = command.DefaultSpPerBug` in the full-replacement initializer — the highest-risk omission point was handled correctly.
- EF migration correctly corrects EF's auto-generated `defaultValue: 0` to `3` and includes an UPDATE SQL for existing rows. This is exactly right — EF always generates 0 for int columns.
- `EpicProgressService.GetEffectiveTicketSp` correctly applies the same logic as `SprintMembership.GetEffectiveSp`, and `unestimatedCount` correctly uses it so bugs that received a default are NOT counted as unestimated (preventing double-imputation — BR16 compliance).
- `BugRatioService.EvaluateAlert` correctly accepts `defaultSpPerBug` and applies it when evaluating consecutive sprint streaks.
- Dashboard grid correctly changed from `grid-cols-2 lg:grid-cols-4` to `grid-cols-2 lg:grid-cols-5` at DashboardView.vue:107.
- Health score correctly uses combined `DisruptionRate` (sum of scope + bug) — no formula change, just a display split.
- KB entries updated across all 9 files listed in the plan (Step 14 fully completed).

## Gaps

- `ComputeTopEpics` first phase (selecting sprint memberships, line 431) correctly uses `GetEffectiveSp` for the "SP completed this sprint" column. Only the second phase (epic overall completion from `allEpicTickets`) is affected. The gap is scoped to the completion bar and `totalSp`/`doneSp` fields in the Top Epics card.
- No test evidence for the disruption sum invariant (scope + bug = total). This is a spec requirement (BR7) but is provable by code inspection: `disruptionRate = scopeDisruptionRate + bugDisruptionRate` at SprintSummaryService.cs:257, with scope and bug partitioned by `IsBug(m)`.

## Open Questions

None. All CRITICAL/HIGH findings have HIGH confidence and clear evidence.

## Evidence

| Check | Result | Command | Output |
|---|---|---|---|
| Build | **pass** | `dotnet build Fokus.API.csproj -o build-review-tmp` | 0 errors, 3 warnings (all pre-existing: 2x NU1903 package vulnerability not introduced by this feature, 1x CS9107 in DeveloperRepository not introduced by this feature) |
| GetEffectiveSp logic | pass | code inspection | Correct: HasValue && > 0 → return StoryPoints; IsBug && default > 0 → return (decimal)default; else null |
| SaveSettings full-replacement | pass | code inspection | DefaultSpPerBug = command.DefaultSpPerBug present at SaveSettingsEndpoint.cs:25 |
| Validator range | pass | code inspection | InclusiveBetween(0, 13) at SaveSettingsCommand.cs:79 |
| Migration defaultValue | pass | code inspection | defaultValue: 3 at migration:18; UPDATE SQL at migration:20 |
| MetricsResult shape | pass | code inspection | 5 properties: SpCompleted, CompletionRate, ScopeDisruptionRate, BugDisruptionRate, CarryOverRate |
| Dashboard grid | pass | code inspection | `grid-cols-2 lg:grid-cols-5` at DashboardView.vue:107 |
| TS types updated | pass | code inspection | scopeDisruptionRate + bugDisruptionRate in MetricsResult; defaultSpPerBug in AppSettings |
| Step 13 deviation | justified | implementation.md | Services accept AppSettings directly; endpoint signatures unchanged; DefaultSpPerBug extracted internally |
| ComputeTopEpics raw SP | **FAIL** | SprintSummaryService.cs:452-459 | t.StoryPoints.HasValue filter + raw sum — no GetEffectiveTicketSp call |
