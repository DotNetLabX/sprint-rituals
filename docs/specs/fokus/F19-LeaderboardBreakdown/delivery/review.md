# LeaderboardBreakdown — Review

## Reviewed By
`reviewer` (Sonnet agent, claude-sonnet-4-6)

## Verdict: APPROVE

## Pre-commitment Predictions

| Predicted Problem Area | Actual Finding |
|------------------------|----------------|
| `excludedDeveloperIds` parameter unused inside LeaderboardService | CONFIRMED — dead parameter (LOW, not a logic error because endpoint pre-filters) |
| Double sub-team filter in GetDeveloperMemberships | CONFIRMED — redundant but harmless (LOW) |
| Delta polarity direction for `neutral` might be wrong | Not an issue — hardcoded `"neutral"` string is correct per plan |
| Missing case where leaderboard tab is null when first rendered | Not an issue — DevelopersView guards with `v-else-if="store.leaderboard"` |
| Tooltip text mismatch with help.tooltips.md | No mismatch — all tooltip texts match exactly |

## Findings

### [LOW] Unused `excludedDeveloperIds` parameter in LeaderboardService
**File:** `src/Services/Fokus/Fokus.API/Features/Analytics/LeaderboardService.cs:80` and `:146`
**Issue:** Both `ComputeMultiSprint` and `ComputeSingleSprint` declare `HashSet<string>? excludedDeveloperIds = null` but never read it inside the method body. The exclusion is correctly applied — the endpoint pre-filters `activeDevelopers` before passing them in — so there is no logic error. The dead parameter misleads readers into thinking exclusion happens inside the service.
**Fix:** Either remove the parameter (since the caller handles exclusion before calling in), or add a guard inside the service body to filter developers by `excludedDeveloperIds` (removing the pre-filter in the endpoint). Either approach is correct; removing the parameter is simpler.

### [LOW] Redundant sub-team filter in GetDeveloperMemberships
**File:** `src/Services/Fokus/Fokus.API/Features/Analytics/LeaderboardService.cs:240-249`
**Issue:** `GetDeveloperMemberships` filters `m.Ticket?.Assignee?.SubTeam == subTeam` on memberships, but `FilterDevelopers` already scoped the developer list to the requested sub-team before the loop. A developer from the wrong sub-team can never reach this call. The condition is never false inside the loop.
**Fix:** Remove the sub-team check from `GetDeveloperMemberships` and simplify it to: `sprint.Memberships.Where(m => m.Ticket?.AssigneeId == developerId).ToList()`. The sub-team scoping is already provided by `FilterDevelopers`.

### [LOW] LeaderboardTab tooltip placed on outer container div instead of info icon
**File:** `client/src/components/developers/LeaderboardTab.vue:41`
**Issue:** The tooltip `title` attribute is placed on the outer `<div class="flex flex-col gap-6">` wrapper, not on an info icon. This means hovering anywhere in the large container area triggers the tooltip text. The plan specifies tooltip wiring on the tab area using the info icon pattern (matching the info icons used in single/multi card headers on lines 50-54 and 69-73 of the same file). The outer div tooltip text is also duplicated by the info icons inside.
**Fix:** Remove the `title` attribute from the outer wrapper div (line 41). The two inner info icons already carry the correct tooltip text.

## Positive Observations

- **Plan conformance is complete.** All 11 steps are implemented as specified, including the KB update, frontend-map update, and tooltip wiring from help.tooltips.md. No steps were skipped or silently deviated from.
- **BugRatioService pattern followed exactly.** `LeaderboardService` mirrors `BugRatioService` in structure, naming, and helper methods. `CompletedMemberships`, `FilterDevelopers`, `DeltaDirection`, `DeltaPolarity` are all consistent with the established pattern. Any developer reading both services will find them immediately familiar.
- **TypeScript types match backend records precisely.** All 8 new interfaces in `index.ts` match their C# counterparts field-for-field, including optional fields (`subTeam: string | null`, `delta: LeaderboardDeveloperDelta | null`). The `mode` discriminator is correctly typed as `'multi' | 'single'` (literal union, not `string`).
- **`isSingleEntry` type narrowing function** is a clean, idiomatic TypeScript approach to discriminate between multi and single entries without casting. The `'delta' in dev` check is correct since `LeaderboardDeveloperEntry` has no `delta` field.
- **Dashboard leaderboard reset** (`leaderboardMode.value = 'features'` on `onMounted`, line 25 of DashboardView.vue) correctly implements BR6 — resets to Features on each page mount.
- **Empty sprint case** correctly returns `LeaderboardResponse("multi", null, null)` rather than a 404, matching the spec's "200 with empty results" behavior.
- **Multi-sprint exclusion logic** (exclude developers excluded in ALL selected sprints) is correctly implemented in the endpoint, consistent with the BugRatio pattern.
- **Stacked bar chart zero-value handling** — bars for developers with `totalSp === 0` render as an empty bar (no segments rendered due to `v-if="dev.totalSp > 0"`), which is correct. Zero-SP developers remain visible per BR8.
- **Sorting duplication between backend and frontend is intentional and correct.** The backend sorts by `TotalSp desc, DisplayName asc`; `LeaderboardTab.vue` re-sorts in the same order before passing to children. This is defensive and correct — ensures UI is not dependent on backend sort stability.

