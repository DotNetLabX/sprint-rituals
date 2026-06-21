# Daily Progress Enhancements — Review

## Reviewed By

reviewer (Sonnet 4.6 agent) — primary review. Codex cross-validation: pending (invoked after Sonnet review; see Cross-Validation section).

## Verdict: APPROVE

## Pre-commitment Predictions

| Prediction | Outcome |
|-----------|---------|
| Direction derivation priority — `new-stall` vs `stall-resolved` when both conditions exist simultaneously | Mutually exclusive by definition: `stall-resolved` = no stalls today, `new-stall` = a stall exists today → can't both be true. Code handles correctly. |
| `BuildStalledTickets` parameterization — date parameter plumbing may miss edge cases at call sites | Single call site. Two calls made correctly: today's date and yesterday's date. No issues found. |
| Frontend `text-status-success` token — carry-over note flags this; likely missing | Token IS defined in `client/src/assets/main.css:23` (`--color-status-success`). Used widely in codebase. No issue. |
| SP split line display logic — "only one type" case may have formatting inconsistency | `v-else` branch correctly fires only when `completedSp > 0` and `featureCompletedSp === 0` and `bugCompletedSp > 0`. Invariant guarantees correctness. |
| Alert sort — `new-stall` at top of worsening, `stall-resolved` at top of improving — complex and may have ordering bugs | Sort logic is correct. Analyzed in detail below. |

## Stage 1: Plan Conformance

All five plan steps are implemented and verified.

| Plan Step | Status | Evidence |
|-----------|--------|---------|
| Step 1: `FeatureCompletedSp` and `BugCompletedSp` on `DeveloperProgressEntry` | PASS | `DeveloperProgressService.cs:27-30`, accumulation at lines 121-139 |
| Step 1: Bug classification `m.Ticket?.IssueType == "Bug"` | PASS | `DeveloperProgressService.cs:137` |
| Step 1: Invariant `featureCompletedSp + bugCompletedSp == completedSp` | PASS | Tested in Slice 10 (`DeveloperProgressServiceTests.cs:369`) |
| Step 1: Null-SP tickets excluded from both sums | PASS | `effectiveSp.HasValue` guard at line 133; accumulation only inside that branch |
| Step 1: Rounding to 1 decimal | PASS | `DeveloperProgressService.cs:207-208` |
| Step 2: `GapDelta` (decimal?) and `Direction` (string) on `DeveloperProgressAlert` | PASS | `DeveloperProgressService.cs:13-14` |
| Step 2: `BuildStalledTickets` accepts `DateTime referenceDate` | PASS | Method signature at line 297-303; called with `todayDate` and `yesterdayDate` at lines 193-198 |
| Step 2: Gap delta from last two entries of `DailyBreakdown` | PASS | Lines 229-237 |
| Step 2: Null when `Count < 2` | PASS | Guard at line 231 |
| Step 2: Direction derivation priority (stall > gap > stable) | PASS | Lines 248-257, stall checks before gap checks |
| Step 2: Alert inclusion `direction != "stable"` | PASS | Line 259 `if (direction == "stable") continue;` |
| Step 2: Alert sorting (two groups, priority entries at top, then by gapDelta) | PASS | Lines 278-284 |
| Step 2: Grace period suppresses alerts | PASS | `if (!isGracePeriod)` at line 224 |
| Step 3: `zoom: { enabled: false }` in chart config | PASS | `DeveloperProgressCard.vue:43` |
| Step 3: `reversed: false` on yaxis | PASS | `DeveloperProgressCard.vue:64` |
| Step 3: Chart height changed from 80 to 140 | PASS | `DeveloperProgressCard.vue:180` |
| Step 3: Split line with feature/bug breakdown | PASS | `DeveloperProgressCard.vue:150-165` |
| Step 3: Split line hidden when `completedSp === 0` | PASS | `v-if="developer.completedSp > 0"` at line 152 |
| Step 4: Banner title "Pace Changes" | PASS | `DailyProgressTab.vue:96` |
| Step 4: Two groups — worsening/stalled (warning) and improving/recovered (success) | PASS | Lines 100-149 |
| Step 4: `deltaDescription()` function per direction | PASS | Lines 27-42 |
| Step 4: Stall count from `data.developers` by `accountId` lookup | PASS | `stallCount()` function lines 21-24 |
| Step 4: Grace period text updated to "no pace change alerts" | PASS | `DailyProgressTab.vue:83` |
| Step 5: `DeveloperProgressAlert` extended with `gapDelta` and `direction` | PASS | `client/src/types/index.ts:1357-1358` |
| Step 5: `DeveloperProgressEntry` extended with `featureCompletedSp` and `bugCompletedSp` | PASS | `client/src/types/index.ts:1392-1393` |
| Step 5: `direction` is a union type | PASS | `'worsening' \| 'improving' \| 'stable' \| 'new-stall' \| 'stall-resolved'` |
| Step 5: `gapDelta` nullable | PASS | `number \| null` |
| KB: `docs/kb/analytics/daily-progress.md` updated | PASS | Bug/Feature SP Breakdown, Delta-Based Alerting, BuildStalledTickets Parameterization sections all present |

