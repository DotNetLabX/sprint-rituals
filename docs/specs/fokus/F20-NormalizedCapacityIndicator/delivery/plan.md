# Normalized Capacity Indicator

**Feature Spec:** `docs/features/NormalizedCapacityIndicator/spec.md`

## Context

Developers with reduced capacity (vacation, tech lead duties) show lower raw SP completed than full-time peers. A manager sees "8 SP" and may assume underperformance, when the developer is delivering at a higher rate per available hour. This feature adds a bracketed normalized value `(~X)` next to SP completed — visible only for developers below 100% capacity — so comparisons are fair without replacing the real planning number.

Three display surfaces are affected:
1. **Developer Throughput tab** — already has capacity data; frontend-only change.
2. **Leaderboard tab + Dashboard leaderboard widget** — API responses lack capacity data; backend change needed before frontend display.
3. **Dashboard leaderboard** — uses `DeveloperSummary` from `SprintSummaryService`; same gap.

**Services impacted:** Fokus (single service).

## Scope

**In scope:**
- Add `capacityPercent` to leaderboard API response records (multi, single, sprint breakdown)
- Add `capacityPercent` to dashboard `DeveloperSummary` record
- Pass capacity data into `LeaderboardService` and `SprintSummaryService.ComputeLeaderboard`
- Frontend: display `(~X)` next to SP completed on all three surfaces
- Multi-sprint: per-sprint normalization then average (BR6)
- Tooltip wiring from `help.tooltips.md`

**Out of scope:**
- Normalizing other metrics (SP assigned, tickets, rolling average, completion %)
- Configurable show/hide toggle
- Color coding the normalized value
- Capacity-normalized rolling average

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | (none) | -- | Add `capacityPercent` field to 4 C# response records in `LeaderboardService.cs`; plumb capacity data into service methods | |
| 2 | (none) | -- | Add `capacityPercent` field to `DeveloperSummary` record in `SprintSummaryService.cs`; plumb capacity into `ComputeLeaderboard` | |
| 3 | (none) | -- | Pass capacity records from `GetLeaderboardEndpoint` into `LeaderboardService` methods | |
| 4 | (none) | -- | Pass capacity records from `GetSprintSummaryEndpoint` into `SprintSummaryService.ComputeSummary` and down to `ComputeLeaderboard` | |
| 5 | vue-patterns | Follow | Add `capacityPercent` to 4 TS interfaces in `types/index.ts` | |
| 6 | vue-patterns | Follow | Create `normalizedSp` helper + add `(~X)` display to throughput table (single + multi) in `DevelopersView.vue` | |
| 7 | vue-patterns | Follow | Add `(~X)` display to `LeaderboardTable.vue` (Total SP column, single + multi) | |
| 8 | vue-patterns | Follow | Add `(~X)` display to `DashboardView.vue` leaderboard widget rows | |
| 9 | vue-patterns | Follow | Wire tooltip text from `help.tooltips.md` to normalized SP indicators using `InfoTooltip` | |

No gaps requiring new skills. All backend steps are record field additions and parameter plumbing — no new patterns involved. Frontend steps follow `vue-patterns` for Composition API conventions.

## Domain Model Changes

None. No new entities, value objects, or domain events. This feature uses existing `DeveloperSprintCapacity` records and `Developer.DefaultCapacityPercent`.

## Data Model Changes

None. No database changes, no migrations. Capacity data is already persisted.

## Implementation Steps

### Step 1: Add `capacityPercent` to leaderboard response records

**What:** Add an `int CapacityPercent` field to four C# response records in the leaderboard service. Add a capacity resolution helper to the service. The service methods will accept capacity data (added in Step 3) and populate the new field.

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/LeaderboardService.cs`

**Details:**

Add `int CapacityPercent` to these records:
- `LeaderboardDeveloperSprintBreakdown` — per-sprint capacity for that developer in that sprint
- `LeaderboardDeveloperEntry` — average capacity across selected sprints (rounded to nearest int)
- `LeaderboardDeveloperSingleEntry` — capacity for the single sprint

Add a private `GetCapacity` helper to `LeaderboardService` (same pattern as `DeveloperThroughputService.GetCapacity` at line 225-238). Parameters: `Dictionary<string, Dictionary<int, int>> capacityLookup`, `string developerId`, `int sprintId`, `List<Developer> developers`. Returns the sprint-specific override or developer default.

Update `ComputeMultiSprint` signature to accept `Dictionary<string, Dictionary<int, int>> capacityLookup` and `List<Developer> allDevelopers`. In the per-sprint breakdown lambda, call `GetCapacity` and pass the result as the new `CapacityPercent` constructor arg. For the aggregate `LeaderboardDeveloperEntry`, compute average capacity across sprint breakdowns (rounded to nearest int via `(int)Math.Round(...)`).

Update `ComputeSingleSprint` signature to accept `Dictionary<string, Dictionary<int, int>> capacityLookup` and `List<Developer> allDevelopers`. In the per-developer lambda, call `GetCapacity` for the target sprint and pass as `CapacityPercent`.

**Pattern reference:** `DeveloperThroughputService.GetCapacity` (lines 225-238) and `DeveloperThroughputService.ComputeThroughput` capacity lookup construction (lines 74-78).

**Depends on:** Nothing.

### Step 2: Add `capacityPercent` to `DeveloperSummary` and `ComputeLeaderboard`

**What:** Add an `int CapacityPercent` field to the `DeveloperSummary` record in `SprintSummaryService.cs`. Update `ComputeLeaderboard` to accept capacity data and populate the field.

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs`

