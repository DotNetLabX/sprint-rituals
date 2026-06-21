# Bug Cost & Disruption Split

**Feature Spec:** `docs/features/BugCostDisruption/spec.md`

## Context

Bug tickets in Jira are rarely estimated, making SP-based metrics misleadingly low. This feature adds a configurable "default SP per bug" fallback to AppSettings and splits the Dashboard's single disruption rate card into two: scope disruption and bug disruption. The default SP applies system-wide across all SP calculations (dashboard, throughput, scope change, carry-over, bug ratio, epic progress).

**Services impacted:** Fokus (sole service). Changes span domain model, 5 analytics services, 2 settings endpoints, the dashboard frontend, the settings frontend, and frontend types/stores.

## Scope

**In scope:**
- `DefaultSpPerBug` property on AppSettings (0-13, default 3)
- Centralized SP substitution method for bug tickets with null/zero SP
- Disruption rate split into scope disruption + bug disruption on Dashboard
- Dashboard metric card grid expansion from 4 to 5 columns
- Default SP fallback applied across SprintSummaryService, ScopeChangeService, CarryOverService, BugRatioService, DeveloperThroughputService, EpicProgressService
- Settings UI field for default SP per bug
- Frontend type and store updates

**Out of scope:**
- Per-issue-type default SP
- Sprints page disruption split (retains single rate)
- Historical default SP tracking
- Variable default by sprint
- Bug severity weighting
- Cycle Time SP changes

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | domain-patterns | Follow | AppSettings entity, `DefaultSpPerBug` property, int 0-13 default 3 | |
| 2 | (none) | -- | Centralized `GetEffectiveSp` method on SprintMembership partial class | Log: no skill for adding behavior methods to existing entities |
| 3 | persistence-patterns | Follow | EF migration for new DefaultSpPerBug column | |
| 4 | create-feature | Follow | SaveSettingsCommand/GetSettingsResponse gain `DefaultSpPerBug`, validator rule | |
| 5 | (none) | -- | SprintSummaryService split disruption into scope + bug | |
| 6 | (none) | -- | ScopeChangeService apply effective SP | |
| 7 | (none) | -- | CarryOverService apply effective SP | |
| 8 | (none) | -- | BugRatioService apply effective SP | |
| 9 | (none) | -- | DeveloperThroughputService apply effective SP | |
| 10 | (none) | -- | EpicProgressService apply effective SP with precedence over imputation | |
| 11 | vue-patterns | Follow | MetricsResult type gains 5th card, MetricCard tooltip update, grid 4->5 cols | |
| 12 | vue-patterns | Follow | SettingsView adds DefaultSpPerBug field, settingsStore default | |

## Domain Model Changes

**Modified: `AppSettings`** (`src/Services/Fokus/Fokus.Domain/Settings/AppSettings.cs`)
- Add property: `public int DefaultSpPerBug { get; set; } = 3;`
- Update `CreateDefault()` to include `DefaultSpPerBug = 3`

**Modified: `SprintMembership` (partial behavior class)** (`src/Services/Fokus/Fokus.Domain/Sprint/Behaviors/SprintMembership.cs`)
- Add centralized method: `GetEffectiveSp(int defaultSpPerBug)` -- returns `StoryPoints` if non-null and non-zero, else returns `defaultSpPerBug` if ticket is a Bug and defaultSpPerBug > 0, else returns `null`. This is the single shared method all analytics services call.

## Data Model Changes

- New column `DefaultSpPerBug` (int, default 3) on `AppSettings` table
- EF migration required

## Implementation Steps

**Cross-cutting substitution rule (applies to Steps 5-10, 13):** Every `m.StoryPoints.HasValue` filter in analytics services must become `m.GetEffectiveSp(defaultSpPerBug).HasValue`, and every `m.StoryPoints!.Value` must become `m.GetEffectiveSp(defaultSpPerBug)!.Value` (or equivalent via a local variable). The only exception is BugRatioService which uses the `?? 0m` pattern instead of `HasValue` guards -- see Step 8 for that service's specific substitution.

### Step 1: Add DefaultSpPerBug to AppSettings domain entity

**Files to modify:**
- `src/Services/Fokus/Fokus.Domain/Settings/AppSettings.cs`