## Stage 2: Code Quality

### Carry-Over Findings (from implementation.md)

**1. Pre-existing build errors (`BaseCard`, `deltaClass`, `authStore`)**
Confirmed pre-existing. `tsc --noEmit` on current branch: clean (zero output). The three mentions are in `DeveloperProgressCard.vue` (BaseCard import that IS used in template — actually no longer unused after checking), `QaWorkloadMetricCards.vue` (deltaClass), and `UsersTab.vue` (authStore). These are pre-existing and out of scope for this review. Not introduced by F33.

**2. `text-status-success` token**
Confirmed valid. `client/src/assets/main.css:23` defines `--color-status-success: var(--status-success)`. Both dark (line 50) and light (line 112) theme values defined. Token used extensively across 15+ components before F33. No issue.

### Logic Analysis

**Sort order correctness (DeveloperProgressService.cs:278-284)**

```
.OrderBy(a => a.Direction is "improving" or "stall-resolved" ? 1 : 0)   // Group: worsening=0, improving=1
.ThenBy(a => a.Direction is "new-stall" or "stall-resolved" ? 0 : 1)    // Priority within group: stall=0, gap=1
.ThenBy(a => a.Direction is "improving" or "stall-resolved"
    ? a.GapDelta ?? 0m
    : -(a.GapDelta ?? 0m))                                                // Within group: asc for improving, desc for worsening
```

Trace through spec requirements:
- Worsening group (slot 0): `new-stall` gets priority rank 0, then `worsening` rank 1. Within worsening: `-(gapDelta)` ascending = gapDelta descending (largest increase first). Correct.
- Improving group (slot 1): `stall-resolved` gets priority rank 0, then `improving` rank 1. Within improving: `gapDelta` ascending (most negative = largest improvement first). Correct.

**`stall-resolved` with `gapDelta > 0` (plan spec: "stall-resolved appears in Improving group even if gapDelta > 0")**

The first `OrderBy` puts `stall-resolved` in slot 1 (improving group). Even if `gapDelta > 0`, the direction is set before `gapDelta > 0` would make it `worsening`. The stall-resolved path exits early in the `if/else if` chain (line 250) before reaching the gap checks. Confirmed correct.

**`new-stall` with `gapDelta < 0` (plan spec: "new-stall appears in Worsening group even if gapDelta < 0")**

Same analysis — `hasNewStall` check at line 248 exits before gap checks. `new-stall` always lands in slot 0 (worsening group) regardless of gapDelta. Correct.

**SP partition invariant edge case: null-SP ticket that IS a bug**

When `effectiveSp.HasValue` is false, the ticket is excluded from `completedSp`, `bugCompletedSp`, and `featureCompletedSp` (all accumulation happens inside the `if (effectiveSp.HasValue)` block at line 133). Invariant holds for null-SP tickets: they contribute 0 to all three sums. This matches the plan spec: "Null-SP tickets excluded from both sums."

