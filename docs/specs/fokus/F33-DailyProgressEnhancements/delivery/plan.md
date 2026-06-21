# Daily Progress Enhancements

**Feature Spec:** `docs/specs/F33-DailyProgressEnhancements/definition/spec.md`

## Context

F32 introduced per-developer daily progress cards for the active sprint. Three usability problems reduce the tab's value: (1) the "Behind Pace" alert banner lists everyone when everyone is behind — noise instead of signal, (2) progress cards show total SP but don't distinguish bugs from features, and (3) mini burnup charts are too small with rendering defects (inverted Y-axis potential, unpredictable zoom). This feature addresses all three by adding bug/feature SP breakdown, delta-based alerting (showing *change* not just position), and chart fixes.

**Services impacted:** Fokus (single service). Backend: modify `DeveloperProgressService` response records and computation. Frontend: modify `DailyProgressTab` alert banner and `DeveloperProgressCard` SP line + chart options.

## Scope

**In scope:**
- Backend: Add `featureCompletedSp` and `bugCompletedSp` fields to `DeveloperProgressEntry`
- Backend: Add `gapDelta` and `direction` fields to alert records; change alert inclusion to direction-based
- Backend: Recompute yesterday's stall state for `new-stall` / `stall-resolved` direction derivation
- Frontend: Bug/feature SP split line on developer cards
- Frontend: Restructure alert banner from "Behind Pace" to "Pace Changes" with two groups
- Frontend: Fix chart Y-axis, increase chart height, disable zoom on mini charts
- Frontend: Update TypeScript types to match extended response

**Out of scope:**
- Stacked burnup chart (bugs vs features as areas) — deferred to F34
- Configurable stall threshold — remains fixed at 2 business days
- Historical delta (cross-sprint comparison) — deferred to F34
- Per-card delta badge — banner provides scannable summary

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | (none) | -- | Extend DeveloperProgressService: bug/feature SP partition on completed memberships | Log: metric computation patterns gap |
| 2 | (none) | -- | Extend DeveloperProgressService: gap delta computation from daily breakdown, direction derivation, yesterday's stall recomputation, alert restructuring | Log: metric computation patterns gap |
| 3 | vue-patterns | Follow | Extend DeveloperProgressCard: add split line, fix chart options | |
| 4 | vue-patterns | Follow | Restructure DailyProgressTab alert banner: two groups, new title, sorting | |
| 5 | (none) | -- | Update TypeScript types to match extended backend response | |

## Domain Model Changes

None. No new entities, value objects, or domain events. This feature modifies response record shapes and computation logic only.

## Data Model Changes

None. No new tables, columns, or migrations.

## Implementation Steps

### Step 1: Add bug/feature SP breakdown to DeveloperProgressService

Extend the backend computation to partition completed SP into feature and bug categories per developer.

**No matching skill** -- metric computation patterns are a gap.

**Files:**
- Modify: `src/Services/Fokus/Fokus.API/Features/Analytics/DeveloperProgressService.cs`

**Record changes:**

Extend `DeveloperProgressEntry` with two new fields after `CompletedSp`:
- `FeatureCompletedSp` (decimal) -- cumulative SP completed for non-bug ticket types
- `BugCompletedSp` (decimal) -- cumulative SP completed for bug ticket types

**Computation rules:**

- Bug classification: `m.Ticket?.IssueType == "Bug"` (exact, case-sensitive). Same `IsBug` pattern used in `LeaderboardService` (line 287), `BugRatioService` (line 460), `SprintSummaryService` (line 290).
- The split covers completed SP only (not assigned SP). Partition the existing completed membership loop where `completedSp` is accumulated.
- SP attribution: same `m.GetEffectiveSp(settings.DefaultSpPerBug)` already used. Null-SP tickets excluded from both sums.
- Invariant: `featureCompletedSp + bugCompletedSp == completedSp` for every developer.
- Round both values to 1 decimal place, consistent with `completedSp` rounding.

**Accept:**
- `FeatureCompletedSp` and `BugCompletedSp` added to `DeveloperProgressEntry` record
- Invariant holds for all developers
- Bug classification matches Leaderboard/BugRatio (case-sensitive `"Bug"`)
- Null-SP tickets excluded from both sums but appear in `completedTickets` lists