Add `DefaultSpPerBug` property (int, default 3) alongside existing properties. Update `CreateDefault()`.

Follow domain-patterns. Pattern example: existing properties in AppSettings like `BugRatioAlertThreshold`.

### Step 2: Add centralized GetEffectiveSp method

**Files to modify:**
- `src/Services/Fokus/Fokus.Domain/Sprint/Behaviors/SprintMembership.cs`

Add instance method `GetEffectiveSp(int defaultSpPerBug)` to the partial `SprintMembership` class:
- If `StoryPoints` has a value and is > 0, return `StoryPoints`
- If `Ticket?.IssueType == "Bug"` and `defaultSpPerBug > 0`, return `(decimal)defaultSpPerBug`
- Otherwise return `null`

This centralizes BR1 (fallback not override), BR4 (only bugs), BR3 (0 disables), BR12 (committed bugs too). Every analytics service calls this instead of reading `m.StoryPoints` directly for SP sums.

**Depends on:** Step 1 (needs to know the parameter type).

### Step 3: Add EF migration for DefaultSpPerBug

**Files to modify:**
- `src/Services/Fokus/Fokus.Persistence/` (new migration file auto-generated)

Follow persistence-patterns.

```bash
dotnet ef migrations add AddDefaultSpPerBug -p Services/Fokus/Fokus.Persistence -s Services/Fokus/Fokus.API
```

No seed data changes needed -- existing row will get the default value of 3 from the column default.

**Depends on:** Step 1.

### Step 4: Update Settings endpoints and validator

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Settings/GetSettings/GetSettingsQuery.cs` -- add `DefaultSpPerBug` to `GetSettingsResponse`
- `src/Services/Fokus/Fokus.API/Features/Settings/GetSettings/GetSettingsEndpoint.cs` -- map `DefaultSpPerBug` from settings entity
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveSettings/SaveSettingsCommand.cs` -- add `DefaultSpPerBug` to `SaveSettingsCommand` (default 3), add validator rule `InclusiveBetween(0, 13)`
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveSettings/SaveSettingsEndpoint.cs` -- add `DefaultSpPerBug = command.DefaultSpPerBug` to the `new AppSettings { ... }` object initializer in `HandleAsync`. **Critical:** The endpoint uses a full-replacement pattern (constructs a new `AppSettings` from command fields). Any unmapped property silently resets to its default. Omitting `DefaultSpPerBug` would reset it to 3 on every save.

Follow create-feature for the validator pattern. Pattern example: `BugRatioAlertThreshold` validation in `SaveSettingsCommandValidator`.

**Depends on:** Step 1.

### Step 5: Split disruption rate in SprintSummaryService

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs`

This is the largest step. Changes:

1. **Add `int defaultSpPerBug` parameter** to `ComputeSummary` signature. The endpoint already passes `settings` -- extract `settings.DefaultSpPerBug` and pass it through.

2. **Update `ComputeMetrics`** to use `GetEffectiveSp(defaultSpPerBug)` instead of `m.StoryPoints`. Replace every `m.StoryPoints.HasValue` + `m.StoryPoints!.Value` pattern with: `var sp = m.GetEffectiveSp(defaultSpPerBug)` then `sp.HasValue` / `sp.Value`. This affects committed, completed, added, carryOver sums.

3. **Split `SpAdded` into `SpAddedScope` and `SpAddedBug`:**
   - `SpAddedScope` = sum of effective SP for non-bug mid-sprint additions (`!m.WasCommitted && m.RemovedAt == null && !IsBug(m)`)
   - `SpAddedBug` = sum of effective SP for bug mid-sprint additions (`!m.WasCommitted && m.RemovedAt == null && IsBug(m)`)
   - Add `IsBug(SprintMembership m)` helper: `m.Ticket?.IssueType == "Bug"`

4. **Compute two disruption rates:**
   - `ScopeDisruptionRate = committed > 0 ? spAddedScope / committed * 100 : 0`
   - `BugDisruptionRate = committed > 0 ? spAddedBug / committed * 100 : 0`

5. **Update `SprintMetrics` record** to replace `DisruptionRate` with `ScopeDisruptionRate` and `BugDisruptionRate`. Add `SpAdded` (total = scope + bug) for any code that needs total added.

