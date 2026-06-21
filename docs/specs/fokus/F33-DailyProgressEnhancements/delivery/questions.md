# F33-DailyProgressEnhancements — Questions

**Status:** Phase 1 analysis complete — no blocking questions.

## Findings summary

All referenced files verified:
- `DeveloperProgressService.cs` — found, fully read. Record shapes, computation loops, `BuildStalledTickets`, `CountBusinessDays` all located.
- `DeveloperProgressCard.vue` — found, fully read. Chart options at lines 36-90 match plan references.
- `DailyProgressTab.vue` — found, fully read. Alert banner at lines 53-82 matches plan references.
- `client/src/types/index.ts` — `DeveloperProgressAlert` at line 1351, `DeveloperProgressEntry` at line 1383. Matches plan.
- `Fokus.Tests/Features/Analytics/DeveloperProgressServiceTests.cs` — exists, 9 slices already written. Test fixtures (`TestData.cs`) have `BugTicket`, `FeatureTicket`, `Membership`, `Transition` helpers — sufficient for new test slices.
- `LeaderboardService.cs` — `IsBug` helper confirmed: `m.Ticket?.IssueType == "Bug"` (line 287). Partition pattern at lines 109-113 matches plan.

## Observations for implementation

1. **`BuildStalledTickets` parameterization (Step 2):** The method uses `DateTime.UtcNow.Date` as a local variable `today` (line 225). Extracting this to a `referenceDate` parameter is straightforward — only one internal call site (`CountBusinessDays(lastTransition, today)`). The method is `private`, so only called from `ComputeProgress` — one call site to update (known deviation: update ALL call sites).

2. **Step order:** Plan lists Step 5 (TypeScript types) last but Steps 3 and 4 declare it as a dependency. Since Steps 3/4 are frontend-only and TypeScript compilation is checked at build time, implementing Step 5 first (or immediately before Step 3) avoids type errors during the frontend steps. Will implement Step 5 before Steps 3/4.

3. **Alert sorting (Step 2):** `new-stall` at top of worsening group, then by `gapDelta` desc. `stall-resolved` at top of improving group, then by `gapDelta` asc. Plan is unambiguous — implementing as two-pass sort (group first, then sub-sort within group).

4. **`gapDelta` null on day 1:** `dailyBreakdown.Count < 2` → gapDelta is null, direction is stable, no alert. Confirmed: daily breakdown is built 1..currentDay, so on day 1 count == 1 < 2.

5. **KB Impact is a non-numbered step** (known deviation logged). Will treat it as an implicit final step and include in implementation.md.

## Questions

None — all clear, ready to implement.