**Details:**

Add `int CapacityPercent` to `DeveloperSummary` record (after `BugTickets`).

Update `ComputeLeaderboard` signature to accept additional parameters: `Dictionary<string, Dictionary<int, int>> capacityLookup`, `List<Developer> allDevelopers`, `int sprintId`. In the developer projection, resolve capacity using the same logic as `DeveloperThroughputService.GetCapacity`: check `capacityLookup[dev.Id][sprintId]`, fall back to `dev.DefaultCapacityPercent`, default 100.

Update the call site in `ComputeSummary` (line 174) to pass the new parameters.

**Pattern reference:** Same `GetCapacity` pattern as Step 1.

**Depends on:** Nothing (parallel with Step 1).

### Step 3: Plumb capacity data into `GetLeaderboardEndpoint`

**What:** Pass the capacity lookup dictionary and all developers list to `LeaderboardService.ComputeMultiSprint` and `ComputeSingleSprint` calls.

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetLeaderboard/GetLeaderboardEndpoint.cs`

**Details:**

The endpoint already loads `capacityRecords` (line 41) and `allDevelopers` (line 39). Build the capacity lookup dictionary the same way `DeveloperThroughputService` does:
```
var capacityLookup = capacityRecords
    .GroupBy(c => c.DeveloperAccountId)
    .ToDictionary(
        g => g.Key,
        g => g.ToDictionary(c => c.SprintId, c => c.CapacityPercent));
```

Pass `capacityLookup` and `allDevelopers` to both `ComputeSingleSprint` and `ComputeMultiSprint` calls (lines 62 and 82).

**Pattern reference:** `GetLeaderboardEndpoint.cs` already loads the required data; `DeveloperThroughputService.ComputeThroughput` lines 74-78 for the lookup construction.

**Depends on:** Step 1 (service signatures changed).

### Step 4: Plumb capacity data into `GetSprintSummaryEndpoint` for dashboard leaderboard

**What:** Pass the capacity lookup dictionary, all developers, and selected sprint ID down through `ComputeSummary` to `ComputeLeaderboard`.

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetSprintSummary/GetSprintSummaryEndpoint.cs`
- `src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs` (update `ComputeSummary` signature)

**Details:**

The endpoint already loads `capacityRecords` (line 64). Build the capacity lookup dictionary (same pattern as Step 3). 

Update `ComputeSummary` signature to accept `Dictionary<string, Dictionary<int, int>> capacityLookup` (or pass the raw `capacityRecords` and build the lookup inside). The simpler approach: build the lookup in the endpoint and pass it into `ComputeSummary`, which forwards it to `ComputeLeaderboard`.

Update the `ComputeSummary` call (line 76) to pass the capacity lookup.

Inside `ComputeSummary`, pass `capacityLookup`, `allDevelopers` (already available from `filteredDevelopers` parent list — but `ComputeLeaderboard` needs ALL developers for default fallback), and `selectedSprint.Id` to `ComputeLeaderboard`.

Note: `ComputeSummary` receives `activeDevelopers` filtered by exclusion. For capacity default fallback, pass the full developer list. The endpoint already has `allDevelopers` (line 57) — add it as a `ComputeSummary` parameter.

**Depends on:** Step 2 (service signatures changed).

### Step 5: Add `capacityPercent` to TypeScript types

**What:** Add `capacityPercent: number` to the TypeScript interface definitions matching the C# record changes.

**Files to modify:**
- `client/src/types/index.ts`

**Details:**

Add `capacityPercent: number` to:
- `LeaderboardDeveloperSprintBreakdown` (after `bugTickets`, line ~867)
- `LeaderboardDeveloperEntry` (after `totalTickets`, line ~881)
- `LeaderboardDeveloperSingleEntry` (after `totalTickets`, line ~917)
- `DeveloperSummary` (after `bugTickets`, line ~109)

Follow vue-patterns: TypeScript-first, exact property name match with camelCase of C# record.

**Depends on:** Steps 1-4 (backend fields exist).