6. **Update `MetricsResult` record** to have 5 properties: `SpCompleted`, `CompletionRate`, `ScopeDisruptionRate`, `BugDisruptionRate`, `CarryOverRate`.

7. **Build two sparklines** -- one for scope disruption, one for bug disruption. Update `ComputeDisruptionRate` to split into `ComputeScopeDisruptionRate` and `ComputeBugDisruptionRate` (or a combined method returning both).

8. **Build two metric cards** replacing the single DisruptionRate card:
   - `ScopeDisruptionRate` card: name "Scope Disruption Rate", polarity "positive-down"
   - `BugDisruptionRate` card: name "Bug Disruption Rate", polarity "positive-down"

9. **Health score unchanged in formula.** Pass total disruption rate (scope + bug) to `ComputeHealthScore`. The `DisruptionRate` in `SprintMetrics` should still carry the combined total for health scoring.

10. **Update `ComputeSpCompleted`, `ComputeCompletionRate`, `ComputeCarryOverRate`** helper methods to accept `defaultSpPerBug` and use `GetEffectiveSp`.

11. **Update `BuildSparkline`** to pass `defaultSpPerBug` through the value selector chain.

12. **Update `ComputeFlags` (MidSprintDisruption)** -- the `m.StoryPoints.HasValue` check should use `GetEffectiveSp` so mid-sprint disruption SP totals include default SP.

13. **Update `ComputeLeaderboard` and `ComputeTopEpics`** -- use `GetEffectiveSp` for SP sums so leaderboard and top epics reflect default SP.

**Important:** Do NOT add excluded-from-scope filtering to SprintSummaryService.ComputeMetrics. The existing dashboard pattern intentionally omits excluded-from-scope filtering (unlike ScopeChangeService which does apply it). This must remain consistent -- the dashboard shows raw committed/added/completed sums without exclusion.

**Depends on:** Steps 1, 2.

### Step 6: Apply effective SP in ScopeChangeService

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/ScopeChangeService.cs`

1. Add `int defaultSpPerBug` parameter to `ComputeMultiSprint` and `ComputeSingleSprint`.
2. Replace all `m.StoryPoints.HasValue` / `m.StoryPoints!.Value` with `GetEffectiveSp(defaultSpPerBug)` in:
   - `ComputeSprintMetrics` (committedSpActive, committedSpTotal, addedSp, removedSp, completedSp)
   - `BuildBurnupData` (starting committed, added today, removed today, completed today)
   - `BuildEventTable` (event StoryPoints display -- show effective SP)
   - `GetMidSprintAdditions` / `BuildClassificationBreakdown` (spTotal per category)
3. Update callers (endpoints) to pass `settings.DefaultSpPerBug`.

Note: The Sprints page disruption rate remains a single combined rate (BR17). The service does not split scope vs bug disruption. The numbers just become larger when unestimated bugs carry default SP.

**Depends on:** Steps 1, 2.

### Step 7: Apply effective SP in CarryOverService

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/CarryOverService.cs`

1. Add `int defaultSpPerBug` parameter to `ComputeMultiSprint` and `ComputeSingleSprint`.
2. Replace all `m.StoryPoints.HasValue` / `m.StoryPoints!.Value` with `GetEffectiveSp(defaultSpPerBug)` in:
   - `ComputeCarryOverMetrics` (carryOverSp, committedSpActive, addedSp)
   - `ComputePerSprintData`
   - `BuildStatusDistribution` (spTotal per stage)
   - `BuildIssueTypeBreakdown` (spTotal per type)
   - `BuildCarryOverDestination` (priorCarryOverSp, bucket SP sums)
3. For `CarryOverTicketEntry.StoryPoints` -- show effective SP (not raw) so the table reflects default SP. Use `m.GetEffectiveSp(defaultSpPerBug)`.
4. Update callers (endpoints) to pass `settings.DefaultSpPerBug`.

**Depends on:** Steps 1, 2.