## Gaps

- **No null-safe guard on `store.leaderboard` in DevelopersView when initial tab is leaderboard from URL.** If `store.leaderboard` is still null after `fetchLeaderboard()` succeeds with an empty response (e.g., no closed sprints), the `v-else-if="store.leaderboard"` condition suppresses rendering and shows nothing — no empty state message. This matches the Bug Ratio tab behavior (same pattern), so it is consistent, but a future improvement would be an explicit empty state. Not a defect given spec says "200 with empty results."
- **Tooltip on Total Tickets column has no delta indicator tooltip** — the delta tooltip string ("Change versus the prior sprint...") is applied to all columns except Total Tickets. Total Tickets has no delta in single-sprint mode (by design), so this is correct, but worth noting as an intentional omission.

## Open Questions

None. All self-audit findings were downgraded to LOW — none involve genuine logic errors given the endpoint pre-filters developers before passing them to the service.

## Evidence

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Build | PASS | `dotnet build Fokus.API.csproj -c Release` | 0 errors, 3 pre-existing warnings (NU1903 vulnerability notice, CS9107 in DeveloperRepository — both pre-existing, not introduced by this feature) |
| Plan step 1 | PASS | Read SprintSummaryService.cs | `DeveloperSummary` record extended with `FeatureSp`, `BugSp`, `FeatureTickets`, `BugTickets`; `ComputeLeaderboard` applies excluded-from-scope filter |
| Plan step 2 | PASS | Read LeaderboardService.cs | All 8 response records present; `ComputeMultiSprint` and `ComputeSingleSprint` implemented with correct polarity |
| Plan step 3 | PASS | Read GetLeaderboardEndpoint.cs + GetLeaderboardQuery.cs | Route `/api/analytics/leaderboard`, all 4 validator rules, mirrors GetBugRatioEndpoint exactly |
| Plan step 4 | PASS | Read types/index.ts lines 848-925 | All 8 Leaderboard* interfaces present, `DeveloperSummary` extended |
| Plan step 5 | PASS | Read api/analytics.ts lines 95-102 | `getLeaderboard` function present, matches `getBugRatio` pattern |
| Plan step 6 | PASS | Read developersStore.ts | `leaderboard`, `leaderboardLoading`, `leaderboardError` refs; `fetchLeaderboard`, `switchTab`, `selectSprint`, `selectLastN`, `selectSubTeam` all extended |
| Plan step 7 | PASS | Read DashboardView.vue | Toggle present, `sortedLeaderboard` computed, all tooltips match help.tooltips.md |
| Plan step 8 | PASS | Read LeaderboardTab.vue | L1 container, mode detection, children receive sorted data |
| Plan step 9 | PASS | Read LeaderboardChart.vue + LeaderboardTable.vue | Stacked bars via inline CSS, delta indicators, `isSingleEntry` narrowing |
| Plan step 10 | PASS | Read DevelopersView.vue | Third tab button, URL sync `tab=leaderboard`, loading/error/content pattern |
| Plan step 11 | PASS | Read docs/kb/analytics/leaderboard.md, docs/kb/index.md, docs/kb/frontend-map.md | KB entry created, index updated, frontend-map updated |
| Tooltip wiring | PASS | Cross-checked all tooltip strings against help.tooltips.md | All 10 tooltip texts match exactly |
| TypeScript types vs backend records | PASS | Cross-checked index.ts interfaces against LeaderboardService.cs records | All fields match, nullability correct |
| DI registration | PASS | Read DependencyInjection.cs line 34 | `AddScoped<LeaderboardService>()` registered |