### Step 6: Add normalized SP display to throughput table in `DevelopersView.vue`

**What:** Show `(~X)` next to SP Completed in both single-sprint and multi-sprint throughput tables when a developer's capacity is below 100%. Add a `normalizedSp` helper function.

**Files to modify:**
- `client/src/views/DevelopersView.vue`

**Details:**

Add a helper function in `<script setup>`:
```typescript
function normalizedSp(spCompleted: number, capacityPercent: number): number | null {
  if (capacityPercent >= 100 || capacityPercent <= 0) return null
  return Math.round(spCompleted / (capacityPercent / 100))
}
```

**Single-sprint table (SP Completed cell, around line 385-389):**
After the existing `{{ row.bd.spCompleted }}` span, add a conditional span:
```html
<span v-if="normalizedSp(row.bd.spCompleted, row.bd.capacityPercent) !== null"
      class="ml-1 text-xs text-text-muted"
      title="Estimated SP at full capacity. Shows what this developer's output would look like at 100% availability.">
  (~{{ normalizedSp(row.bd.spCompleted, row.bd.capacityPercent) }})
</span>
```

**Multi-sprint table (Avg SP Completed cell, around line 476):**
Compute per-sprint normalization then average (BR6). Add a helper:
```typescript
function avgNormalizedSpCompleted(dev: DeveloperThroughputEntry): number | null {
  const perSprintNormalized = dev.sprintBreakdowns.map(b => {
    if (b.capacityPercent >= 100 || b.capacityPercent <= 0) return b.spCompleted
    return b.spCompleted / (b.capacityPercent / 100)
  })
  const avg = perSprintNormalized.reduce((a, b) => a + b, 0) / perSprintNormalized.length
  // Show only if at least one sprint had < 100% capacity
  const anyReduced = dev.sprintBreakdowns.some(b => b.capacityPercent < 100 && b.capacityPercent > 0)
  if (!anyReduced) return null
  return Math.round(avg)
}
```
After the existing `{{ avgSpCompleted(dev) }}` text, add:
```html
<span v-if="avgNormalizedSpCompleted(dev) !== null"
      class="ml-1 text-xs text-text-muted"
      title="Estimated SP at full capacity. Shows what this developer's output would look like at 100% availability.">
  (~{{ avgNormalizedSpCompleted(dev) }})
</span>
```

**Pattern reference:** Existing delta indicator pattern in the same file (span with conditional display, `text-xs` sizing).

**Depends on:** Step 5 (types exist).

### Step 7: Add normalized SP display to `LeaderboardTable.vue`

**What:** Show `(~X)` next to the Total SP column for developers with < 100% capacity, in both single and multi-sprint modes.

**Files to modify:**
- `client/src/components/developers/LeaderboardTable.vue`

**Details:**