### Step 8: Apply effective SP in BugRatioService

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/BugRatioService.cs`

1. Add `int defaultSpPerBug` parameter to `ComputeMultiSprint` and `ComputeSingleSprint`.
2. Replace all `m.StoryPoints ?? 0m` and `m.StoryPoints.HasValue` patterns with `GetEffectiveSp(defaultSpPerBug)` in:
   - Team-level per-sprint trend (bugSp, nonBugSp)
   - Per-developer sprint breakdowns (bugSp, nonBugSp)
   - Single-sprint team metrics
   - Single-sprint per-developer metrics and deltas
   - Alert evaluation (`EvaluateAlert`)
   - `BuildIssueTypeBreakdown` (spTotal)
3. Update callers (endpoints) to pass `settings.DefaultSpPerBug`.

**BugRatioService substitution pattern:** This service uses `m.StoryPoints ?? 0m` (not the `HasValue` guard used elsewhere). The substitution becomes `m.GetEffectiveSp(defaultSpPerBug) ?? 0m` -- the `?? 0m` fallback is still needed because `GetEffectiveSp` returns `null` for non-bug tickets without SP. The `CompletedMemberships` filter does not filter by SP presence (it filters by done status + not removed + not excluded), so tickets with null effective SP will hit the `?? 0m` path and contribute 0, which is correct.

**Depends on:** Steps 1, 2.

### Step 9: Apply effective SP in DeveloperThroughputService

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/DeveloperThroughputService.cs`

1. Add `int defaultSpPerBug` parameter to `ComputeThroughput`.
2. Replace all `m.StoryPoints.HasValue` / `m.StoryPoints!.Value` with `GetEffectiveSp(defaultSpPerBug)` in:
   - Per-sprint breakdown (spAssigned, spCompleted)
   - Prior sprint delta computation
   - `ComputeRollingAverage` (spCompleted sum)
3. Update callers (endpoint) to pass `settings.DefaultSpPerBug`.

Note: DeveloperThroughputService does NOT apply ExcludedFromScopeStatuses (KB confirms this). The default SP substitution still applies -- it is independent of exclusion logic.

**Depends on:** Steps 1, 2.

### Step 10: Apply effective SP in EpicProgressService with precedence

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/EpicProgressService.cs`

1. Add `int defaultSpPerBug` parameter to `ComputeEpicProgress`.
2. For bug-type tickets (`t.IssueType == "Bug"`): apply default SP substitution before imputation.
   - When computing `totalSp`, `doneSp`, `remainingSp`: for bug tickets, use effective SP (actual if exists, else default if > 0, else null). For non-bug tickets, use `t.StoryPoints` as-is.
   - For imputation (BR16 in spec): when counting `unestimatedCount` and `remainingUnestimatedCount`, bugs that received default SP are treated as estimated -- they should NOT be counted as unestimated for imputation purposes. This prevents double-counting.
3. The EpicProgressService works with `Ticket` entities (not `SprintMembership`). Add a helper method `GetEffectiveTicketSp(Ticket t, int defaultSpPerBug)` that mirrors the logic: if `t.StoryPoints` has value and > 0, return it; if `t.IssueType == "Bug"` and default > 0, return default; else null.
4. Update callers (endpoint) to pass `settings.DefaultSpPerBug`.

**Depends on:** Steps 1, 2.

### Step 11: Update frontend types, dashboard store, and dashboard view

**Files to modify:**
- `client/src/types/index.ts` -- update `MetricsResult` interface: replace `disruptionRate: MetricCard` with `scopeDisruptionRate: MetricCard` and add `bugDisruptionRate: MetricCard`
- `client/src/views/DashboardView.vue` -- update metric card grid from `lg:grid-cols-4` to `lg:grid-cols-5`; render 5 cards: `spCompleted`, `completionRate`, `scopeDisruptionRate`, `bugDisruptionRate`, `carryOverRate`
- `client/src/components/dashboard/MetricCard.vue` -- update `metricTooltip` function: replace the `Disruption Rate` case with two cases:
  - `Scope Disruption Rate`: "Non-bug work added mid-sprint as % of committed SP. Lower is better."
  - `Bug Disruption Rate`: "Bug work added mid-sprint as % of committed SP. Lower is better."
- `client/src/components/dashboard/HealthScoreBadge.vue` -- no changes needed (health score structure unchanged)

Follow vue-patterns for reactive state and template patterns.

**Layout note:** The grid changes from `grid-cols-2 lg:grid-cols-4` to `grid-cols-2 lg:grid-cols-5`. On screens below 1440px, the 5 cards will wrap to a second row (3+2), which matches the spec's acceptable behavior.

**Depends on:** Step 5 (backend response shape must be final).

### Step 12: Update frontend settings types, store, view, and API

**Files to modify:**
- `client/src/types/index.ts` -- add `defaultSpPerBug: number` to `AppSettings` interface
- `client/src/stores/settingsStore.ts` -- add `defaultSpPerBug: 3` to the default settings object
- `client/src/views/SettingsView.vue` -- add `defaultSpPerBug` to the `form` reactive object (default 3), add to `syncFromStore()`, add a "Default SP per bug" number input field in the settings form (in a new section or alongside Bug Ratio Alerts), range 0-13
- `client/src/api/settings.ts` -- no changes needed (uses generic `AppSettings` type, serializes entire object)

Follow vue-patterns for v-model binding and form patterns. Pattern example: the `bugRatioAlertThreshold` field in SettingsView.vue.

**Depends on:** Step 4 (backend settings endpoint must accept the field).

### Step 13: Update all analytics endpoint callers

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetSprintSummary/GetSprintSummaryEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetScopeChange/GetScopeChangeEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetCarryOver/GetCarryOverEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetBugRatio/GetBugRatioEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetDeveloperThroughput/GetDeveloperThroughputEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetEpicProgress/GetEpicProgressEndpoint.cs`

