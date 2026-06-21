# Team Management — Review

## Reviewed By
`reviewer` (Sonnet agent) — Sonnet-only review, no Codex cross-validation requested.

## Verdict: APPROVE

Cycle 1 fixes verified. No remaining CRITICAL or HIGH issues.

---

## Cycle 2 Fix Verification

**[HIGH] subTeams list update — FIXED.** `teamStore.ts:52-68` now captures `oldSubTeam` from the pre-update snapshot, runs `developers.value[index] = updated` first (so `stillUsed` reflects the new state), then adds the new sub-team (sorted insert) and removes the old if no other developer still holds it. Ordering is correct.

**[MEDIUM] Sidebar order — FIXED.** `AppSidebar.vue` navItems order is now: Dashboard → Developers → **Team** → Sprints → Epics → Cycle Time → Settings. Matches spec acceptance criterion.

**TypeScript type check: pass** (0 errors, re-verified after fixes).

---

## Pre-commitment Predictions

1. **subTeamProvided JSON detection pattern — fragile?** Investigated: `EnableBuffering()` is called before `JsonDocument.ParseAsync` — pattern is correct and functional.
2. **Multi-sprint exclusion semantics deviating from spec** — Confirmed: "excluded in ALL sprints" vs spec BR5 "per-sprint". Documented as a key decision in implementation.md. Moved to escalation (architecture decision, not a code bug).
3. **ExcludedDeveloperFilter applied to Dashboard** — No dedicated dashboard endpoint; Dashboard uses GetSprintSummaryEndpoint which does apply the filter. Correct.
4. **Sidebar placement violation** — Confirmed: plan says "after Developers", spec acceptance criteria says "after Developers", implementation placed it after "Cycle Time".
5. **UpdateTeamConfigRequest TypeScript type** — `string | null` vs `?` deviations: harmless for role/capacity, intentional for subTeam. Correct.

---

## Findings

### [HIGH] teamStore.updateConfig does not update subTeams list after sub-team change

**File:** `client/src/stores/teamStore.ts:49-53`

**Issue:** Plan Step 8 explicitly requires: "On success, if sub-team changed, update the `subTeams` list (add new sub-team if typed, remove old if no one else uses it)." The `updateConfig` action in the store only replaces `developers.value[index] = updated` on success but never reads or mutates `subTeams`. Consequence:

- User types a new sub-team name in `TeamRow.vue`'s "New..." input — the API call succeeds, the developer moves to the new group, but the new sub-team name never appears in other rows' existing sub-team dropdown until the full page is refreshed.
- User moves the last developer out of a sub-team — the now-empty sub-team name stays in the dropdown indefinitely until refresh.

This breaks the UX requirement that the dropdown is kept current without a full reload.

**Fix:** After `developers.value[index] = updated`, compute the new distinct sub-team list from `developers.value` and assign to `subTeams.value`. Alternatively, add/remove the specific sub-team name per the plan's description. Example:

```typescript
// After: developers.value[index] = updated
subTeams.value = [...new Set(
  developers.value
    .map(d => d.subTeam)
    .filter((s): s is string => s !== null)
)].sort()
```

---

### [MEDIUM] Sidebar "Team" entry placed after "Cycle Time" instead of after "Developers"

**File:** `client/src/components/AppSidebar.vue:62-69`

**Issue:** The spec acceptance criteria states: "Team appears as a sidebar navigation entry after Developers." The plan (Step 7) also says "after /developers". The implementation placed the Team entry after "Cycle Time" (second-to-last before Settings). The implementation.md documents this as a "presentation decision only" but the spec acceptance criterion is explicit.

Current order: Dashboard → Developers → Sprints → Epics → Cycle Time → **Team** → Settings

Required order: Dashboard → **Team** (after Developers) → Sprints → Epics → Cycle Time → Settings

**Fix:** Move the `/team` navItem in the `navItems` array to the position immediately after the `/developers` entry.

---

## Positive Observations