Add the same `normalizedSp` helper in `<script setup>` (or import from a shared location — but since it's a 3-line function, inline is fine per existing patterns in this codebase).

**Total SP cell (around line 111-119):**
After `{{ dev.totalSp.toFixed(1) }}`, add:
```html
<span v-if="normalizedSp(dev.totalSp, dev.capacityPercent) !== null"
      class="ml-1 text-xs text-text-muted"
      title="Estimated SP at full capacity. Shows what this developer's output would look like at 100% availability.">
  (~{{ normalizedSp(dev.totalSp, dev.capacityPercent) }})
</span>
```

For **multi-sprint mode**, `dev.capacityPercent` is the average capacity from the API. The normalized value = `totalSp / (avgCapacity / 100)`. This is the average-then-normalize approach, but for the leaderboard multi-sprint response, the API provides aggregated totals (not per-sprint breakdowns per developer in the entry-level record). However, the API DOES provide `sprintBreakdowns` on `LeaderboardDeveloperEntry`. Use per-sprint normalization then average for accuracy (BR6):

Add a helper:
```typescript
function normalizedTotalSp(dev: LeaderboardDeveloperEntry | LeaderboardDeveloperSingleEntry): number | null {
  if ('sprintBreakdowns' in dev && Array.isArray(dev.sprintBreakdowns)) {
    // Multi-sprint: per-sprint normalize then average (BR6)
    const breakdowns = dev.sprintBreakdowns as Array<{ totalSp: number; capacityPercent: number }>
    const anyReduced = breakdowns.some(b => b.capacityPercent < 100 && b.capacityPercent > 0)
    if (!anyReduced) return null
    const perSprint = breakdowns.map(b => {
      if (b.capacityPercent >= 100 || b.capacityPercent <= 0) return b.totalSp
      return b.totalSp / (b.capacityPercent / 100)
    })
    return Math.round(perSprint.reduce((a, b) => a + b, 0) / perSprint.length)
  }
  // Single-sprint
  const cap = (dev as LeaderboardDeveloperSingleEntry).capacityPercent
  if (cap >= 100 || cap <= 0) return null
  return Math.round(dev.totalSp / (cap / 100))
}
```

**Pattern reference:** Existing delta display pattern in the same file.

**Depends on:** Step 5 (types exist).

### Step 8: Add normalized SP display to Dashboard leaderboard widget

**What:** Show `(~X)` next to SP value in the dashboard leaderboard rows for developers with < 100% capacity. Preserve across features/bugs toggle.

**Files to modify:**
- `client/src/views/DashboardView.vue`

**Details:**

Add a `normalizedSp` helper (same 3-line function as Step 6).

In the leaderboard rows (around line 220-224), the SP display currently shows:
```html
{{ leaderboardMode === 'features' ? dev.featureSp : dev.bugSp }} SP
```

After this text (but inside the same `<span>` or as a sibling), add a normalized indicator. The dashboard leaderboard shows featureSp or bugSp depending on mode. Normalize whichever is displayed:

```html
<span v-if="normalizedSp(leaderboardMode === 'features' ? dev.featureSp : dev.bugSp, dev.capacityPercent) !== null"
      class="ml-1 text-xs text-text-muted font-normal"
      title="Estimated SP at full capacity. Shows what this developer's output would look like at 100% availability.">
  (~{{ normalizedSp(leaderboardMode === 'features' ? dev.featureSp : dev.bugSp, dev.capacityPercent) }})
</span>
```

The `DeveloperSummary` type now has `capacityPercent` (from Steps 2+4+5), so `dev.capacityPercent` is available.

The features/bugs toggle just re-sorts the same data and selects a different SP field — the normalized indicator follows the selected field automatically.

**Pattern reference:** Existing leaderboard row pattern in the same file.

**Depends on:** Step 5 (types exist).

### Step 9: Wire tooltip text from `help.tooltips.md`

**What:** Ensure the `title` attributes on normalized SP indicators match the help tooltip text exactly. Optionally add an `InfoTooltip` component to the throughput table's SP Completed column header to explain normalization.

**Files to modify:**
- `client/src/views/DevelopersView.vue`
- `client/src/components/developers/LeaderboardTable.vue`
- `client/src/views/DashboardView.vue`

**Details:**

The tooltip text from `docs/features/NormalizedCapacityIndicator/help.tooltips.md`:
- **Normalized SP Value (~X):** "Estimated SP at full capacity. Shows what this developer's output would look like at 100% availability."
- **No Bracket Shown:** "Developer is at 100% capacity -- raw SP is already the full-capacity value."

Steps 6-8 already use the first tooltip text as `title` attributes on the `(~X)` spans. This step verifies consistency and adds the `InfoTooltip` component to the SP Completed column header in the throughput table (both single and multi-sprint) to explain what the bracketed number means.

In `DevelopersView.vue`, import `InfoTooltip` (already imported, line 12) and add it next to the "SP Completed" / "Avg SP Completed" column headers:
```html
<th ...>SP Completed <InfoTooltip text="When a developer has reduced capacity, a bracketed (~X) value shows their estimated SP at full availability." />{{ throughputSortIcon('spCompleted') }}</th>
```

No additional `InfoTooltip` needed on leaderboard or dashboard — the `title` attribute on each `(~X)` span is sufficient for those compact displays.

**Depends on:** Steps 6-8 (indicators exist to verify).

## Cross-Service Changes

None. Single-service feature.

## Migration Notes

None. No database changes.

## Testing Strategy

1. **Single-sprint, developer at 60% capacity, 8 SP completed:** Verify `(~13)` appears next to `8` in throughput table, leaderboard table, and dashboard widget. Formula: `8 / (60/100) = 13.33`, rounded to `13`.

2. **Developer at 100% capacity:** Verify no bracket appears anywhere.

3. **Developer at 0% capacity (but has completed tickets, so not excluded):** Verify no bracket appears (0% is a guard condition — division by zero).

4. **Multi-sprint, mixed capacity (100%, 50%, 100% across 3 sprints):** For throughput, verify per-sprint normalization then averaging. If SP completed = [10, 5, 10] and capacity = [100, 50, 100], normalized per sprint = [10, 10, 10], average = 10. The bracket shows `(~10)` next to the raw average of `8.3`.

5. **Dashboard features/bugs toggle:** Switch between modes. Verify brackets persist and show correct normalized values for the selected SP field.

6. **Leaderboard chart:** Verify the stacked bar chart does NOT show normalized values (it shows raw SP — normalization is table/text only per spec).

7. **API contract:** Verify `GET /api/analytics/leaderboard` response includes `capacityPercent` on developer entries. Verify `GET /api/analytics/sprint-summary` response includes `capacityPercent` on leaderboard entries.

## Open Questions

None.
