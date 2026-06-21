# Settings Gap Fill — Review

## Reviewed By
`reviewer` (Sonnet agent — claude-sonnet-4-6)

## Verdict: APPROVE

---

## Pre-commitment Predictions

| Prediction | Actual |
|---|---|
| `UpdateSubTeamAsync` may double-save or miss save | Correctly split: repository mutates state only, endpoint calls `SaveChangesAsync` — matches `SetCapacityEndpoint` pattern |
| `SetSubTeamEndpoint` missing validator class | Confirmed absent, but plan explicitly states "no additional validation needed beyond 404 check" — acceptable |
| `SyncBackSprintCount` may be missing from `SaveSettingsEndpoint` AppSettings construction | Correctly mapped at line 21 |
| Frontend may miss `syncBackSprintCount` in form/store | Correctly wired in both form (line 44) and store (line 26) |
| `Future` sprint state addition may cause issues | No issues — additive type union, no existing consumers broken |

---

## Findings (Cycle 1 — all resolved)

### [HIGH] ~~Migration column default is 0, not 20~~ — FIXED

**File:** `src/Services/Fokus/Fokus.Persistence/Migrations/20260509192107_AddSyncBackSprintCount.cs:18`

**Fix applied:** `defaultValue: 0` → `defaultValue: 20`. Verified correct.

---

### [MEDIUM] ~~`SaveSettings` silently wipes `ExcludedFromScopeStatuses`~~ — FIXED

**File:** `src/Services/Fokus/Fokus.API/Features/Settings/SaveSettings/SaveSettingsEndpoint.cs`

**Fix applied:** Endpoint now reads existing settings first (`repository.GetAsync(ct)`) and carries `existing.ExcludedFromScopeStatuses` into the new `AppSettings` object. Data loss eliminated. Verified correct.

---

### [LOW] `syncAll()` slices on the full Jira sprint list including future sprints

**File:** `client/src/views/SettingsView.vue:389-396`

**Issue:** `getJiraSprints(form.boardId)` returns sprints of all states (Active, Closed, Future since `SprintState` now includes `'Future'`). `jiraSprints.slice(-form.syncBackSprintCount)` takes the last N — if future sprints appear at the end of the list (which Jira typically returns in date order), the "last N" could include future sprints in the range passed to `syncSprints(first.id, last.id)`. The separate `syncBacklog()` call handles future sprints via the backlog endpoint, so this could result in duplicate future-sprint processing.

**Self-audit:** Confidence LOW. The behaviour depends on how Jira orders sprints and what `GetJiraSprintsEndpoint` returns. The plan's out-of-scope note says future sprints are handled by `syncBacklog()`. Moving to LOW — this may be benign if `syncSprints` handles duplicate future sprints gracefully (idempotent upsert). Leaving as LOW for awareness.

---

## Positive Observations

- The `UpdateSubTeamAsync` / `SaveChangesAsync` split is correct and matches the `SetCapacityEndpoint` pattern precisely. No double-save, no missing save.
- `GetDevelopersEndpoint` returns all developers from the repository; frontend filters to `isActive`. This keeps the endpoint general-purpose while meeting the UI requirement — good separation.
- `Promise.allSettled` batch in `onMounted` correctly includes `getDevelopers()` — no blocking sequential calls.
- Per-developer save state (`developerSaving`, `developerSaved`, `developerError` as `Record<string, ...>`) is a clean pattern for inline-save-per-row UX with no shared state bleed.
- `availableExcludedStatuses` computed property correctly excludes already-excluded statuses, preventing duplicates without extra logic in `addExcludedStatus`.
- `syncBackSprintCount` flows end-to-end correctly: domain default → migration (default 20) → response DTO → store default → form reactive → `syncAll()` usage → validation rule.
- `encodeURIComponent(accountId)` in `developers.ts:9` is correct defensive practice for account IDs that may contain special characters.
- The `selectedExcludedStatus` initialization (lines 233–235) is correctly placed in the `statusesResult.status === 'fulfilled'` block alongside `selectedStatus`, keeping logic consolidated as noted in Key Decisions.

---

## Gaps

- No UI guard prevents "Sync Range" when `fromSprintId > toSprintId` (by position). The backend will accept it and attempt to sync in reverse order. Low-risk since the backend `syncSprints` likely handles it gracefully, but user experience is undefined.
- The Sub-Team Management section shows an empty-state message "No active developers found. Sync from Jira to discover developers." — but there is no loading state indicator while `getDevelopers()` is in-flight. The section shows "No active developers found" during the brief load window, which may confuse users. This is a UX gap but not a correctness issue.

---

## Open Questions

None — all findings are conclusive from the code.

---

## Evidence

| Check | Result | Command | Output |
|---|---|---|---|
| Build (cycle 1) | PASS | `dotnet build` | `Build succeeded. 0 Error(s), 2 Warning(s)` |
| Migration default (cycle 1) | FAIL | Read `20260509192107_AddSyncBackSprintCount.cs:18` | `defaultValue: 0` — should be `20` |
| ExcludedFromScopeStatuses preservation (cycle 1) | FAIL | Read `SaveSettingsEndpoint.cs` | New `AppSettings` object omitted field, silently clearing on save |
| Build (cycle 2) | PASS | `dotnet build` | `Build succeeded. 0 Error(s), 2 Warning(s)` |
| Migration default (cycle 2) | PASS | Read `20260509192107_AddSyncBackSprintCount.cs:18` | `defaultValue: 20` — correct |
| ExcludedFromScopeStatuses preservation (cycle 2) | PASS | Read `SaveSettingsEndpoint.cs:11-25` | `existing` loaded first; `ExcludedFromScopeStatuses = existing.ExcludedFromScopeStatuses` carried through |
| SetSubTeamEndpoint 404 path | PASS | Read `SetSubTeamEndpoint.cs:27-30` | Null-checks repository return, sends 404 before SaveChangesAsync |
| SaveChangesAsync split | PASS | Compare `SetSubTeamEndpoint.cs:33` vs `SetCapacityEndpoint.cs:19` | Pattern matches: repository mutates, endpoint saves |
| SyncBackSprintCount validation | PASS | Read `SaveSettingsCommand.cs:69-71` | `InclusiveBetween(1, 50)` present |
| Frontend form wiring | PASS | Read `SettingsView.vue:44, 182, 396` | Default, syncFromStore, and syncAll() all use `syncBackSprintCount` |
| Store default | PASS | Read `settingsStore.ts:26` | `syncBackSprintCount: 20` present |
| Developer type | PASS | Read `types/index.ts:123-129` | `Developer` interface matches plan spec |
| getDevelopers API fn | PASS | Read `api/developers.ts:4-6` | `GET /api/developers` correctly called |
| setDeveloperSubTeam API fn | PASS | Read `api/developers.ts:8-13` | `PUT /api/developers/{accountId}/sub-team` with body `{ subTeam }` |
| Frontend isActive filter | PASS | Read `SettingsView.vue:251` | `filter(d => d.isActive)` applied after developersResult |
| Promise.allSettled batch | PASS | Read `SettingsView.vue:199-215` | getDevelopers() included in parallel batch |
| SprintState Future | PASS | Read `types/index.ts:121` | `'Future'` added to union type |
| Route binding pattern | PASS | Compare `SetSubTeamRequest.AccountId` vs `SetCapacityRequest.AccountId` | FastEndpoints auto-binds `{accountId}` route segment to `AccountId` property — matches established pattern |