**Pattern reference:** `src/Services/Fokus/Fokus.API/Features/Analytics/LeaderboardService.cs` lines 109-113 (bug/feature SP partition pattern)

**Dependencies:** None

---

### Step 2: Add delta-based alerting to DeveloperProgressService

Extend the backend to compute gap delta, direction, and restructure the alerts list from "behind pace only" to "pace changes" (any direction != stable).

**No matching skill** -- metric computation patterns are a gap.

**Files:**
- Modify: `src/Services/Fokus/Fokus.API/Features/Analytics/DeveloperProgressService.cs`

**Record changes:**

Replace `DeveloperProgressAlert` with an extended version adding two fields:
- `GapDelta` (decimal?) -- change in SP gap since previous day. Null on day 1.
- `Direction` (string) -- one of: `worsening`, `improving`, `stable`, `new-stall`, `stall-resolved`

Retain existing fields: `AccountId`, `DisplayName`, `AvatarUrl`, `GapSp`, `GapDays`.

**Computation rules:**

Gap delta (spec BR4-5):
- For each developer, the gap on any day N is: `expectedCumulativeSp[N] - cumulativeSp[N]` (positive = behind).
- `gapDelta = gap(currentDay) - gap(currentDay - 1)`. Positive = gap grew (fell further behind). Negative = gap shrank (caught up).
- Day 1 (or when `dailyBreakdown.Count < 2`): gapDelta is null.
- Round to 1 decimal place, consistent with F32's `paceGapSp` rounding.
- The daily breakdown array already contains both `CumulativeSp` and `ExpectedCumulativeSp` per day -- compute delta from those values directly.

Direction derivation (spec BR6):
- Requires comparing today's stall state to yesterday's stall state.
- **Yesterday's stall recomputation:** The existing `BuildStalledTickets` method uses `DateTime.UtcNow.Date` internally. Extract the date parameter so the method accepts a `DateTime referenceDate` instead of using `UtcNow` directly. Call it twice: once with today's date (existing behavior) and once with yesterday's date to get yesterday's stall set.
- `new-stall`: developer has at least one stalled ticket today that was NOT stalled yesterday (compare by ticket key).
- `stall-resolved`: developer had stalled tickets yesterday but has NONE today.
- When both a gap change and a stall change occur: stall signal takes priority (`new-stall` or `stall-resolved`). The `gapDelta` is still included for context.
- `worsening`: gapDelta > 0 and no stall change.
- `improving`: gapDelta < 0 and no stall change.
- `stable`: gapDelta == 0 (or null on day 1) and no stall change.

Alert inclusion (spec BR7):
- A developer appears in the alerts list when `direction != "stable"`.
- This replaces the current filter of `isBehindPace` only.
- Developers who are behind pace but whose gap did not change are NOT listed.
- The alert list now includes improving developers -- not just those who are behind.

Alert sorting (spec BR9):
- Two logical groups in order: Worsening/Stalled first, then Improving/Recovered.
- Within "worsening" group (`direction` is `worsening` or `new-stall`): sort by `gapDelta` descending (largest increase first). `new-stall` entries at the top of this group.
- Within "improving" group (`direction` is `improving` or `stall-resolved`): sort by `gapDelta` ascending (largest improvement first, i.e. most negative first). `stall-resolved` entries at the top of this group.
- Implement sorting in the service so the frontend can render in order without re-sorting.

Grace period (spec BR6 -- unchanged):
- During grace period (`currentDay <= 2`): alerts list empty, same as F32.

Day 3 baseline (spec BR8):
- On day 3 (first day after grace period): `gapDelta = gap(day3) - gap(day2)`. The underlying gap data exists during the grace period -- grace period only suppresses alert display, not computation.

**Accept:**
- `GapDelta` and `Direction` fields added to `DeveloperProgressAlert` record
- `BuildStalledTickets` accepts a `referenceDate` parameter instead of using `UtcNow` internally
- Direction derivation follows priority: stall change > gap change > stable
- Alert list includes all developers with `direction != "stable"` (not just behind-pace)
- Alerts sorted: worsening/stalled group first (stalls at top, then by gapDelta desc), then improving/recovered group (stall-resolved at top, then by gapDelta asc)
- Grace period still suppresses alerts entirely
- Day 1: gapDelta is null, direction is stable, no alert entry
- `stall-resolved` appears in Improving group even if gapDelta > 0
- `new-stall` appears in Worsening group even if gapDelta < 0