Note: `GetEffectiveSp(settings.DefaultSpPerBug)` returns the default SP value for null-SP bugs, so null-SP bugs ARE counted (via the default) when DefaultSpPerBug is set. The code at line 132 calls `GetEffectiveSp(settings.DefaultSpPerBug)` which returns a value for null-SP bugs. The null case only applies to tickets where even the effective SP is null. This is consistent with the plan and Slice 13 test confirms it.

**Yesterday's stall date — `todayDate.AddDays(-1)` across weekend**

`yesterdayDate = todayDate.AddDays(-1)` (line 93). On Monday, `yesterdayDate` = Sunday. `BuildStalledTickets` with `referenceDate = Sunday` calls `CountBusinessDays(lastTransition, Sunday)`. Since Sunday has `DayOfWeek.Sunday`, the counter correctly excludes it. The stall threshold uses business days, so a ticket stalled from Thursday's perspective would NOT count Sunday as a business day when using yesterday's (Sunday) reference. This is correct behavior: on Monday morning, yesterday's stall set is computed as of Sunday, meaning no new weekday business elapsed, so tickets that crossed the 2-day threshold on Monday itself (using today's date) will correctly appear as `new-stall`. The test Slice 18 explicitly handles the Monday edge case (comment lines 649-652) and confirms it is correct.

**`deltaDescription()` when `gapDelta` is null for `worsening`/`improving`**

`DailyProgressTab.vue:30` for worsening: `alert.gapDelta != null ? \`gap grew by ${...}\` : 'gap growing'`.
`DailyProgressTab.vue:32` for improving: `alert.gapDelta != null ? ... : 'gap shrinking'`.

However: the plan states day 1 has `gapDelta = null` and `direction = "stable"` — no alert. After grace period, alerts only appear when `direction != "stable"`, which requires `gapDelta` to be non-null (for gap-based directions) or a stall event. For stall directions (`new-stall`, `stall-resolved`), `gapDelta` CAN be null (breakdown < 2 days). The fallback strings "gap growing" / "gap shrinking" would only appear for those stall cases. This is defensive coding — appropriate.

**`gapSp` when developer is `improving` (gap is negative)**

Line 261: `var gapSp = entry.PaceGapSp ?? 0m;`. When a developer completes more than expected, `gap` is negative and `isBehindPace` is false. `PaceGapSp` is still set (line 188: `paceGapSp = Math.Round(gap, 1)`) even when `isBehindPace` is false. So `gapSp` will correctly show a negative value for developers ahead of pace. The frontend displays `alert.gapSp.toFixed(1) SP gap` — a negative number like `-2.0 SP gap` is technically correct but could read oddly. This is a display nuance, not a bug — the value accurately reflects the developer is ahead of pace.

**Confidence: MEDIUM** — This is a display/UX nuance. No incorrect behavior, no data error. Moved to Open Questions.

### Naming Conventions

- Record names: `DeveloperProgressAlert`, `DeveloperProgressEntry` — PascalCase, consistent with codebase.
- TS types: camelCase properties matching C# record JSON serialization defaults. Consistent.
- Vue component: `worseningAlerts`, `improvingAlerts`, `stallCount`, `deltaDescription` — consistent with existing naming in the component.

### Security

No new inputs from the request payload. No hardcoded secrets. No injection vectors introduced.

### Performance

`BuildStalledTickets` is called twice per developer (today + yesterday). This doubles the stall detection work but is O(memberships × transitions) — acceptable for sprint-scale data. The intermediate tuple approach avoids materializing additional collections unnecessarily.

### Gap Analysis

**Missing test: `stall-resolved` direction**

The plan's testing strategy lists "Stall resolved: direction is `stall-resolved` regardless of gapDelta" as a required test. No test for this scenario exists in Slices 14-19. Slice 18 covers `new-stall`. There is no Slice covering the `stall-resolved` case.

