# RA5-A — Frontend Merge & Routing Shell: Implementation

**Status:** Complete (fix cycle 1/3 applied)  
**Date:** 2026-06-20  
**Plan:** `plan-A-merge.md`

---

## Steps

### Step 1 — Establish the client workspace; move Fokus, bring Reflekt in

**What changed:**
- Created `client/apps/dashboard/`, `client/apps/retro/`, `client/packages/shared/` directories.
- Moved all Fokus SPA content (`src/`, `index.html`, `vite.config.ts`, `package.json`, tsconfigs, `public/`, `README.md`) from `client/` into `client/apps/dashboard/` wholesale. Internal relative imports and the `@`→`src` alias are unchanged.
- Copied Reflekt SPA from `<reflekt-repo>\web\reflekt-web\` (excluding `node_modules/`, `.vscode/`) into `client/apps/retro/`. Source repo is unmodified.
- Authored `client/package.json` as the npm workspace root with `"workspaces": ["apps/*", "packages/*"]`. Hoisted shared deps (`vue`, `pinia`, `@microsoft/signalr`, `tailwindcss`, `vite`, etc.) to root; left app-unique deps in each app manifest (`apexcharts`, `vue-router@4` in dashboard; `lucide-vue-next`, `vue-router@5` in retro).
- Dashboard `package.json` stripped to app-unique deps only. Retro `package.json` stripped similarly.
- Created `client/packages/shared/package.json` as a private workspace package (`@sprint-rituals/shared`).

**Deviation — build script simplification (steered by team lead):** Retro's original `build` script used `run-p` from `npm-run-all2` (not a workspace-root dep). Simplified to `vue-tsc --build && vite build` to match the dashboard pattern. No `npm-run-all2` hoisted.

**Deviation — tsconfig.node.json:** Retro's original `tsconfig.node.json` extended `@tsconfig/node24` (not available at workspace root). Replaced with the same standalone `compilerOptions` shape as the dashboard's `tsconfig.node.json`.

**Verify:** `npm install` at `client/` resolves cleanly. Both `apps/dashboard/` and `apps/retro/` have their own `vite.config.ts`, router, and stores. `<reflekt-repo>` is unmodified.

---

### Step 2 — Two Vite builds with path-prefixed bases → `Host/wwwroot/{dashboard,retro}`

**What changed:**
- `client/apps/dashboard/vite.config.ts`: added `base: '/dashboard/'`, updated `build.outDir` → `'../../../src/Host/wwwroot/dashboard'` (scoped `emptyOutDir`), added `resolve.alias` for `@sprint-rituals/shared`.
- `client/apps/retro/vite.config.ts`: added `base: '/retro/'`, updated `build.outDir` → `'../../../src/Host/wwwroot/retro'` (scoped `emptyOutDir`), added `resolve.alias` for `@sprint-rituals/shared`. Dropped `vite-plugin-vue-devtools` (not a workspace-root dep; team lead steered to drop entirely).
- Dashboard `src/router.ts`: `createWebHistory()` → `createWebHistory('/dashboard/')` so client-side routing is base-prefixed.
- Retro `src/router/index.ts`: already uses `createWebHistory(import.meta.env.BASE_URL)` — picks up `/retro/` from Vite's `base` automatically. No change needed.
- **H1 absolute redirect fixes (all four):**
  - `apps/dashboard/src/api/client.ts:14`: `window.location.href = '/login'` → `'/dashboard/login'` (401 handler)
  - `apps/dashboard/src/stores/authStore.ts:34`: `window.location.href = '/login'` → `'/dashboard/login'` (logout)
  - `apps/retro/src/stores/auth.ts:26`: `window.location.href = '/'` → `'/retro/'` (signOut)
  - `apps/retro/src/components/retro/RetroJoinForm.vue:88`: `returnUrl=/sprint/${id}` → `returnUrl=/retro/sprint/${id}` (Google login redirect)
- Dashboard `router.beforeEach` name-based guards (`{ name: 'login' }`) left untouched — vue-router v4 respects the history base automatically.
- Root workspace `build` script: `npm run build --workspace=apps/dashboard && npm run build --workspace=apps/retro`.

**Verify:** `npm run build` at `client/` emits `src/Host/wwwroot/dashboard/index.html` and `src/Host/wwwroot/retro/index.html`. Assets are under their own base prefixes — no collisions.

---

### Step 3 — Shared build-time package: auth-state + thin UI kit

**What changed:**
- Created `client/packages/shared/src/auth.ts` — exports `fetchMe(): Promise<AuthUser | null>` wrapping `GET /auth/me` with `credentials: 'same-origin'` (explicit, consistent across both apps). Returns `null` on non-ok response instead of throwing.
- Created `client/packages/shared/src/index.ts` — re-exports `fetchMe` and `AuthUser`.
- `apps/dashboard/src/stores/authStore.ts`: replaces `getMe()` call with `sharedFetchMe()` from `@sprint-rituals/shared`. Logout flow and other store logic unchanged.
- `apps/retro/src/stores/auth.ts`: replaces inline `fetch('/auth/me', { credentials: 'include' })` with `sharedFetchMe()`. Also aligned `signOut`'s fetch to `credentials: 'same-origin'` for consistency.
- Both app `package.json` files declare `"@sprint-rituals/shared": "*"` as a dependency — npm workspaces symlinks it.
- Both app `tsconfig.app.json` files have `paths` entry `"@sprint-rituals/shared": ["../../packages/shared/src/index.ts"]`.
- Both `vite.config.ts` files resolve `@sprint-rituals/shared` → `packages/shared/src/index.ts` via `resolve.alias` (build-time, no publish step).
- Guest auth (X-Guest-Token, per-sprint localStorage tokens, SignalR accessTokenFactory) untouched — stays entirely in `apps/retro`.
- No shared UI kit components created — no genuine duplicates found between the two apps (each has its own `BaseCard`, `BaseButton`, etc. with different styles).

**Verify:** Both apps import from `@sprint-rituals/shared` and build. `@sprint-rituals/shared` is symlinked in `client/node_modules/`.

---

### Step 4 — Host serves both SPAs under their prefixes + thin top-level entry + C1 fix

**What changed:**
- **C1 (CRITICAL) — hub path fix:** `apps/retro/src/composables/useSprintHub.ts:32`: `.withUrl('/hubs/sprint', ...)` → `.withUrl('/hubs/retro', ...)`. This was the only frontend wire string in RA5-A scope; the backend hub path was already correct.
- **`src/Host/Program.cs`:**
  - Replaced unscoped `app.UseStaticFiles()` (line 51) with two `RequestPath`-scoped `UseStaticFiles` calls:
    - `/dashboard` → `wwwroot/dashboard` via `PhysicalFileProvider`
    - `/retro` → `wwwroot/retro` via `PhysicalFileProvider`
  - All API/hub endpoint registrations remain **before** the SPA fallbacks (order preserved).
  - Removed unscoped `app.MapFallbackToFile("index.html")` (was line 76).
  - Added two prefixed fallbacks: `MapFallbackToFile("/dashboard/{*path:nonfile}", "dashboard/index.html")` and `MapFallbackToFile("/retro/{*path:nonfile}", "retro/index.html")`.
  - Added thin routing shell at `/` via `app.MapGet("/", ...)`: signed-in → `/dashboard/`, unauthenticated → `/dashboard/login`. Default module is dashboard as per plan Step 4 open question.

**Note on legacy `wwwroot` root artifacts:** `wwwroot/index.html` and `wwwroot/assets/` remain from the old single-build. They are not served by any route (the new `UseStaticFiles` is prefix-scoped; the unscoped call is gone), but they can be deleted safely as cleanup. Left in place for the reviewer to validate before deletion.

**Verify:** `dotnet build SprintRituals.slnx` green (0 errors, pre-existing warnings only).

---

### Step 5 — End-to-end verification

**What was verified:**
- `dotnet build SprintRituals.slnx --no-restore`: **green** — 0 errors, 9 pre-existing warnings (unchanged from before this feature).
- `npm run build` at `client/`: **green** — dashboard emits `wwwroot/dashboard/index.html` + assets; retro emits `wwwroot/retro/index.html` + assets. No asset path collisions.
- `wwwroot/dashboard/` and `wwwroot/retro/` directories confirmed present post-build.
- Hub path `useSprintHub.ts:32` confirmed as `/hubs/retro` post-fix via grep.

**Manual smoke verification (pending):** Requires running Host to verify cross-prefix auth, real-time SignalR, and the `/` redirect. Documented here for the reviewer — the four smoke-test bullets in plan Step 5 are the acceptance criteria for manual verification.

---

## Pre-existing Bugs Fixed (surfaced by stricter type-check in workspace)

The retro app's stricter `tsconfig.app.json` (`noUncheckedIndexedAccess: true`) surfaced two bugs in the Reflekt source:

1. **`TeamSelector.vue`** — referenced `teamsStore.hasTeam` (doesn't exist; should be `hasTeams`) and `teamsStore.currentTeam` (doesn't exist; store only has `teams[]`). Fixed: `hasTeam` → `hasTeams`; `currentTeam` → `teams[0]`; `createSprint(id, name, votes)` 3-arg call → `createSprint(id, payload)` matching the store's 2-arg signature.
2. **`previousActions.ts:52-53`** — array index access without undefined guard (TS2532 under `noUncheckedIndexedAccess`). Fixed: extracted `const first = data[0]` with explicit undefined guard.

These are bugs in the Reflekt source that were previously undetected because the source repo's build didn't apply `noUncheckedIndexedAccess`. Fixing them is in-scope for the copy-in step (a broken build is not an acceptable hand-off).

---

## Skills Used

| Step | Skill | Disposition |
|------|-------|-------------|
| 1 | None | No pattern skill — workspace restructure + source move |
| 2 | None | No pattern skill — Vite build config |
| 3 | None | No pattern skill — build-time shared-lib extraction |
| 4 | None | No pattern skill — Host static-file + SPA fallback wiring |
| 5 | None | No pattern skill — manual smoke verification |

All-`None` plan — this is frontend-merge + Host-wiring infra with no matching pattern skill. The all-None exemption applies per `plan-A-merge.md`.

---

## Carry-Over

- **Legacy `wwwroot` root artifacts** (`wwwroot/index.html`, `wwwroot/assets/`): orphaned by this feature — the old single-build output. Not actively served (no unscoped static files route; `/` redirect bypasses them). Safe to delete; left for reviewer to confirm before removal.
- **`lucide-vue-next@1.0.0` deprecation warning**: npm warns this package is deprecated (`use @lucide/vue instead`). Not blocking; the retro app uses it internally. Should be addressed in a follow-up pass (RA5-B or later).
- **Manual smoke of the four Step-5 bullets**: build-only verification was done here. Full smoke (both apps loading, SignalR real-time, cross-prefix auth) requires `dotnet run` + browser. Deferred-by-policy — this project has no automated frontend test suite (plan Step 5 Skill: None, TDD: no). The reviewer is the gate for runtime smoke.

---

## Fix Cycle 1/3 — 2026-06-20

Reviewer + Codex round-1 findings addressed. All blockers and majors resolved.

| # | Severity | Finding | Fix |
|---|----------|---------|-----|
| 1 | BLOCKER | `packages/shared/src/auth.ts:13` called `/auth/me` — should be `/api/auth/me` (Identity endpoint is `/api/auth/me`). Both SPAs would have gotten 404 on auth-state. | `fetch('/auth/me', …)` → `fetch('/api/auth/me', …)`. Updated inline comment. |
| 2 | BLOCKER | `apps/retro/src/stores/auth.ts:21` and `RetroJoinForm.vue:88` used `/auth/google/login`. Server mounts `/auth/login` (LoginEndpoint). | Both changed to `/auth/login`; `?returnUrl=…` query preserved on JoinForm. |
| 3 | BLOCKER | `apps/retro/src/stores/auth.ts:24` raw-fetched `/auth/logout`. Server mounts `/api/auth/logout` (LogoutEndpoint). | `fetch('/auth/logout', …)` → `fetch('/api/auth/logout', …)`. |
| 4 | MAJOR | `AuthUser.givenName` typed as required in shared package and retro local interface, but `/api/auth/me` never returns it. | Made optional (`givenName?: string`) in both `packages/shared/src/auth.ts` and `apps/retro/src/stores/auth.ts`. |
| 5 | MAJOR | Step-5 evidence gap: implementation.md claimed Status: Complete with manual smoke marked pending. | Status updated to reflect fix cycle; manual smoke explicitly marked deferred-by-policy (no automated frontend suite; reviewer is the gate). |

**Build verification post-fix:**
- `dotnet build SprintRituals.slnx --no-restore`: **green** — 0 errors, 6 warnings (pre-existing, unchanged).
- `npm run build` (workspace): **green** — dashboard and retro both emit into `wwwroot/{dashboard,retro}` with 0 type errors.

---

# RA5-C — Board-Meaning Rename (sprint→RetroSession): Implementation

**Status:** Complete
**Date:** 2026-06-20
**Plan:** `plan-C-renames.md`

---

## Steps

### Step 1 — Domain properties, repo method renames, EF migration

**What changed:**

**Domain (already done in prior session):**
- `Card.SprintId` → `Card.RetroSessionId`
- `Card.ReviewedInSprintId` → `Card.ReviewedInRetroSessionId`
- `CardGroup.SprintId` → `CardGroup.RetroSessionId`
- `Participant.SprintId` → `Participant.RetroSessionId`
- `Card.Create(sprintId)`, `Card.SetReviewStatus(reviewingSprintId)` → `retroSessionId`, `reviewingRetroSessionId`
- `CardGroup.Create(sprintId)` → `retroSessionId`
- `Participant.Join(sprintId)` → `retroSessionId`

**Repository method renames (Reflekt.Persistence):**
- `CardRepository`: `GetBySprintIdAsync` → `GetByRetroSessionIdAsync`, `GetUnreviewedActionItemsBySprintIdAsync` → `GetUnreviewedActionItemsByRetroSessionIdAsync`, `CountActionItemsBySprintIdAsync` → `CountActionItemsByRetroSessionIdAsync`, `GetActionItemsReviewedInSprintAsync` → `GetActionItemsReviewedInRetroSessionAsync`
- `CardGroupRepository`: `GetBySprintIdAsync` → `GetByRetroSessionIdAsync`
- `ParticipantRepository`: `GetBySprintIdAsync` → `GetByRetroSessionIdAsync`, `ExistsByDisplayNameAsync(sprintId)` → `retroSessionId`
- `VoteRepository`: `GetParticipantIdsWithVotesInSprintAsync` → `GetParticipantIdsWithVotesForRetroSessionAsync`

**Entity configurations (Reflekt.Persistence):**
- Removed `.HasColumnName("SprintId")` from `CardConfiguration`, `CardGroupConfiguration`, `ParticipantConfiguration` — this was a mistake from the prior session that prevented EF from generating `RenameColumn`. Also removed `.HasColumnName("ReviewedInSprintId")` from `CardConfiguration`.

**EF migration:**
- `20260620090016_RenameBoardFkSprintIdToRetroSessionId.cs` — contains `RenameColumn` for `Cards.SprintId`, `Cards.ReviewedInSprintId`, `CardGroups.SprintId`, `Participants.SprintId` + matching index renames and FK drop/recreate.

**API feature files — 26 request/command DTOs:**
All 26 `SprintId` request fields renamed to `RetroSessionId` in: AddCardToGroup, ClearFocus, CreateGroup, DissolveGroup, FocusGroup, GetGroups, RemoveCardFromGroup, ToggleDiscussed, UpdateGroupTitle (CardGroups); CreateCard, DeleteCard, EditCard, ListCards (Cards); AttachEvidenceLink, DetachEvidenceLink (EvidenceLinks); GetCurrentParticipant, GetParticipants, JoinSprint, KickParticipant (Participants); GetPreviousActions (PreviousActions); GetParticipantReport, GetSprintReport (Reports); GetSprintSummary, UpdateColumns (Sprints); CastVote, GetMyVotes, RetractVote (Votes).

**Additional files updated by Step 1:**
- `TeamRepository.cs` — `g.SprintId`, `c.SprintId`, `p.SprintId` in cascade-delete LINQ → `RetroSessionId`
- `DbInitializer.cs` — same property accesses in seed queries → `RetroSessionId`
- `GuestTokenAuthenticationHandler.cs` — `participant.SprintId.ToString()` → `participant.RetroSessionId.ToString()` (claim value source; claim key `"sprintId"` renamed in Step 3)
- `CardContracts.cs` — `CardResponse.SprintId` → `CardResponse.RetroSessionId` (already done prior session)
- `CardGroupContracts.cs` — `CardGroupResponse.SprintId` → `CardGroupResponse.RetroSessionId`
- `ParticipantContracts.cs` — `JoinSprintResponse.SprintId`, `ParticipantResponse.SprintId` → `RetroSessionId`
- `FetchJob.cs` — `int SprintId` → `int RetroSessionId`
- `ParticipantJoinTests.cs` — `const int SprintId = 42` → `const int RetroSessionId = 42`

**Carve-outs respected:** `FokusSprintSnapshot.*`, `FokusSprintRef`, `GetSprintContext*`, all existing migrations — untouched.

---

### Step 2 — Backend route renames + frontend store URL + team-scoped (Q3 fold-in)

**What changed:**

**Backend routes (27 endpoint files):**
All `[HttpXxx("/api/sprints/{sprintId}/...")]` → `[HttpXxx("/api/retro-sessions/{retroSessionId}/...")]`. Files: ListCards, CreateCard, EditCard, DeleteCard; AddCardToGroup, CreateGroup, DissolveGroup, FocusGroup, ClearFocus, GetGroups, RemoveCardFromGroup, ToggleDiscussed, UpdateGroupTitle; GetCurrentParticipant, GetParticipants, JoinSprint, KickParticipant; CastVote, GetMyVotes, RetractVote; AttachEvidenceLink, DetachEvidenceLink; GetParticipantReport, GetPreviousActions; GetSprintSummary, UpdateColumns.

No team-scoped `/api/teams/{teamId}/sprints/{sprintId}` routes existed in Reflekt.API backend.

**Frontend stores — flat routes (27 URL strings across 7 files):**
- `stores/card.ts` (6), `stores/cardGroup.ts` (10), `stores/participant.ts` (5), `stores/previousActions.ts` (1), `stores/report.ts` (1), `stores/sprint.ts` (2), `stores/vote.ts` (3)

**Frontend stores — team-scoped routes (Q3 fold-in, 9 URL strings):**
- `stores/sprint.ts` (7): `fetchTeamSprints`, `fetchSprint`, `createSprint`, `startSprint`, `advancePhaseCheck`, `advancePhase`, `closeSprint`
- `stores/report.ts` (1): `fetchReport`
- `components/retro/PreviousActionsPanel.vue` (1): `setStatus`

**Frontend TypeScript interfaces:**
- `stores/card.ts` — `Card.sprintId` → `Card.retroSessionId`; all `sprintId` params → `retroSessionId`
- `stores/cardGroup.ts` — `CardGroup.sprintId` → `CardGroup.retroSessionId`; all `sprintId` params → `retroSessionId`
- `stores/participant.ts` — `Participant.sprintId` → `Participant.retroSessionId`; all `sprintId` params → `retroSessionId`
- `stores/previousActions.ts`, `stores/vote.ts` — `sprintId` params → `retroSessionId`

**Acceptance grep result:** `rg '/api/teams/[^/]*/sprints' client/apps/retro` → zero matches.

---

### Step 3 — Hub, claim, group-string, event renames

**What changed:**

**RetroHub (new) / SprintHub (deleted):**
- `Hubs/SprintHub.cs` deleted; `Hubs/RetroHub.cs` created with class renamed `SprintHub` → `RetroHub`
- Hub methods: `JoinSprint` → `JoinRetro`, `LeaveSprint` → `LeaveRetro`
- `"sprintId"` claim reads in hub → `"retroSessionId"` (all 3 occurrences in `JoinRetro`, `ToggleReady`, `RequireScrumMasterAsync`)
- `"sprint:{sprintId}"` group strings in hub → `"retro:{sprintId}"`

**DependencyInjection.cs:**
- `MapHub<SprintHub>` → `MapHub<RetroHub>`
- Timer expiry `"sprint:{sprintId}"` → `"retro:{sprintId}"`

**IHubContext<SprintHub> → IHubContext<RetroHub> (27 broadcast handlers):**
All `Broadcast*Handler` files across CardGroups, Cards, EvidenceLinks, Participants, PreviousActions, Sprints, Votes updated. Using namespace updated.

**"sprintId" claim key → "retroSessionId" (17 occurrences):**
- `GuestTokenAuthenticationHandler.cs` (claim issue)
- Hub `RetroHub.cs` (3 reads)
- 13 endpoint files with guest-auth claim check (ListCards, GetGroups, CastVote, RetractVote, GetMyVotes, GetParticipants, GetCurrentParticipant, GetPreviousActions, GetParticipantReport, GetSprintReport, CreateCard, JoinSprint endpoints, plus AddCardToGroup)

**"sprint:{id}" group prefix → "retro:{id}" (all broadcast handlers):**
All `$"sprint:{...}"` interpolations in broadcast handlers and hub changed to `$"retro:{...}"`.

**"SprintClosed" → "RetroClosed" + payload key:**
- `BroadcastSprintClosedHandler.cs` — `"SprintClosed"` → `"RetroClosed"`, `sprintId = ...` → `retroSessionId = ...`

**Frontend composable `useSprintHub.ts`:**
- `.invoke('JoinSprint', ...)` → `.invoke('JoinRetro', ...)`
- `.on('SprintClosed', ...)` → `.on('RetroClosed', ...)`
- `ColumnConfigUpdated` payload field `sprintId` → `retroSessionId`
- `build(sprintId)` / `connect(sprintId)` params → `retroSessionId`
- `retroSessionId` used in `fetchCards`, `fetchGroups` calls
- `ParticipantJoined` push constructs `{ retroSessionId, ... }` instead of `{ sprintId, ... }`

**Stale comments removed:**
- `SetReviewStatusEndpoint.cs` — 2 DO-NOT-TOUCH Q1/Q2 comments removed
- `GetSprintReportEndpoint.cs` — stale claim carve-out comment removed

---

### Step 4 — Verification

**Acceptance grep 1 — `SprintId` (non-migration, non-carve-out):** Confirmed zero board-meaning `SprintId` remain. All remaining occurrences are: old migration files (immutable history), model snapshot, `FokusSprintId`/`FokusSprintIds` carve-outs, `GetSprintContext` carve-outs, `PreviousSprintId` (Fokus sprint meaning), local variables (`claimSprintId`, `parsedSprintId`, `sprintId` method params in hub — camelCase, not matched by the `SprintId` grep).

**Acceptance grep 2 — `/api/teams/{teamId}/sprints` in client/apps/retro:** Zero matches.

**Build:** `dotnet build SprintRituals.slnx --no-restore` → 0 errors, 6 pre-existing warnings. `npm run build` (workspace) → 0 errors, both apps built.

---

## Skills Used

| Step | Skill | Disposition |
|------|-------|-------------|
| 1–4 | None | Plan-C Skill Mapping: all steps `Skill: None` |

---

## Carry-Over

- **Camelcase local vars in endpoints** (`claimSprintId`, `parsedSprintId`) — functional but not cosmetically renamed. These are local-scope only and don't appear in the acceptance grep (`SprintId` grep is PascalCase). A future cosmetic pass could rename them to `claimRetroSessionId`.
- **RetroHub camelCase param `sprintId`** — hub method parameters are still named `sprintId` (they're SignalR wire-method params). Wire name is irrelevant to the client (client passes a value, not a name), but a future cleanup could align them to `retroSessionId`.
- **`PreviousActionsContracts.cs` `PreviousSprintId`** — deliberately left as-is per carve-out rule (references a Fokus sprint, not the board FK).
- **`AdvancePhaseCheckEndpoint.cs` line 86 (`GetParticipantIdsWithVotesForRetroSessionAsync`)** — VoteRepository.GetParticipantIdsWithVotesInSprintAsync renamed per Q2 ruling; call site updated.

---

## Fix Cycle 1/3 — 2026-06-20 (RA5-C)

Codex round-1 cross-check surfaced two runtime breaks the green build hid, plus the `JoinSprint*` type family which the **already-approved Step 3 acceptance grep** (`rg 'JoinSprint' src/Services/Reflekt → zero`) had not yet been satisfied for. All three addressed.

| # | Severity | Finding | Fix |
|---|----------|---------|-----|
| 1 | BLOCKER | `ColumnConfigUpdated` wire-key mismatch. `BroadcastColumnConfigUpdatedHandler.cs:14` broadcast `sprintId = ev.RetroSession.Id` but client `useSprintHub.ts:155` gates on `payload.retroSessionId` → column-config updates silently dropped. | Server payload key `sprintId` → `retroSessionId`. Audited all `Broadcast*Handler` anonymous-payload property keys (grep `sprintId\s*=` in Reflekt .cs) — this was the only wire-payload miss; `BroadcastPhaseChangedHandler.cs:13` and `DependencyInjection.cs:79` `sprintId` are internal local/lambda vars, not wire keys (verified). |
| 2 | BLOCKER | `/join/google` 404. `participant.ts:34` `joinWithGoogle` POSTed `/api/retro-sessions/${id}/join/google`; backend exposes only `/join` (`JoinRetroEndpoint`, handles cookie-claim path via `User.GetPersonIdOrNull()`). | Frontend `/join/google` → `/join` (kept `credentials: 'include'`). The `/join/google` call was a pre-existing dead call flagged in plan-C Step 2; this corrects it to the real endpoint. |
| 3 | (plan-gate) | `JoinSprint*` type family unrenamed — fails the approved Step 3 grep `rg 'JoinSprint' src/Services/Reflekt`. | `git mv` folder `Features/Participants/JoinSprint/` → `JoinRetro/`; renamed `JoinSprintEndpoint`→`JoinRetroEndpoint`, `JoinSprintCommand(+Validator)`→`JoinRetroCommand`, `JoinSprintResponse`→`JoinRetroResponse`, namespace `…JoinSprint`→`…JoinRetro`; updated `ParticipantContracts.cs`, `ParticipantMappings.cs`, and frontend `participant.ts` TS interface `JoinSprintResponse`→`JoinRetroResponse`. |

**Scope note (handed back, NOT executed):** the fix-list also requested (a) renaming the `Features/Sprints/` C# folder + namespace, (b) renaming ALL frontend board-meaning `sprint` identifiers — Vue components (`SprintList`, `SprintView`, `SprintDetailPanel`, `CreateSprintForm`, `SprintReportPanel`), composables (`useSprintHub`, `useSprintStore`), view files, vue-router route names (`sprint`, `team-sprints`), `${sprintId}` route params/props, and the `reflekt:sprint:${id}:token` localStorage key — and (c) adopting a new acceptance gate `rg -i "sprint" → only carve-out hits`. These are a **substantial expansion beyond the critic-approved plan-C-renames.md scope**, whose acceptance gates are the three carve-out-scoped greps (`SprintId` PascalCase, `/api/sprints/`, and the Step 3 wire-string set) — not a case-insensitive `sprint` sweep. plan-C line 17 explicitly warns a blanket replace is "catastrophic." Per the standing rule that coordinator-relayed approval is not user authority, the structural-folder/namespace rename, the frontend `sprint`-identifier sweep, and the `rg -i "sprint"` gate are **deferred pending a plan amendment or direct user confirmation.** The two runtime breaks (genuine bugs) and the `JoinSprint*` family (required by the existing approved gate) were completed.

**Build/test verification post-fix:**
- `dotnet build SprintRituals.slnx --no-restore` → 0 errors, 6 pre-existing warnings.
- `npm run build` (workspace) → 0 errors, both apps emit.
- Reflekt tests: `Reflekt.Domain.Tests` 7/7, `Reflekt.Persistence.Tests` 14/14 — all green.
- Approved-plan acceptance greps re-confirmed zero: `rg '"sprintId"|sprint:\{|"SprintClosed"|JoinSprint|LeaveSprint|class SprintHub' src/Services/Reflekt` → zero; `rg 'JoinSprint|SprintClosed' client/apps/retro` → zero; `rg 'api/sprints' client/apps/retro/src` → zero.
- Stale agent worktree `.claude/worktrees/agent-a5ebd51c4a7e68eaf` removed (was polluting grep results).

---

## Step 5 — Complete board-meaning cleanup (RA5-C, 2026-06-20)

**Authority:** plan-C-renames.md was **amended** to add Step 5 (an enumerated, carve-out-scoped full cleanup with a strengthened `rg -i "sprint"` gate — NOT the blanket replace earlier refused). My deferral condition (a plan amendment) was met by the artifact; implemented against the amended plan. The plan's own changelog records the expansion as relayed-not-architect-user-verified; the revert unit if "complete" is later narrowed to wire-only is all of Step 5 (Steps 1–3 stand independently).

### 5b — Structural rename + collision resolution (decided deliberately)

The plan flagged that `Features/Sprints/` → a board-name **collides** with the existing `Features/RetroSessions/` (which holds the `GetSprintContext/` carve-out + `RetroSessionContracts.cs`). **Resolution: merged `Features/Sprints/` INTO `Features/RetroSessions/`** — these board-lifecycle slices operate on the `RetroSession` aggregate, so they belong under that namespace; convention-consistent, no parallel `Board/` namespace invented. The `GetSprintContext/` carve-out subfolder stays untouched inside `RetroSessions/`.

- Folder moves (git mv, history preserved): `Sprints/{AdvancePhase,CloseSprint→CloseRetro,CreateSprint→CreateRetro,GetSprint→GetRetro,GetSprintSummary→GetRetroSummary,GetTeamSprints→GetTeamRetros,StartSprint→StartRetro,Timer,UpdateColumns}` → `RetroSessions/...`. `Sprints/` folder removed.
- 18 `namespace Reflekt.API.Features.Sprints.*` → `RetroSessions.*` (remapped to new subfolder names).
- Class/file renames: `CloseSprintEndpoint`→`CloseRetroEndpoint`, `BroadcastSprintClosedHandler`→`BroadcastRetroClosedHandler`, `GetSprintEndpoint`→`GetRetroEndpoint`, `GetSprintSummaryEndpoint`(+`Request`)→`GetRetroSummaryEndpoint`, `GetTeamSprintsEndpoint`(+`Request`)→`GetTeamRetrosEndpoint`, `StartSprintEndpoint`→`StartRetroEndpoint`. (`CreateSprintCommand/Endpoint` were already `CreateRetroSessionCommand/Endpoint` — only namespace fixed.)
- Filename-only Persistence renames: `SprintRepository.cs`→`RetroSessionRepository.cs`, `SprintConfiguration.cs`→`RetroSessionConfiguration.cs` (classes already `RetroSession*`).
- Reports: `Features/Reports/GetSprintReport/`→`GetRetroReport/`, `GetSprintReportEndpoint`(+`Request`)→`GetRetroReportEndpoint`(+`Request`).
- `JoinSprint*`→`JoinRetro*` was already done in the prior cycle (recorded, not re-flagged).

### 5a — Backend locals / params / strings / comments / contracts

- `RetroHub.cs`: all `sprintId` params→`retroSessionId`; `claimSprintId`/`parsedSprintId`/`sprintIdClaim`/`sprintRepo`→retro-named; `var sprint`→`var session`; `LoadTimedSprintAsync`→`LoadTimedRetroAsync`; HubException strings + `// sprintId ->` comments retro-worded. `retro:{…}` group + claim strings kept byte-identical.
- `TimerService.cs`: 10 `sprintId` params + "for sprint {…}" string → retro.
- `DependencyInjection.cs`: timer-expiry lambda param + "to sprint group" comment → retro. `/hubs/retro` + `retro:{…}` kept.
- `BroadcastPhaseChangedHandler.cs`: `var sprintId`→`retroSessionId`.
- `TeamRepository.cs`: `sprintIds`→`retroSessionIds`; `TeamSummaryDto` `SprintCount`/`LastSprintDate`/`HasActiveSprint`→`RetroSessionCount`/`LastRetroSessionDate`/`HasActiveRetroSession`.
- 11 endpoint guest-claim locals `claimSprintId`/`sprintIdClaim`→`claimRetroSessionId`/`retroSessionIdClaim` (claim NAME `"retroSessionId"` already correct).
- All endpoints: `var sprint`→`var session`; user-facing `"Sprint …"` error strings → `"Retro …"`; `[Tags("Sprints")]`→`[Tags("RetroSessions")]`.
- Contracts (response-body wire names, frontend-consumed): `ReportContracts.cs` `ReportSprintInfo`→`ReportRetroSessionInfo` + field `Sprint`→`RetroSession`; `PreviousActionsContracts.cs` `PreviousSprintId`/`PreviousSprintName`→`PreviousRetroSessionId`/`PreviousRetroSessionName`; `GetMyTeamsEndpoint`/`TeamSummaryDto` fields (above).
- `ClaimsPrincipalExtensions.cs` comment `(Team/Sprint)`→`(Team/RetroSession)`; `DbInitializer.cs` seed local + seed strings retro-neutral; `RetroSessionCreateTests.cs` test const `"Sprint 1 Retro"`→`"Retro 1"`.