Each endpoint already loads `settings` via `appSettingsRepository.GetAsync(ct)`. Add `settings.DefaultSpPerBug` as the new parameter to each service method call.

**Depends on:** Steps 5-10 (all service signatures updated).

### Step 14: Update KB entries

**Files to modify:**
- `docs/kb/domain/settings.md` -- add `DefaultSpPerBug` to property table
- `docs/kb/analytics/health-score.md` -- note disruption split (scope + bug) and that health score uses combined total
- `docs/kb/analytics/scope-change.md` -- note default SP substitution in formulas
- `docs/kb/analytics/bug-ratio.md` -- note default SP substitution in formulas
- `docs/kb/analytics/carry-over.md` -- note default SP substitution
- `docs/kb/analytics/throughput.md` -- note default SP substitution
- `docs/kb/analytics/epic-progress.md` -- note default SP precedence over imputation
- `docs/kb/cross-cutting.md` -- add section on default SP substitution pattern
- `docs/kb/frontend-map.md` -- update dashboard metrics section (5 cards)

**Depends on:** All previous steps.

## Cross-Service Changes

None. Single-service system.

## Migration Notes

```bash
dotnet ef migrations add AddDefaultSpPerBug -p Services/Fokus/Fokus.Persistence -s Services/Fokus/Fokus.API
dotnet ef database update -p Services/Fokus/Fokus.Persistence -s Services/Fokus/Fokus.API
```

No seed data changes -- the existing AppSettings row (Id=1) will get the column default of 3.

## Testing Strategy

1. **Settings round-trip:** Save defaultSpPerBug = 5, reload, verify it persists. Save 0, verify disabled behavior. Save 14, verify 400 response.
2. **Default SP substitution:** Create a sprint with a Bug ticket (null SP) and default SP = 3. Verify all analytics reflect 3 SP for that ticket.
3. **Override precedence:** Bug ticket with SP = 8 and default = 3. Verify 8 is used (not 3).
4. **Non-bug exclusion:** Story ticket with null SP and default = 3. Verify it remains excluded from SP metrics.
5. **Disruption split:** Sprint with 2 non-bug additions (5 SP each) and 3 bug additions (0 SP each, default = 3). Verify scope disruption = 10 / committed * 100, bug disruption = 9 / committed * 100.
6. **Health score invariant:** Verify scope disruption + bug disruption = total disruption rate used in health score.
7. **Dashboard layout:** Verify 5 cards render on 1440px+ screens. Verify wrap on smaller screens.
8. **Epic progress precedence:** Bug with null SP and default = 3. Verify epic progress uses 3 (not imputed average).
9. **Zero default:** Set default = 0. Verify all metrics match pre-feature behavior (unestimated bugs excluded).
10. **Carry-over impact:** Committed bug with null SP, not completed. Verify it contributes default SP to both carry-over numerator and denominator.

## Open Questions

None.