**Confidence: HIGH** — Slice numbering goes 14–19 with explicit scenario descriptions. `stall-resolved` is absent. The plan's Testing Strategy (`docs/specs/F33-DailyProgressEnhancements/delivery/plan.md:298`) explicitly lists this as a required scenario.

**Severity: LOW** — The direction logic is simple (`hadStallYesterday && hasNoStallToday`), and the existing sort order test (Slice 19) implicitly exercises parts of the alert path. The missing test is a gap in coverage completeness, not a defect in the code. The code itself is correct. This is a LOW, not a blocker.

**Missing test: sort sub-ordering (new-stall within worsening group)**

Slice 19 tests worsening-before-improving but does not test `new-stall` appearing before `worsening` within the worsening group, nor `stall-resolved` before `improving` in the improving group.

**Confidence: MEDIUM** — The sort logic is correct per analysis, but fine-grained ordering is not tested.

**Severity: LOW** — No blocker. Noted for completeness.

**Friday/Monday business-day boundary (stall-resolved)**

`yesterdayDate = todayDate.AddDays(-1)`. On Monday, yesterday = Sunday. A ticket stalled Thursday (3 business days ago per today's count) would have been stalled Friday when using Sunday as reference (Thursday to Sunday: Thu→Fri = 1 day, Sat/Sun not counted = 1 business day ≤ 2). So on Monday morning: stalled today, NOT stalled yesterday (Sunday ref) → `new-stall`. This is correct per the plan's "Friday/Monday boundary: stall computation uses business days correctly across weekends."

The "stall-resolved" boundary is symmetric: if a ticket was stalled on Friday (yesterday per Saturday, but Saturday is weekend so no stall detection runs) — this is a production nuance: `DateTime.UtcNow` runs on every API call, including weekends. On Saturday, `yesterdayDate = Friday`, and both today (Saturday) and yesterday (Friday) would see the same business-day count for the ticket. Stall state would not change on a Saturday vs Friday unless a transition occurred. This is correct behavior — no new stall or resolution fires spuriously over weekends.

## Findings

No CRITICAL or HIGH findings.

### [LOW] Missing `stall-resolved` test case
**File:** `src/Services/Fokus/Fokus.Tests/Features/Analytics/DeveloperProgressServiceTests.cs`
**Issue:** The plan's testing strategy explicitly lists "Stall resolved: direction is `stall-resolved` regardless of gapDelta" as a required test scenario. No test covers this case among Slices 14-19.
**Fix:** Add a Slice 20 that sets up a developer with a stalled ticket yesterday (transition > 2 business days ago as of yesterday, but <= 2 as of today, i.e. resolved today) and verifies `direction == "stall-resolved"`.
**Confidence:** HIGH

### [LOW] Improving developer shows negative `gapSp` in alert banner
**File:** `client/src/components/developers/DailyProgressTab.vue:116`
**Issue:** For improving developers who are ahead of pace, `alert.gapSp` will be negative (e.g., `-2.0 SP gap`). The display is technically accurate but may read oddly to users. The plan does not explicitly address this display case.
**Fix:** Consider rendering `Math.abs(alert.gapSp).toFixed(1)` with a label like `ahead by X.X SP` for improving developers, or suppress the gap display when gapSp ≤ 0.
**Confidence:** MEDIUM

## Positive Observations

- The intermediate tuple structure `(Entry, YesterdayStalledKeys, Dev)` is a clean solution: it carries yesterday's stall set into the alert-building loop without polluting the response record shape. Well-reasoned deviation from the plan.
- `BuildStalledTickets` parameterization is clean — single parameter added, single call site updated, both calls clearly labeled with `todayDate` / `yesterdayDate`. Easy to follow.
- Gap delta computation using `DailyBreakdown[^1]` and `[^2]` is correct and idiomatic C# index-from-end syntax.
- The frontend split line logic handles all three cases (both, features-only, bugs-only) with clear `v-if/v-else-if/v-else` branches and the outer `completedSp > 0` guard is correct.
- Sort logic comment is accurate and precise — matches the implementation exactly. Future maintainers will not be confused.
- KB updated with all required sections: formula, invariant, parameterization pattern, alert inclusion rule, sort order. All key files listed correctly.
- Test Slice 18 weekend guard is correctly documented and the edge case is well-understood — the comment explains exactly why Saturday/Sunday are skipped.
- `deltaDescription()` fallback strings ("gap growing" / "gap shrinking") are defensive — they handle the unlikely null gapDelta case for stall events gracefully.

## Open Questions

1. **Negative `gapSp` display for improving developers** — When a developer is ahead of pace (`gapSp < 0`), the alert banner shows e.g. `— -2.0 SP gap`. Is this the intended UX, or should the improving group show "ahead by X.X SP" instead? The code is not wrong, but the display may be confusing.

## Evidence

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Backend build | PASS | `dotnet build Fokus.API.csproj` | Build succeeded. 0 Errors, 2 Warnings (pre-existing NU1903 vulnerability in Microsoft.Build.Tasks.Core — not introduced by F33) |
| Backend tests | PASS | `dotnet test Fokus.Tests.csproj` | Failed: 0, Passed: 19, Skipped: 0, Total: 19, Duration: 81ms |
| Frontend type check | PASS | `npx tsc --noEmit` (in client/) | Zero output (clean) |
| `text-status-success` token | PASS | Read `client/src/assets/main.css` | Defined at line 23; dark/light values at lines 50 and 112 |
| Sort logic trace | PASS | Manual analysis of lines 278-284 | All four group/priority/ordering cases verified correct |
| SP invariant | PASS | Slice 10 test + code inspection | `featureCompletedSp + bugCompletedSp == completedSp` for all cases |
| KB update | PASS | Read `docs/kb/analytics/daily-progress.md` | All required sections added (F33 Bug/Feature, F33 Alerting, BuildStalledTickets) |

## Cross-Validation

Codex independently reviewed the same implementation. Comparison follows.

### Agreed (flagged by both Sonnet and Codex)

- **Missing `stall-resolved` test** — Both reviews independently identified the absence of a `stall-resolved` test scenario among Slices 14-19. Sonnet rated LOW (code is correct, coverage gap only). Codex rated MEDIUM. Final call: LOW — the direction logic is simple, the code is correct, and no other test coverage deficit rises to a blocker.

- **Missing fine-grained sort-order tests** — Both reviews noted the absence of tests verifying `new-stall` above `worsening` and `stall-resolved` above `improving` within their groups. LOW.

### Codex Only — Investigated and Assessed

**[MEDIUM] `stall-resolved` does not fire when stall resolves via ticket completion today**
**File:** `DeveloperProgressService.cs:319-324` (call to `IsCompletedInSprint`)
**Codex claim:** `BuildStalledTickets` calls `IsCompletedInSprint` with `sprintStart`/`sprintEnd` rather than `referenceDate`. A ticket completed TODAY is excluded from the "completed" check even when computing yesterday's stall set. So if a ticket was stalled yesterday but completed today: `yesterdayStalledKeys` will not contain it (excluded as completed), `isStallResolved` = false, and direction becomes gap-based (`improving`) instead of `stall-resolved`.

**Investigation result:** Confirmed as a real behavioral gap. Tracing the code path:
1. `BuildStalledTickets(..., yesterdayDate)` at line 195 calls `IsCompletedInSprint(m.TicketId, ..., sprintStart, sprintEnd, ...)` — note `sprintEnd`, not `yesterdayDate`.
2. If a ticket's done transition is today, `IsCompletedInSprint` returns `(true, today)` → ticket excluded from stall list.
3. Therefore: "yesterday's stall set" contains no ticket that was completed today, even if it was stalled as of yesterday.
4. The ticket also won't appear in today's stall set (completed → excluded). So `hadStallYesterday` = false, `isStallResolved` = false.
5. Direction falls through to gap-based — `improving` (since completing a ticket shrinks the gap).

The `stall-resolved` direction does not fire in this scenario. However: the developer does appear in alerts as `improving`, which is directionally correct (the gap improved). The signal is weaker than `stall-resolved` but not incorrect. No data is corrupted or hidden — only the direction label differs from spec intent.

**Severity: MEDIUM** — Genuine spec deviation. The fix is to pass `yesterdayDate` (or more accurately, a "yesterday end of day" timestamp) as the upper bound of `IsCompletedInSprint` when computing yesterday's stall set. This requires a signature change to `IsCompletedInSprint` or an alternate call. Escalating to developer as MEDIUM — consider fixing or documenting as known deviation.
**Confidence: HIGH**

**[MEDIUM] `new-stall` banner reports total stall count, not newly-stalled count**
**File:** `DailyProgressTab.vue:21-24`
**Codex claim:** `stallCount(alert)` returns `developer.stalledTickets.length` (total stalled today), but the message says "N ticket(s) newly stalled". If a developer had 2 stalled tickets yesterday and 1 more crosses the threshold today, the banner shows "3 tickets newly stalled" when only 1 is newly stalled.

**Investigation result:** Confirmed. `stallCount()` at line 21 returns `dev?.stalledTickets.length ?? 0` — the full count of today's stalled tickets, not the count of tickets newly stalled since yesterday. The banner text says "newly stalled" but the number represents "currently stalled." The plan spec (Step 4) says: `"N ticket(s) newly stalled" — derive stall count from the developer's stalledTickets list`. The plan wording is ambiguous — it says "stall count from stalledTickets", which is exactly what the code does. However the word "newly" in the message implies only newly-stalled count.

This is an ambiguity in the plan spec itself. The developer correctly implemented what the plan said (use `stalledTickets.length`). However the display is semantically inconsistent — "newly stalled" and "total stalled" are different things.

**Severity: LOW** — The plan said to use `stalledTickets.length` and that's what the code does. The wording discrepancy ("newly stalled" vs total count) is a spec ambiguity, not a code error. Escalating to architect if clarification is needed on whether the count should be "newly stalled count" (requires backend to expose it) or "currently stalled count" (current behavior). For now: LOW, does not block merge.
**Confidence: MEDIUM** — The plan wording is genuinely ambiguous here.

**[LOW] Missing feature-only partition test and case-sensitivity contract test**
Codex noted the absence of a "feature-only" test (developer completes only features, no bugs) and a case-sensitivity test (`"bug"` lowercase should not match). These are valid coverage gaps. The case-sensitivity requirement is tested implicitly (all test bugs use `TestData.BugTicket()` which presumably sets `IssueType = "Bug"`), but no explicit negative test exists. LOW, not a blocker.

### Sonnet Only — Not flagged by Codex

- **Negative `gapSp` display for improving developers** — Codex did not flag this UX nuance. Retained as an Open Question in this review.

### Disagreements

- **Sonnet rated "missing stall-resolved test" as LOW; Codex rated MEDIUM.** Final call: LOW. The direction logic (`hadStallYesterday && hasNoStallToday`) is a two-line boolean — high correctness confidence even without a dedicated test. The code behavior for `stall-resolved` is actually partially broken (see `IsCompletedInSprint` finding above), making a test for the current behavior potentially misleading anyway.

- **`IsCompletedInSprint` finding severity: Codex rated HIGH; Sonnet rates MEDIUM.** Sonnet investigated the full impact chain: the developer still appears in alerts as `improving`, no data is hidden, no incorrect values displayed. The deviation is direction label precision, not a correctness defect. MEDIUM is appropriate — fix warranted but not a merge blocker.

## Updated Verdict

Adding the Codex findings: two new MEDIUM issues identified (one confirmed behavioral gap in `stall-resolved` detection, one LOW-reclassified display ambiguity). No CRITICAL or HIGH issues.

**Final Verdict: APPROVE with MEDIUM notes for developer consideration.**

The `IsCompletedInSprint` finding (MEDIUM) does not block merge — developers still appear in alerts correctly as `improving` when they resolve a stall by completing a ticket. The direction label is imprecise in that specific scenario. Developer should decide whether to fix or document as a known limitation.