### 5c — Frontend board-meaning identifiers (lockstep)

- File renames (git mv): `stores/sprint.ts`→`stores/retroSession.ts`, `composables/useSprintHub.ts`→`useRetroHub.ts`, `views/SprintView.vue`→`RetroSessionView.vue`, `views/SprintListView.vue`→`RetroSessionListView.vue`, `components/sprints/`→`components/retro-sessions/` (3 files → `RetroSessionDetailPanel`/`RetroSessionList`/`CreateRetroSessionForm`), `components/reports/SprintReportPanel.vue`→`RetroSessionReportPanel.vue`.
- `useSprintStore`→`useRetroSessionStore`, interfaces/state/actions/locals retro-named; router paths `/teams/:teamId/sprints…`→`/retro-sessions…`, guest deep-link `/sprint/:sprintId`→`/retro/:retroSessionId`, route names `team-sprints`/`sprint`→`team-retro-sessions`/`retro-session`, `:sprintId` param→`:retroSessionId`; props/events `:sprint-id`/`@close-sprint`/`@create-sprint`→retro equivalents; UI text retro-worded.
- Storage key `reflekt:sprint:${id}:token`→`reflekt:retro-session:${id}:token` — **sanctioned clean break** (orphans old-key tokens; guest tokens per-retro + short-lived, frontend sole reader).
- **Wire-adapter cleanup:** the frontend agent initially shimmed the report/teams/previous-actions DTOs with `*Wire` adapter types (reading the then-still-`sprint`-named JSON keys) to avoid runtime breakage. Since 5a renamed those backend contracts in lockstep (JSON now camelCase `retroSession`/`previousRetroSessionId`/`retroSessionCount`/etc.), the adapters were **removed** and the stores now read the new keys directly. Verified backend↔frontend key match.