- **`subTeamProvided` detection pattern is solid.** Calling `EnableBuffering()` before re-reading the body with `JsonDocument` is the correct ASP.NET Core pattern. Both `"subTeam"` and `"SubTeam"` casing variants are checked, covering camelCase (JavaScript clients) and PascalCase (C# test clients).
- **`ExcludedDeveloperFilter` is clean and testable.** Pure static method with no side effects. The capacity-then-default-fallback logic matches the spec BR2 and BR3 exactly.
- **Migration is correct.** Both columns added with the right defaults (`"Developer"` / `100`), matching the entity and EF config. `Down()` reversal is present.
- **Partial update semantics are correct.** `subTeamProvided` boolean threaded through to repository cleanly distinguishes `null` (clear) from absent (no change). This is an unusual but correct solution to JSON optional-null semantics.
- **All six analytics endpoints wired.** GetDeveloperThroughput, GetSprintSummary (Dashboard), GetBugRatio, GetScopeChange, GetCarryOver, GetCycleTime — all load `allDevelopers` + `capacityRecords` and apply the exclusion filter. No endpoint was missed.
- **`UpdateTeamConfigValidator` covers all plan-specified rules.** `InclusiveBetween(0, 100)` for capacity, `NotEmpty()` + allowed-list for role. `AllowedRoles` uses `StringComparer.OrdinalIgnoreCase` for robustness.
- **Frontend types match backend shapes.** `TeamDeveloperDto` fields are structurally identical to `UpdateTeamConfigResponse` — JSON camelCase serialization aligns them correctly. The `updateTeamConfig` function typing is accurate.
- **Build passes with 0 errors.** Only pre-existing NU1903 NuGet advisory warnings (unrelated to this feature).
- **TypeScript type check passes with 0 errors.**
- **`SetSubTeam` endpoint and `UpdateSubTeamAsync` repository method cleanly removed.** No orphaned references found.
- **`SettingsView.vue` cleanup is complete.** All sub-team state variables, functions, imports, and the `Promise.allSettled` entry are removed with no residue.
- **`groupedBySubTeam` sorting is correct functionally.** Sorting is delegated to `TeamTable.vue`'s `sortedGroups()` (inactive at bottom, alphabetical within active, Unassigned group last) — the spec behaviour is met even though sorting is in the component rather than the computed.

---

## Gaps

- **`subTeams` dropdown not kept current without refresh** — covered in HIGH finding above.
- **`onNewSubTeam` fires on `change` not `blur/enter`**: `TeamRow.vue:32-37` uses `@change` on the text input. For `<input type="text">`, `change` fires on blur, not on keystroke — user has to click away to save. This matches how the role/capacity inputs work, so it is consistent, but it means if a user types in the "New..." box and immediately clicks a different row's input, the sub-team save fires. This is a UX edge case, not a bug.
- **No validation on the frontend for capacity input outside 0–100**: `onCapacityChange` at `TeamRow.vue:22` does check `value >= 0 && value <= 100`, but silently drops invalid input rather than showing an error. The server will also reject it with 400 (validator). The silent drop is acceptable for this feature scope.
- **Dashboard exclusion covers metric cards but spec also mentions health score inputs** — the health score in `SprintSummaryService.ComputeHealthScore` uses `selected` metrics which come from `FilterMemberships` (which applies `excludedDeveloperIds`). This is correct — health score is derived from filtered memberships.

---

## Open Questions

- **Multi-sprint exclusion "all-sprints" vs "per-sprint" semantics**: The spec BR5 says "in multi-sprint views, they appear for sprints where they contributed and are absent for sprints where they didn't." The implementation uses "excluded in ALL selected sprints" for developer-list filtering in multi-sprint mode, then passes those `excludedDeveloperIds` to `FilterMemberships` for each sprint's team totals. This means: a developer who was 0%-capacity-no-work in sprint 25 but had work in sprint 26 would NOT be in `excludedInAll`, so they appear in the multi-sprint developer table and their memberships appear in both sprints' team totals. This is consistent with "show them if they contributed in any sprint" — arguably the correct user-facing behaviour for a developer table. However, spec BR5 says their sprint 25 data should show them as absent. The current implementation would show them in the developer table for all sprints with zeroes for sprint 25, which is different from "absent." This is an architecture call: should multi-sprint developer tables show a zero row for the excluded sprint or omit the developer entirely from that sprint's data? Escalated to architect for decision.

---

## Evidence

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Backend build | **pass** | `dotnet build src/Services/Fokus/Fokus.API/Fokus.API.csproj` | Build succeeded. 0 Error(s), 2 pre-existing NU1903 warnings |
| Frontend type check | **pass** | `cd client && npx tsc --noEmit` | No output (0 errors) |
| SetSubTeam deleted | **pass** | `Glob **/*SetSubTeam*` in `src/` | No files found |
| Migration present | **pass** | `Glob **/*AddDeveloperTeamConfig*` | `20260509214118_AddDeveloperTeamConfig.cs` found |
| SettingsView sub-team cleanup | **pass** | `Grep subTeam\|getDevelopers\|setDeveloperSubTeam` in `SettingsView.vue` | No matches found |
| All 6 analytics endpoints wired | **pass** | `Grep ExcludedDeveloperFilter\|excludedIds` in `Features/Analytics` | 7 files matched (filter + 6 endpoints) |
