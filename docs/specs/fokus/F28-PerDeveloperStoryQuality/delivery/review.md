# Per-Developer Story Quality — Review

## Reviewed By
`reviewer` (Sonnet agent) — fix-cycle re-review (Cycle 2/3). No Codex cross-validation on re-review cycles per protocol.

---

## Verdict: APPROVE

All CRITICAL and HIGH issues from Cycle 1 are resolved. No new issues introduced by the fix. MEDIUM and LOW findings from Cycle 1 remain as recorded (unchanged, no new blockers).

---

## Pre-commitment Predictions (Cycle 2)

1. **HIGH 1 fix correctness** — streak window extended to 12 sprints: expected correct, needed to verify membership loading and TE loading for non-sparkline streak sprints. **Confirmed fixed and correct.**
2. **HIGH 1 fix regressions** — deduplication of `allLoadedSprints`, transition loading extension: expected clean, needed to verify concat/GroupBy logic. **Confirmed clean.**
3. **HIGH 2 (developer claims already correct)** — `GetSprintIdsWithQaDataAsync` tiebreaker: expected to find it already present, needed hard evidence. **Confirmed: two-step BR8 tiebreaker was already correct in initial implementation.**
4. **MEDIUM (developer claims already correct)** — `directTeIds` excludes parent TEs from pass rate/bugs: expected correct, needed hard evidence. **Confirmed: `directTeIds` built only from `activeTicketKeys` (developer's own scope), parent TE IDs never added.**
5. **New issues from streak fix** — possible N+1 in streak TE loading, missing membership data for streak sprints. **No N+1: membership load is batched via `GetSprintsWithMembershipsAsync(streakIds)`. Sparkline TE reuse is correct.**

---

## Findings

### HIGH 1 — Below-median streak capped at sparkline window — FIXED

**Verified fixed.**

Endpoint `GetDeveloperQualityEndpoint.cs` lines 107-137:
- `candidateIds` = all closed sprint IDs up to and including the target sprint.
- `qaFilteredIds` = `candidateIds` filtered via `GetSprintIdsWithQaDataAsync` (BR8 semantics).
- `streakIds = qaFilteredIds.TakeLast(12)` — capped at 12, documented rationale in comment.
- `streakSprints` loaded with memberships via `GetSprintsWithMembershipsAsync(streakIds)` (line 127).
- TEs loaded for all streak sprints (line 131-137), reusing sparkline TEs where IDs overlap — no redundant DB round-trip.
- Transition loading (line 148-153) extended to include `streakWindow.Select(s => s.Id)`.
- `allLoadedSprints` deduplication via `.GroupBy(s => s.Id).Select(g => g.First())` (lines 166-172) — correct.

Service `DeveloperQualityService.cs` lines 367-423:
- `ComputeBelowMedianStreak` signature now takes `streakSprints` + `streakTEsBySprintId` directly.
- No fallback chain — every sprint in `streakSprints` is pre-filtered to QA-bearing sprints, so `sprintTEs.Count == 0` check at line 393 is a correct defensive guard.
- Call site (lines 167-171) passes `streakWindow` and `streakTEsBySprintId` correctly.
- `filteredDevelopers` passed to the streak (line 170) correctly applies sub-team scoping for the median recalculation (BR16).

---

### HIGH 2 — `GetSprintIdsWithQaDataAsync` missing BR8 tiebreaker — CONFIRMED ALREADY CORRECT

**Developer's claim verified. The tiebreaker was present in the initial implementation.**

`TestExecutionRepository.cs` lines 259-293:
- Two-step implementation with explicit comment referencing BR8 tiebreaker.
- Step 1 (lines 265-269): finds candidate TE IDs linked to any ticket in the input sprint set.
- Step 2 (lines 277-292): groups by TE ID, applies `g.Max(x => x.SprintId)` tiebreaker, filters to only TEs whose attributed sprint is in the candidate set and non-cancelled, returns `Distinct()` sprint IDs.
- This exactly mirrors `GetTestExecutionsForSprintAsync` semantics — a sprint is returned as "has QA data" only if it would receive at least one TE after the tiebreaker is applied.
- No fix was needed. No fix was made. Confirmed correct.

---

### MEDIUM — Sub-task inheritance bleeds into pass rate and bugs found — CONFIRMED ALREADY CORRECT

**Developer's claim verified. The separation was present in the initial implementation.**

`DeveloperQualityService.cs` lines 222-257:
- Coverage loop (lines 224-238): sub-task inheritance block at lines 231-238 calls only `coveredKeys.Add(key)`. It does NOT add any parent TE IDs to any collection.
- `directTeIds` (lines 250-254): built exclusively from `activeTicketKeys` (the developer's own active-scope tickets) via `testsTesByTicket`. Parent TE IDs are never included.
- `devTEs` (line 257): filtered to `directTeIds` — contains only TEs for the developer's own ticket keys.
- Pass rate (line 258) and bugs found (lines 260-270) both use `devTEs` only — inheritance-scoped to coverage per BR10.
- No fix was needed. No fix was made. Confirmed correct.

---

### MEDIUM — `AddError`/`SendErrorsAsync` in HandleAsync (carried from Cycle 1, no change)

**File:** `src/Services/Fokus/Fokus.API/Features/Analytics/GetDeveloperQuality/GetDeveloperQualityEndpoint.cs:44-47`

Not a blocker. Reference endpoint (`GetDeveloperThroughputEndpoint.cs`) uses the same pattern. This is a pre-existing deviation in the Analytics feature area. Architect decision on whether to migrate. Does not block approval.

---

### LOW — Warning flag tooltip text (carried from Cycle 1, no change)

No change. Tooltip wires dynamic streak count correctly. Matches spec intent.

---

### LOW — `sprintMode` prop not passed separately to `QualityDevTable` (carried from Cycle 1, no change)

No change. Sensible simplification — `isSingleSprint` boolean carries the same information. No functional issue.

---

## Positive Observations

- **Streak fix is clean and complete.** The developer correctly identified that both the data-loading scope (endpoint) and the algorithm signature (service) needed updating. No shortcuts.
- **Sparkline TE reuse in streak loading** (endpoint line 133-135) avoids redundant DB calls for the overlapping 4-sprint window — good performance awareness.
- **Transition loading correctly extended** to cover streak window sprints — the `GetActiveFeatureTicketKeysForDev` call inside `ComputeBelowMedianStreak` relies on `transitionsByTicket`, and those transitions are now pre-loaded for all streak sprints.
- **`allLoadedSprints` deduplication** via `GroupBy/First` is correct and avoids duplicate membership data being passed to the service.
- **Lessons file updated correctly** — developer added two new lessons covering the streak window separation and call-site update discipline.
- **All originally correct code (HIGH 2, MEDIUM) was left unchanged** — developer did not introduce any churn to "show a fix" on things that were already correct.

---

## Gaps

- **Streak cap at 12** is a documented design decision (comment at endpoint line 124). The spec (BR15) states no explicit cap. The cap is reasonable for practical use but would silently truncate a streak > 12 sprints. Accepted as-is given the comment and practical bound.
- **No automated test coverage** for the streak-at-8 or streak-at-12 scenario — consistent with the project's overall lack of unit tests for analytics services. Not introduced by this feature.

---

## Open Questions

- **`AddError`/`SendErrorsAsync` in HandleAsync** (MEDIUM): does the Analytics feature area have an established local convention that diverges from csharp.md? Architect to decide.

---

## Evidence

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Backend build (Fokus.API) | PASS | `dotnet build Fokus.API/Fokus.API.csproj` | Build succeeded, 2 warnings (pre-existing NU1903), 0 errors |
| Frontend build (Vite) | PASS (pre-existing error only) | `npm run build \| grep "error TS"` | 1 TS error in `SettingsView.vue` (pre-existing); 0 errors in F28 files |
| HIGH 1 fix — streak window loading | PASS | Read endpoint lines 107-137 | `streakIds = qaFilteredIds.TakeLast(12)`, memberships loaded, TEs loaded with sparkline reuse |
| HIGH 1 fix — service signature | PASS | Read service lines 367-424 | `ComputeBelowMedianStreak` takes `streakSprints` + `streakTEsBySprintId`; loop correct |
| HIGH 2 — BR8 tiebreaker | PASS | Read repository lines 259-293 | `g.Max(x => x.SprintId)` tiebreaker present; two-step semantics match `GetTestExecutionsForSprintAsync` |
| MEDIUM — directTeIds scope | PASS | Read service lines 222-257 | `directTeIds` built from `activeTicketKeys` only; parent TE IDs never included |
| Transition coverage for streak | PASS | Read endpoint lines 148-153 | `streakWindow.Select(s => s.Id)` included in `transitionSprintIds` |
| allLoadedSprints deduplication | PASS | Read endpoint lines 166-172 | `GroupBy(s => s.Id).Select(g => g.First())` applied |