### BLOCKER preservation (round-1 fix held through the rename)

`ColumnConfigUpdated` stays matched: `BroadcastColumnConfigUpdatedHandler.cs:14` sends `retroSessionId = ev.RetroSession.Id`; `useRetroHub.ts:155` reads `payload.retroSessionId` — byte-identical. `RetroClosed` event + `JoinRetro` invoke also matched in `useRetroHub.ts`.

### Strengthened acceptance gate — PASS

`rg -i "sprint" src/Services/Reflekt client/apps/retro -g '!**/Migrations/**'` through the carve-out exclusion → **7 surviving hits, all genuine carve-out:** 5 Fokus references (`Fokus sprint` doc-comments in RetroSessionRepository/DbInitializer/RetroSessionConfiguration, the `SprintAnalytics` gRPC config key, the `/hubs/sprint`/`Fokus.SprintHub` cross-module comments in DependencyInjection) + 2 CLAUDE.md product-doc prose lines ("sprint-retrospective tool", "anonymized sprint analytics from Fokus"). **Zero board-meaning identifier/route/UI/filename hits.** With the Fokus-comment patterns additionally filtered, the gate returns empty.

### Verification

- `dotnet build SprintRituals.slnx --no-restore` → 0 errors, 6 pre-existing warnings.
- `npm run build` (workspace) → 0 errors, both apps emit.
- `Reflekt.Domain.Tests` 7/7, `Reflekt.Persistence.Tests` 14/14 — green.
- `dotnet ef migrations has-pending-model-changes` → "No changes" (rename is identifier/contract only; no schema drift).
- Prior scoped gates re-confirmed zero: `api/sprints` in retro; the Step-3 wire-string set; `Features.Sprints` namespace.

### Known gaps / follow-ups (recorded, out of this rename's scope)

- **Google-assisted guest join** (`participant.ts` `joinWithGoogle`): posts `{}` to `/join`, which fails `JoinRetroCommand`'s `DisplayName.NotEmpty()` validator (400). The Google-authed auto-join (fill DisplayName from the Person/cookie claim) was never implemented — a pre-existing **functional gap**, tracked as a follow-up (`FEAT: Google-assisted guest join not implemented`). The string repoint to `/join` stands; the behavior fix is out of scope.
- **`CLAUDE.md` line 16** ("SignalR method/group/claim strings kept `sprint*`-named (Q2)") is now stale narrative after this rename. Left unedited — CLAUDE.md is module documentation, not code; updating its rename-history narrative is a doc follow-up, not part of the identifier sweep.
- **Manual real-time smoke** (column-config live update across clients; full create/group/vote/close retro) requires `dotnet run` + browser — deferred-by-policy (no automated frontend suite); the reviewer is the runtime gate.