**Pattern reference:** Existing `BuildStalledTickets` in `DeveloperProgressService.cs` lines 217-270 (stall detection logic to parameterize). Alert building at lines 189-206 (replace filter logic).

**Confidence:** Medium -- stall recomputation with yesterday's date is a new pattern; developer should verify `CountBusinessDays` behavior with shifted reference dates.

**Dependencies:** Step 1 (modifies same file; coordinate record changes)

---

### Step 3: Fix burnup chart and add SP split line on DeveloperProgressCard

Fix the three chart issues (Y-axis, height, zoom) and add the bug/feature SP split line below the total SP line.

**Follow vue-patterns.**

**Files:**
- Modify: `client/src/components/developers/DeveloperProgressCard.vue`

**Feature-specific inputs:**

SP split line (spec Flow 1):
- Below the existing total SP line (`"X / Y SP (Z%) (all types)"`), add a split line showing: `"X features / Y bugs"` using `developer.featureCompletedSp` and `developer.bugCompletedSp`.
- If `developer.completedSp === 0`: hide the split line entirely.
- If only features or only bugs: show the single category (e.g., `"18.0 features"` or `"8.0 bugs"`).
- Format: 1 decimal place, consistent with existing SP display.

Chart Y-axis fix (spec BR11):
- The current chart options already set `yaxis.min: 0` and `yaxis.max: yMax`. Verify the `reversed` property is not set. Explicitly add `yaxis.reversed: false` to guard against ApexCharts defaults.

Chart height increase (spec BR12):
- Change `height="80"` on the `<apexchart>` element to a taller value (at minimum `height="140"` -- enough vertical space to distinguish actual from expected lines).

Zoom disable (spec BR13):
- Add `zoom: { enabled: false }` inside the `chart` config object. This disables both the toolbar zoom controls and programmatic zoom from scroll/pinch gestures.

**Accept:**
- Split line shows below total SP line with feature/bug breakdown
- Split line hidden when completedSp is 0
- Y-axis always 0 at bottom, max at top (reversed: false)
- Chart height visually increased from 80 to >= 140
- Zoom and pan interactions disabled on mini charts
- Day hover tooltip still works (unchanged)

**Pattern reference:** Current `DeveloperProgressCard.vue` lines 36-90 (existing chart options to extend)

**Dependencies:** Step 5 (TypeScript types must include new fields)

---

### Step 4: Restructure alert banner in DailyProgressTab

Replace the "Behind Pace" banner with the "Pace Changes" banner that shows two groups: Worsening/Stalled and Improving/Recovered.

**Follow vue-patterns.**

**Files:**
- Modify: `client/src/components/developers/DailyProgressTab.vue`

**Feature-specific inputs:**

Banner title (spec Flow 2):
- Change from `"Behind Pace"` to `"Pace Changes"`.

Two-group layout (spec Flow 2):
- The alerts list from the API is pre-sorted (Step 2): worsening/stalled first, then improving/recovered.
- Split the `data.alerts` array into two groups in the template:
  - Worsening/Stalled: entries where `alert.direction` is `"worsening"` or `"new-stall"`. Show with a warning indicator (existing warning color).
  - Improving/Recovered: entries where `alert.direction` is `"improving"` or `"stall-resolved"`. Show with a positive indicator (success/green color).
- Each entry shows: developer name, current gap SP (`alert.gapSp`), and delta description:
  - `worsening`: `"gap grew by X.X SP"`
  - `improving`: `"gap shrank by X.X SP"` (show absolute value)
  - `new-stall`: `"N ticket(s) newly stalled"` -- derive stall count from the developer's `stalledTickets` list in `data.developers` (find by `accountId`)
  - `stall-resolved`: `"stall resolved"`

Banner visibility (spec BR10):
- Hidden when `data.alerts.length === 0` (all developers are stable). This replaces the current condition which already checks `data.alerts.length > 0`.
- Hidden during grace period (unchanged -- `data.isGracePeriod` guard already exists).

Grace period info bar:
- Update text from "no behind-pace alerts" to "no pace change alerts" to match the renamed concept.

