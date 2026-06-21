# BugCostDisruption — Implementation

## Files Created

- `src/Services/Fokus/Fokus.Persistence/Migrations/20260510083320_AddDefaultSpPerBug.cs` — EF migration adding DefaultSpPerBug column (int, default 3) to AppSettings. Manually corrected defaultValue from 0 to 3 and added SQL seed to fix any existing row.
- `src/Services/Fokus/Fokus.Persistence/Migrations/20260510083320_AddDefaultSpPerBug.Designer.cs` — Auto-generated migration snapshot.

## Files Modified

- `src/Services/Fokus/Fokus.Domain/Settings/AppSettings.cs` — Added `DefaultSpPerBug` property (int, default 3) and seeded it in `CreateDefault()`.
- `src/Services/Fokus/Fokus.Domain/Sprint/Behaviors/SprintMembership.cs` — Added `GetEffectiveSp(int defaultSpPerBug)` method: returns StoryPoints if non-null and > 0, else defaultSpPerBug if Bug and default > 0, else null.
- `src/Services/Fokus/Fokus.API/Features/Settings/GetSettings/GetSettingsQuery.cs` — Added `DefaultSpPerBug` to `GetSettingsResponse`.
- `src/Services/Fokus/Fokus.API/Features/Settings/GetSettings/GetSettingsEndpoint.cs` — Added `DefaultSpPerBug = settings.DefaultSpPerBug` to mapping.
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveSettings/SaveSettingsCommand.cs` — Added `DefaultSpPerBug` to command with default 3 and validator rule `InclusiveBetween(0, 13)`.
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveSettings/SaveSettingsEndpoint.cs` — Added `DefaultSpPerBug = command.DefaultSpPerBug` to the full-replacement `new AppSettings { ... }` initializer.
- `src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs` — Split `DisruptionRate` into `ScopeDisruptionRate` + `BugDisruptionRate` in `MetricsResult` and `SprintMetrics`. All SP sums use `GetEffectiveSp(defaultSpPerBug)`. Health score uses combined total. Two metric cards built instead of one.
- `src/Services/Fokus/Fokus.API/Features/Analytics/ScopeChangeService.cs` — All SP sums use `GetEffectiveSp(defaultSpPerBug)` throughout `ComputeSprintMetrics`, `BuildClassificationBreakdown`, `BuildEventTable`, `BuildBurnupData`.
- `src/Services/Fokus/Fokus.API/Features/Analytics/CarryOverService.cs` — All SP sums use `GetEffectiveSp(defaultSpPerBug)` throughout `ComputeCarryOverMetrics`, `BuildStatusDistribution`, `BuildIssueTypeBreakdown`, `BuildCarryOverDestination`, zombie and ticket tables.
- `src/Services/Fokus/Fokus.API/Features/Analytics/BugRatioService.cs` — All SP sums use `GetEffectiveSp(defaultSpPerBug) ?? 0m`. Updated `EvaluateAlert` and `BuildIssueTypeBreakdown` with `defaultSpPerBug` parameter.
- `src/Services/Fokus/Fokus.API/Features/Analytics/DeveloperThroughputService.cs` — All SP sums use `GetEffectiveSp(defaultSpPerBug) ?? 0m`. `ComputeRollingAverage` accepts `defaultSpPerBug` parameter.
- `src/Services/Fokus/Fokus.API/Features/Analytics/EpicProgressService.cs` — Added local `GetEffectiveTicketSp(Ticket t, int defaultSpPerBug)` helper. All ticket SP sums use it. Velocity calculation uses `SprintMembership.GetEffectiveSp`. `unestimatedCount` now counts tickets where effective SP is null.
- `client/src/types/index.ts` — Split `MetricsResult.disruptionRate` into `scopeDisruptionRate` + `bugDisruptionRate`. Added `defaultSpPerBug: number` to `AppSettings`.
- `client/src/views/DashboardView.vue` — Grid changed from `lg:grid-cols-4` to `lg:grid-cols-5`. Renders 5 metric cards using new field names.
- `client/src/components/dashboard/MetricCard.vue` — Replaced `'Disruption Rate'` tooltip with two cases: `'Scope Disruption Rate'` and `'Bug Disruption Rate'`.
- `client/src/stores/settingsStore.ts` — Added `defaultSpPerBug: 3` to the store's default AppSettings ref.
- `client/src/views/SettingsView.vue` — Added `defaultSpPerBug: 3` to `form` reactive, added `form.defaultSpPerBug = s.defaultSpPerBug` in `syncFromStore`, added UI number input (0–13) in Bug Ratio Alerts section.
- `docs/kb/domain/settings.md` — Added `DefaultSpPerBug` row to properties table.
- `docs/kb/analytics/health-score.md` — Documented effective SP rule and split disruption card display.
- `docs/kb/analytics/scope-change.md` — Added Effective SP section.
- `docs/kb/analytics/bug-ratio.md` — Added effective SP note to core formulas.
- `docs/kb/analytics/carry-over.md` — Added Effective SP section.
- `docs/kb/analytics/throughput.md` — Updated SP formulas to reference effectiveSP.
- `docs/kb/analytics/epic-progress.md` — Documented `GetEffectiveTicketSp` helper and updated SP formulas.
- `docs/kb/cross-cutting.md` — Added DefaultSpPerBug cross-cutting rule section.
- `docs/kb/frontend-map.md` — Noted 5-card dashboard layout with split disruption cards.

## Review Cycle 1 Fixes

- `src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs` — [HIGH] Added `GetEffectiveTicketSp(Ticket t, int defaultSpPerBug)` private static helper (same logic as `EpicProgressService`). Fixed `ComputeTopEpics` second phase: replaced `Where(t => t.StoryPoints.HasValue)` filter with `Where(t => GetEffectiveTicketSp(t, defaultSpPerBug) != null)` and both `.Sum(t => t.StoryPoints!.Value)` calls with `.Sum(t => GetEffectiveTicketSp(t, defaultSpPerBug) ?? 0m)`.
- `client/src/components/dashboard/MetricCard.vue` — [LOW] Aligned tooltip text to spec verbatim: "Non-bug work added mid-sprint as % of committed SP. Lower is better." and "Bug work added mid-sprint as % of committed SP. Lower is better."

## Key Decisions

- `GetEffectiveSp` on `SprintMembership` (domain partial class) — keeps the rule co-located with the entity it operates on, consistent with the Behaviors folder pattern.
- `GetEffectiveTicketSp` as a private static on `EpicProgressService` — `Ticket` is a different entity type; the method is scoped to where it's needed rather than added to the domain.
- `EvaluateAlert` given a default parameter `int defaultSpPerBug = 0` — maintains backward compat if ever called without the setting.
- `BuildIssueTypeBreakdown` in BugRatio given a default parameter `int defaultSpPerBug = 0` — same rationale.
- Health score uses combined disruption total (unchanged behavior), only the display splits into two cards.
- EF migration manually corrected: defaultValue 0 → 3, plus UPDATE SQL for existing rows. EF always generates 0 for int columns.

## Deviations from Plan

- Step 13 required no code changes — all analytics services already accept the full `AppSettings` object and extract `DefaultSpPerBug` internally. No endpoint modifications were needed.