**Accept:**
- Banner title reads "Pace Changes"
- Two visually distinct groups: Worsening/Stalled (warning style) and Improving/Recovered (success style)
- Each entry shows developer name, current gap, and delta description
- Stall entries show stall count or "stall resolved"
- Banner hidden when all developers stable (empty alerts list)
- Banner hidden during grace period
- Grace period info text updated

**Pattern reference:** Current `DailyProgressTab.vue` lines 53-82 (existing alert banner to restructure)

**Dependencies:** Step 5 (TypeScript types must include new alert fields)

---

### Step 5: Update TypeScript types

Add the new fields to the frontend type definitions to match the extended backend response.

**No matching skill** -- type definitions are a mechanical mapping.

**Files:**
- Modify: `client/src/types/index.ts`

**Changes:**

Extend `DeveloperProgressEntry` (around line 1383):
- Add `featureCompletedSp: number` after `completedSp`
- Add `bugCompletedSp: number` after `featureCompletedSp`

Extend `DeveloperProgressAlert` (around line 1351):
- Add `gapDelta: number | null` after `gapDays`
- Add `direction: 'worsening' | 'improving' | 'stable' | 'new-stall' | 'stall-resolved'` after `gapDelta`

**Accept:**
- Types match backend response shape exactly
- `direction` is a union type, not a plain string
- `gapDelta` is nullable (null on day 1)
- No breaking changes to existing fields

**Pattern reference:** Current type definitions at `client/src/types/index.ts` lines 1351-1407

**Dependencies:** None (should be implemented first or in parallel with backend steps)

---

## Cross-Service Changes

None. Fokus is a single-service system.

## Migration Notes

None. No schema changes.

## Testing Strategy

### Backend -- Bug/Feature SP Breakdown
- Developer with mixed bugs and features: verify `featureCompletedSp + bugCompletedSp == completedSp`
- Developer with only bugs completed: `featureCompletedSp == 0`, `bugCompletedSp == completedSp`
- Developer with only features completed: `bugCompletedSp == 0`, `featureCompletedSp == completedSp`
- Developer with 0 completed SP: both fields are 0
- Bug classification is case-sensitive: `"Bug"` matches, `"bug"` does not
- Null-SP tickets excluded from both sums

### Backend -- Delta-Based Alerting
- Day 1: all developers have `gapDelta == null`, `direction == "stable"`, no alerts
- Day 2 (grace period): gapDelta computed but alerts suppressed
- Day 3: developers behind pace show as `worsening` with computed gapDelta
- Developer catches up (gapDelta < 0): direction is `improving`, appears in alerts
- Developer gap unchanged (gapDelta == 0): direction is `stable`, NOT in alerts
- New stall detected: direction is `new-stall` regardless of gapDelta
- Stall resolved: direction is `stall-resolved` regardless of gapDelta
- Simultaneous stall + gap change: stall takes priority
- Sort order: worsening group first (stalls at top), improving group second (stall-resolved at top)
- Sub-team filter: only developers in selected sub-team appear in alerts

### Backend -- Yesterday's Stall Recomputation
- Ticket stalled today but not yesterday (transition 3 business days ago today, was 2 yesterday): `new-stall`
- Ticket stalled yesterday but not today (resolved between yesterday and today): `stall-resolved`
- Friday/Monday boundary: stall computation uses business days correctly across weekends

### Frontend -- SP Split Line
- Split line shows `"X features / Y bugs"` when both > 0
- Split line shows single category when only one type has SP
- Split line hidden when completedSp is 0

### Frontend -- Alert Banner
- Title reads "Pace Changes"
- Two groups render with correct styling (warning vs success)
- Stall entries show ticket count or "stall resolved"
- Banner hidden when all stable

### Frontend -- Chart Fixes
- Y-axis: 0 at bottom, max at top for all developers
- Chart height visually increased (actual vs expected distinguishable)
- Zoom/pan/scroll interactions disabled on mini charts
- Day hover tooltip still works

## KB Impact

- Update: `docs/kb/analytics/daily-progress.md` -- add bug/feature SP breakdown section, add delta-based alerting section (gap delta formula, direction derivation, alert inclusion rule, sorting), note `BuildStalledTickets` parameterization

## Open Questions

None -- all resolved during analysis.
